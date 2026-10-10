# Endurecimiento de refresh tokens — Plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Que la base de datos no guarde secretos de sesión utilizables, que un refresh token solo se pueda canjear una vez aunque lleguen peticiones concurrentes, que el coste de refresh, logout y cambio de contraseña no crezca con el histórico, y que los fallos de autenticación respondan 401.

**Architecture:** `UserToken` pasa a guardar el `jti` del JWT y el SHA-256 del refresh token; `JwtTokenProvider` genera ambos y los entrega a los handlers en un nuevo `IssuedToken`, sin cambiar el contrato HTTP de `IdentityToken`. Los handlers cargan solo el token presentado (o los activos) con `Include` filtrado, y `xmin` de Postgres actúa como concurrency token de `user_tokens` para detectar canjes concurrentes. Un job Quartz borra por lotes los tokens caducados hace más de 7 días.

**Tech Stack:** .NET 10, ASP.NET Core Minimal APIs, EF Core + Npgsql + EFCore.NamingConventions, Quartz, xUnit v3 + NSubstitute + FluentAssertions.

**Spec:** `docs/superpowers/specs/2026-10-07-refresh-token-hardening-design.md`

## Global Constraints

- Rama `fix/refresh-token-hardening`; el PR va a `staging`.
- No añadir frameworks ni capas nuevas; Quartz y EF Core ya están en uso.
- Pasar siempre `CancellationToken` en las llamadas asíncronas.
- `Directory.Build.props` trata los warnings como errores (SonarAnalyzer activo): cada paso de build tiene que salir limpio.
- **Nunca usar `dotnet test`.** Compilar y ejecutar los `.exe` de `test/*/bin/Debug/net10.0/`, y comprobar que el runner imprime un número de tests.
- El refresh token se hashea con SHA-256 en hex minúscula (64 caracteres); `AccessTokenId` es el `jti`, máximo 64 caracteres.
- El contrato JSON de `IdentityToken` (respuesta de `auth/login` y `auth/refresh-token`) no cambia.
- La migración borra todas las filas de `user_tokens`.
- Un replay solo se rechaza con `InvalidRefreshToken`; no revoca otras sesiones. Sin logger propio en `RefreshTokenHandler`.
- Configuración del job: sección `UserTokenCleanup` con `IntervalInHours: 24`, `RetentionDays: 7`, `BatchSize: 1000`.
- No incluir en ningún commit `src/Aurora.Flowboard.Api/appsettings.Development.json` ni `.claude/settings.local.json` (cambios locales del usuario).
- Cada commit termina con la línea `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.

## Review Focus

1. **Dos refresh en paralelo con el mismo token contra Postgres real.** Exactamente uno debe dar 200 y el otro 401. Los tests unitarios simulan la excepción, pero no prueban que `xmin` quede mapeado de verdad (Task 4, paso 5, y Task 6, paso 6).
2. **El `Include` filtrado se traduce a SQL.** Refresh, logout y cambio de contraseña deben cargar solo el token presentado o los activos, no todo el histórico. El mock ignora los `Include`, así que solo se ve en el log de SQL (Task 6, paso 7).
3. **Un cliente que presenta un refresh token emitido antes de la migración.** Debe recibir 401, no 500 (Task 4, paso 5).
4. **El job de limpieza nunca borra tokens vigentes.** Una sesión activa debe sobrevivir a la ejecución del job (Task 5, paso 6).
5. **El claim `jti` del JWT emitido coincide con `access_token_id` en la BD.** No hay tests de Infrastructure que lo comprueben (Task 4, paso 5).

---

### Task 1: Categoría `Unauthorized` (401) y `InvalidCurrentPassword` (M11)

**Files:**
- Modify: `src/Aurora.Flowboard.Domain/Abstractions/BaseErrorType.cs`
- Modify: `src/Aurora.Flowboard.Domain/Abstractions/BaseError.cs`
- Modify: `src/Aurora.Flowboard.Api/Responses/ApiResponses.cs:21-39`
- Modify: `src/Aurora.Flowboard.Application/Authentication/AuthenticationErrors.cs`
- Modify: `src/Aurora.Flowboard.Application/Authentication/ChangePassword/ChangePasswordHandler.cs:28-31`
- Modify: `src/Aurora.Flowboard.Api/Endpoints/Authentication/Login.cs:28`
- Modify: `src/Aurora.Flowboard.Api/Endpoints/Authentication/RefreshToken.cs:28`
- Modify: `src/Aurora.Flowboard.Api/Endpoints/Authentication/ChangePassword.cs:27`
- Test: `test/Aurora.Flowboard.Application.UnitTests/Authentication/LoginHandlerTests.cs`
- Test: `test/Aurora.Flowboard.Application.UnitTests/Authentication/RefreshTokenHandlerTests.cs`
- Test: `test/Aurora.Flowboard.Application.UnitTests/Authentication/ChangePasswordHandlerTests.cs:123-145`

**Interfaces:**
- Produces: `BaseErrorType.Unauthorized`, `BaseError.Unauthorized(string code, string message)`, `AuthenticationErrors.InvalidCurrentPassword` (Validation). `InvalidCredentials` e `InvalidRefreshToken` pasan a `Unauthorized`.

- [ ] **Step 1: Write the failing tests**

En `LoginHandlerTests.cs`, añadir después de `Should_ReturnInvalidCredentials_When_PasswordIsWrong`:

```csharp
    [Fact]
    public async Task Should_ReturnUnauthorizedError_When_PasswordIsWrong()
    {
        // Arrange
        User user = CreateUser();
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        _dbContext.Users.Returns(usersMock);
        _passwordHasher.VerifyHashedPassword(HashedPassword, Arg.Any<string>()).Returns(false);

        var command = new LoginCommand("john.doe@example.com", "wrong-password");

        // Act
        Result<IdentityToken> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Error.ErrorType.Should().Be(BaseErrorType.Unauthorized);
    }
```

En `RefreshTokenHandlerTests.cs`, dentro de `Should_ReturnInvalidRefreshToken_When_RefreshTokenIsUnknown`, añadir después de `result.Error.Should().Be(AuthenticationErrors.InvalidRefreshToken);`:

```csharp
        result.Error.ErrorType.Should().Be(BaseErrorType.Unauthorized);
```

En `ChangePasswordHandlerTests.cs`, sustituir el test `Should_ReturnInvalidCredentials_When_CurrentPasswordIsWrong` completo por:

```csharp
    [Fact]
    public async Task Should_ReturnInvalidCurrentPassword_When_CurrentPasswordIsWrong()
    {
        // Arrange
        User user = ChangePasswordCommandData.GetUser();
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        _dbContext.Users.Returns(usersMock);
        _userContext.UserId.Returns(user.Id);

        _passwordHasher
            .VerifyHashedPassword(ChangePasswordCommandData.CurrentPasswordHash, Arg.Any<string>())
            .Returns(false);

        ChangePasswordCommand command = ChangePasswordCommandData.GetCommand(currentPassword: "wrong-password");

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert — 400, not 401: the caller is authenticated, so a client must not treat this as an expired session.
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(AuthenticationErrors.InvalidCurrentPassword);
        result.Error.ErrorType.Should().Be(BaseErrorType.Validation);
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet build "Aurora Flowboard.slnx"`
Expected: FAIL con `CS0117: 'BaseErrorType' does not contain a definition for 'Unauthorized'` y `'AuthenticationErrors' does not contain a definition for 'InvalidCurrentPassword'`.

- [ ] **Step 3: Add the `Unauthorized` category**

`src/Aurora.Flowboard.Domain/Abstractions/BaseErrorType.cs`:

```csharp
namespace Aurora.Flowboard.Domain.Abstractions;

public enum BaseErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Forbidden = 4,
    Unauthorized = 5
}
```

`src/Aurora.Flowboard.Domain/Abstractions/BaseError.cs`, añadir después de `Forbidden(...)`:

```csharp
    public static BaseError Unauthorized(string code, string message) => new(code, message, BaseErrorType.Unauthorized);
```

`src/Aurora.Flowboard.Api/Responses/ApiResponses.cs`, añadir una rama a cada `switch` justo después de la de `Forbidden`:

```csharp
        BaseErrorType.Unauthorized => "https://tools.ietf.org/html/rfc7235#section-3.1",
```

```csharp
        BaseErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
```

- [ ] **Step 4: Update the authentication errors and the change password handler**

`src/Aurora.Flowboard.Application/Authentication/AuthenticationErrors.cs`:

```csharp
namespace Aurora.Flowboard.Application.Authentication;

public static class AuthenticationErrors
{
    public static readonly BaseError InvalidCredentials = BaseError.Unauthorized(
        "Auth.InvalidCredentials",
        "Invalid email or password.");

    public static readonly BaseError InvalidRefreshToken = BaseError.Unauthorized(
        "Auth.InvalidRefreshToken",
        "Invalid or expired refresh token.");

    public static readonly BaseError InvalidCurrentPassword = BaseError.Validation(
        "Auth.InvalidCurrentPassword",
        "The current password is incorrect.");

    public static readonly BaseError NewPasswordMustDiffer = BaseError.Validation(
        "Auth.NewPasswordMustDiffer",
        "The new password must be different from the current password.");
}
```

`ChangePasswordHandler.cs`, en el bloque de `VerifyPassword`:

```csharp
        if (!user.VerifyPassword(passwordHasher, command.CurrentPassword))
        {
            return Result.Fail(AuthenticationErrors.InvalidCurrentPassword);
        }
```

- [ ] **Step 5: Update the endpoint metadata**

- `Login.cs` y `RefreshToken.cs`: sustituir `.Produces<ProblemDetails>(StatusCodes.Status403Forbidden)` por `.Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)`.
- `ChangePassword.cs`: sustituir `.Produces<ProblemDetails>(StatusCodes.Status403Forbidden)` por `.Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)` (el 401 lo produce `RequireAuthorization` cuando falta el bearer).

- [ ] **Step 6: Run tests to verify they pass**

Run:
```bash
dotnet build "Aurora Flowboard.slnx"
./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests.exe -class "Aurora.Flowboard.Application.UnitTests.Authentication.LoginHandlerTests" -class "Aurora.Flowboard.Application.UnitTests.Authentication.RefreshTokenHandlerTests" -class "Aurora.Flowboard.Application.UnitTests.Authentication.ChangePasswordHandlerTests"
```
Expected: build sin warnings; los tres grupos en verde, con el número de tests impreso.

- [ ] **Step 7: Commit**

```bash
git add src/Aurora.Flowboard.Domain/Abstractions src/Aurora.Flowboard.Api/Responses/ApiResponses.cs src/Aurora.Flowboard.Api/Endpoints/Authentication src/Aurora.Flowboard.Application/Authentication test/Aurora.Flowboard.Application.UnitTests/Authentication
git commit -m "Return 401 for invalid credentials and refresh tokens

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 2: Modelo de dominio de `UserToken` (hash + `jti`)

Esta tarea solo deja compilando el proyecto Domain y sus tests: Application e Infrastructure se adaptan en la Task 3. **No se commitea aquí**; el commit de la Task 3 incluye los cambios de ambas para no dejar en la historia un commit que no compila.

**Files:**
- Modify: `src/Aurora.Flowboard.Domain/Users/UserToken.cs`
- Modify: `src/Aurora.Flowboard.Domain/Users/User.cs:107-180`
- Modify: `src/Aurora.Flowboard.Domain/Users/UserTokenErrors.cs`
- Modify: `test/Aurora.Flowboard.Domain.UnitTests/Users/UserData.cs`
- Test: `test/Aurora.Flowboard.Domain.UnitTests/Users/UserTests.cs`

**Interfaces:**
- Produces:
  - `UserToken.AccessTokenId` (`string`), `UserToken.RefreshTokenHash` (`string`), `UserToken.MaxAccessTokenIdLength = 64`, `UserToken.RefreshTokenHashLength = 64`, `UserToken.MaxRefreshTokenLength = 500` (se mantiene).
  - `User.IssueToken(string accessTokenId, string refreshTokenHash, DateTime accessTokenExpiresOnUtc, DateTime refreshTokenExpiresOnUtc, DateTime issuedOnUtc) : Result<UserToken>`.
  - `User.RevokeAllActiveTokens(DateTime utcNow) : Result`.
  - `UserTokenErrors.AccessTokenIdRequired`, `UserTokenErrors.RefreshTokenHashRequired`.
  - Desaparecen `UserToken.AccessToken`, `UserToken.RefreshToken`, `UserToken.MaxAccessTokenLength`, `UserTokenErrors.AccessTokenRequired`, `UserTokenErrors.RefreshTokenRequired`.

- [ ] **Step 1: Rewrite the domain test data**

`test/Aurora.Flowboard.Domain.UnitTests/Users/UserData.cs`:

```csharp
namespace Aurora.Flowboard.Domain.UnitTests.Users;

internal static class UserData
{
    public const string FirstName = "John";
    public const string LastName = "Doe";
    public const string EmailAddress = "john.doe@example.com";
    public const string AccessTokenId = "0f8fad5bd9cb469fa16570867728950e";
    public const string RefreshTokenHash = "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08";
    public static readonly Password Password = Password.Create("hashed_password_123").Value;
    public static readonly Password NewPassword = Password.Create("new_hashed_password_456").Value;
    public static readonly DateTime CreatedOnUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime UpdatedOnUtc = new(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime AccessTokenExpiresOnUtc = new(2026, 1, 1, 1, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime RefreshTokenExpiresOnUtc = new(2026, 1, 8, 0, 0, 0, DateTimeKind.Utc);

    public static User GetActiveUser()
    {
        Email email = Email.Create(EmailAddress).Value;
        return User.Create(FirstName, LastName, email, Password, CreatedOnUtc).Value;
    }

    public static User GetInactiveUser()
    {
        User user = GetActiveUser();
        user.Deactivate(CreatedOnUtc);
        return user;
    }

    public static User GetUserWithToken(out Guid tokenId)
    {
        User user = GetActiveUser();
        Result<UserToken> result = user.IssueToken(
            AccessTokenId,
            RefreshTokenHash,
            AccessTokenExpiresOnUtc,
            RefreshTokenExpiresOnUtc,
            CreatedOnUtc);
        tokenId = result.Value.UserTokenId;
        return user;
    }

    public static User GetUserWithRole(Role role)
    {
        User user = GetActiveUser();
        user.AssignRole(role);
        return user;
    }

    public static User GetUserWithTwoTokens(out Guid firstTokenId, out Guid secondTokenId)
    {
        User user = GetActiveUser();

        firstTokenId = user.IssueToken(
            AccessTokenId,
            RefreshTokenHash,
            AccessTokenExpiresOnUtc,
            RefreshTokenExpiresOnUtc,
            CreatedOnUtc).Value.UserTokenId;

        secondTokenId = user.IssueToken(
            $"{AccessTokenId}-2",
            $"{RefreshTokenHash}-2",
            AccessTokenExpiresOnUtc,
            RefreshTokenExpiresOnUtc,
            CreatedOnUtc).Value.UserTokenId;

        return user;
    }

    // The second token's refresh expiry (CreatedOnUtc - 3 days) is already past at CreatedOnUtc.
    public static User GetUserWithActiveAndExpiredToken(out Guid activeTokenId, out Guid expiredTokenId)
    {
        User user = GetActiveUser();

        activeTokenId = user.IssueToken(
            AccessTokenId,
            RefreshTokenHash,
            AccessTokenExpiresOnUtc,
            RefreshTokenExpiresOnUtc,
            CreatedOnUtc).Value.UserTokenId;

        expiredTokenId = user.IssueToken(
            $"{AccessTokenId}-expired",
            $"{RefreshTokenHash}-expired",
            CreatedOnUtc.AddDays(-9),
            CreatedOnUtc.AddDays(-3),
            CreatedOnUtc.AddDays(-10)).Value.UserTokenId;

        return user;
    }
}
```

- [ ] **Step 2: Apply the mechanical renames to `UserTests.cs`**

Run (Git Bash; `\b` evita tocar `AccessTokenExpiresOnUtc` y `RefreshTokenExpiresOnUtc`):

```bash
sed -i \
  -e 's/UserData\.AccessToken\b/UserData.AccessTokenId/g' \
  -e 's/UserData\.RefreshToken\b/UserData.RefreshTokenHash/g' \
  -e 's/UserTokenErrors\.AccessTokenRequired/UserTokenErrors.AccessTokenIdRequired/g' \
  -e 's/UserTokenErrors\.RefreshTokenRequired/UserTokenErrors.RefreshTokenHashRequired/g' \
  -e 's/_When_AccessTokenIs\(Empty\|Whitespace\)/_When_AccessTokenIdIs\1/g' \
  -e 's/_When_RefreshTokenIs\(Empty\|Whitespace\)/_When_RefreshTokenHashIs\1/g' \
  -e 's/RevokeAllActiveTokens()/RevokeAllActiveTokens(UserData.CreatedOnUtc)/g' \
  test/Aurora.Flowboard.Domain.UnitTests/Users/UserTests.cs
grep -n "AccessToken\b\|RefreshToken\b\|RevokeAllActiveTokens(" test/Aurora.Flowboard.Domain.UnitTests/Users/UserTests.cs
```

Expected: el `grep` solo muestra llamadas `RevokeAllActiveTokens(UserData.CreatedOnUtc)`; ninguna referencia a `UserData.AccessToken` / `UserData.RefreshToken` sin sufijo.

- [ ] **Step 3: Write the new failing tests**

En `UserTests.cs`, dentro de `public sealed class RevokeAllActiveTokens : BaseTest`, después de `Should_Succeed_When_NoTokensExist`:

```csharp
        [Fact]
        public void Should_NotRevokeExpiredTokens_When_RevokingAllActiveTokens()
        {
            // Arrange
            User user = UserData.GetUserWithActiveAndExpiredToken(out Guid activeTokenId, out Guid expiredTokenId);

            // Act
            Result result = user.RevokeAllActiveTokens(UserData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            user.Tokens.Single(t => t.UserTokenId == activeTokenId).IsRevoked.Should().BeTrue();
            user.Tokens.Single(t => t.UserTokenId == expiredTokenId).IsRevoked.Should().BeFalse();
        }

        [Fact]
        public void Should_RaiseEventsOnlyForActiveTokens_When_SomeTokensExpired()
        {
            // Arrange
            User user = UserData.GetUserWithActiveAndExpiredToken(out Guid activeTokenId, out _);
            user.ClearDomainEvents();

            // Act
            user.RevokeAllActiveTokens(UserData.CreatedOnUtc);

            // Assert
            user.DomainEvents.OfType<UserTokenRevokedDomainEvent>()
                .Should().ContainSingle(e => e.UserTokenId == activeTokenId);
        }
```

En `public sealed class IssueToken : BaseTest`, después de `Should_IssueToken_When_DataIsValid`:

```csharp
        [Fact]
        public void Should_StoreAccessTokenIdAndRefreshTokenHash_When_Issued()
        {
            // Arrange
            User user = UserData.GetActiveUser();

            // Act
            Result<UserToken> result = user.IssueToken(
                UserData.AccessTokenId,
                UserData.RefreshTokenHash,
                UserData.AccessTokenExpiresOnUtc,
                UserData.RefreshTokenExpiresOnUtc,
                UserData.CreatedOnUtc);

            // Assert
            result.Value.AccessTokenId.Should().Be(UserData.AccessTokenId);
            result.Value.RefreshTokenHash.Should().Be(UserData.RefreshTokenHash);
        }
```

- [ ] **Step 4: Run the domain tests to verify they fail**

Run: `dotnet build test/Aurora.Flowboard.Domain.UnitTests`
Expected: FAIL. Es correcto que falle: tiene que dar `CS1061` / `CS0117` por `AccessTokenId`, `RefreshTokenHash`, `AccessTokenIdRequired`, `RefreshTokenHashRequired`, y `CS1501` por `RevokeAllActiveTokens` con 1 argumento.

- [ ] **Step 5: Implement the domain changes**

`src/Aurora.Flowboard.Domain/Users/UserToken.cs`:

```csharp
namespace Aurora.Flowboard.Domain.Users;

public sealed class UserToken
{
    public const int MaxAccessTokenIdLength = 64;
    public const int RefreshTokenHashLength = 64;
    public const int MaxRefreshTokenLength = 500;

    public Guid UserTokenId { get; private set; }
    public Guid UserId { get; private set; }
    public string AccessTokenId { get; private set; }
    public string RefreshTokenHash { get; private set; }
    public DateTime AccessTokenExpiresOnUtc { get; private set; }
    public DateTime RefreshTokenExpiresOnUtc { get; private set; }
    public DateTime IssuedOnUtc { get; private set; }
    public bool IsRevoked { get; private set; }

    public User User { get; init; } = null!;

    private UserToken() { } // EF Core

    private UserToken(
        Guid userTokenId,
        Guid userId,
        string accessTokenId,
        string refreshTokenHash,
        DateTime accessTokenExpiresOnUtc,
        DateTime refreshTokenExpiresOnUtc,
        DateTime issuedOnUtc)
    {
        UserTokenId = userTokenId;
        UserId = userId;
        AccessTokenId = accessTokenId;
        RefreshTokenHash = refreshTokenHash;
        AccessTokenExpiresOnUtc = accessTokenExpiresOnUtc;
        RefreshTokenExpiresOnUtc = refreshTokenExpiresOnUtc;
        IssuedOnUtc = issuedOnUtc;
        IsRevoked = false;
    }

    internal static UserToken Create(
        Guid userId,
        string accessTokenId,
        string refreshTokenHash,
        DateTime accessTokenExpiresOnUtc,
        DateTime refreshTokenExpiresOnUtc,
        DateTime issuedOnUtc)
    {
        return new UserToken(
            Guid.NewGuid(),
            userId,
            accessTokenId,
            refreshTokenHash,
            accessTokenExpiresOnUtc,
            refreshTokenExpiresOnUtc,
            issuedOnUtc);
    }

    internal Result Revoke()
    {
        if (IsRevoked)
        {
            return Result.Fail(UserTokenErrors.AlreadyRevoked);
        }

        IsRevoked = true;

        return Result.Ok();
    }

    public bool IsRefreshTokenValid(DateTime utcNow) =>
        !IsRevoked && RefreshTokenExpiresOnUtc > utcNow;
}
```

`src/Aurora.Flowboard.Domain/Users/UserTokenErrors.cs`, sustituir `AccessTokenRequired` y `RefreshTokenRequired` por:

```csharp
    public static readonly BaseError AccessTokenIdRequired = BaseError.Validation(
        "UserToken.AccessTokenIdRequired",
        "Access token identifier is required");

    public static readonly BaseError RefreshTokenHashRequired = BaseError.Validation(
        "UserToken.RefreshTokenHashRequired",
        "Refresh token hash is required");
```

`src/Aurora.Flowboard.Domain/Users/User.cs`, sustituir `IssueToken` y `RevokeAllActiveTokens`:

```csharp
    public Result<UserToken> IssueToken(
        string accessTokenId,
        string refreshTokenHash,
        DateTime accessTokenExpiresOnUtc,
        DateTime refreshTokenExpiresOnUtc,
        DateTime issuedOnUtc)
    {
        if (string.IsNullOrWhiteSpace(accessTokenId))
        {
            return Result.Fail<UserToken>(UserTokenErrors.AccessTokenIdRequired);
        }

        if (string.IsNullOrWhiteSpace(refreshTokenHash))
        {
            return Result.Fail<UserToken>(UserTokenErrors.RefreshTokenHashRequired);
        }

        if (accessTokenExpiresOnUtc <= issuedOnUtc)
        {
            return Result.Fail<UserToken>(UserTokenErrors.InvalidExpiration);
        }

        if (refreshTokenExpiresOnUtc <= issuedOnUtc)
        {
            return Result.Fail<UserToken>(UserTokenErrors.InvalidExpiration);
        }

        var token = UserToken.Create(
            Id,
            accessTokenId,
            refreshTokenHash,
            accessTokenExpiresOnUtc,
            refreshTokenExpiresOnUtc,
            issuedOnUtc);

        _tokens.Add(token);

        AddDomainEvent(new UserTokenIssuedDomainEvent(Id, token.UserTokenId));

        return token;
    }
```

```csharp
    public Result RevokeAllActiveTokens(DateTime utcNow)
    {
        // Expired tokens can no longer be redeemed, so they need no revocation (nor an event).
        foreach (UserToken token in _tokens.Where(t => t.IsRefreshTokenValid(utcNow)))
        {
            token.Revoke();

            AddDomainEvent(new UserTokenRevokedDomainEvent(Id, token.UserTokenId));
        }

        return Result.Ok();
    }
```

- [ ] **Step 6: Run the domain tests to verify they pass**

Run:
```bash
dotnet build test/Aurora.Flowboard.Domain.UnitTests
./test/Aurora.Flowboard.Domain.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Domain.UnitTests.exe
```
Expected: build limpio del proyecto de tests de Domain; todos los tests en verde, incluidos los 3 nuevos, con el número de tests impreso.

- [ ] **Step 7: No commit**

`dotnet build "Aurora Flowboard.slnx"` falla ahora en Application e Infrastructure. Esto es lo esperado. Se commitea al final de la Task 3.

---

### Task 3: Hash y `jti` en el proveedor, mapeo EF y handlers con carga acotada y conflictos `xmin`

**Files:**
- Create: `src/Aurora.Flowboard.Application/Abstractions/Authentication/IssuedToken.cs`
- Modify: `src/Aurora.Flowboard.Application/Abstractions/Authentication/ITokenProvider.cs`
- Modify: `src/Aurora.Flowboard.Infrastructure/Authentication/JwtTokenProvider.cs`
- Modify: `src/Aurora.Flowboard.Infrastructure/Configurations/UserTokenConfiguration.cs`
- Modify: `src/Aurora.Flowboard.Application/Authentication/AuthenticationErrors.cs`
- Modify: `src/Aurora.Flowboard.Application/Authentication/Login/LoginHandler.cs`
- Modify: `src/Aurora.Flowboard.Application/Authentication/RefreshToken/RefreshTokenHandler.cs`
- Modify: `src/Aurora.Flowboard.Application/Authentication/Logout/LogoutHandler.cs`
- Modify: `src/Aurora.Flowboard.Application/Authentication/ChangePassword/ChangePasswordHandler.cs`
- Modify: `src/Aurora.Flowboard.Api/Endpoints/Authentication/ChangePassword.cs`
- Test: `test/Aurora.Flowboard.Application.UnitTests/Authentication/RefreshTokenHandlerTests.cs` (reescritura)
- Test: `test/Aurora.Flowboard.Application.UnitTests/Authentication/LogoutHandlerTests.cs` (reescritura)
- Test: `test/Aurora.Flowboard.Application.UnitTests/Authentication/LoginHandlerTests.cs`
- Test: `test/Aurora.Flowboard.Application.UnitTests/Authentication/ChangePasswordHandlerTests.cs`

**Interfaces:**
- Consumes (Task 2): `User.IssueToken(accessTokenId, refreshTokenHash, ...)`, `User.RevokeAllActiveTokens(DateTime)`, `UserToken.AccessTokenId`, `UserToken.RefreshTokenHash`, `UserToken.MaxAccessTokenIdLength`, `UserToken.RefreshTokenHashLength`. (Task 1): `AuthenticationErrors.InvalidCurrentPassword`, `InvalidRefreshToken` (Unauthorized).
- Produces:
  - `public sealed record IssuedToken(IdentityToken Identity, string AccessTokenId, string RefreshTokenHash)`.
  - `ITokenProvider.CreateToken(TokenRequest) : IssuedToken` y `ITokenProvider.HashRefreshToken(string) : string`.
  - `AuthenticationErrors.SessionChangedConcurrently` (Conflict).
  - `LogoutHandler(IApplicationDbContext, ITokenProvider, IUserContext)`.
  - Shadow property `"Version"` (`uint`, columna `xmin`) en `UserToken`; índices sobre `refresh_token_hash` (único) y `refresh_token_expires_on_utc`.

- [ ] **Step 1: Write the failing refresh token handler tests**

Sustituir `test/Aurora.Flowboard.Application.UnitTests/Authentication/RefreshTokenHandlerTests.cs` completo por:

```csharp
using Aurora.Flowboard.Application.Authentication;
using Aurora.Flowboard.Application.Authentication.RefreshToken;
using NSubstitute.ExceptionExtensions;

namespace Aurora.Flowboard.Application.UnitTests.Authentication;

public sealed class RefreshTokenHandlerTests
{
    private const string HashedPassword = "hashed_password_123";
    private const string PresentedRefreshToken = "presented-refresh-token";
    private const string PresentedRefreshTokenHash = "presented-refresh-token-hash";
    private static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly IApplicationDbContext _dbContext;
    private readonly ITokenProvider _tokenProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly RefreshTokenHandler _handler;

    public RefreshTokenHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _tokenProvider = Substitute.For<ITokenProvider>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();

        _handler = new RefreshTokenHandler(
            _dbContext,
            _tokenProvider,
            _dateTimeProvider);

        _dateTimeProvider.UtcNow.Returns(UtcNow);
        _tokenProvider.HashRefreshToken(PresentedRefreshToken).Returns(PresentedRefreshTokenHash);
    }

    [Fact]
    public async Task Should_ReturnNewIdentityToken_When_RefreshTokenIsValid()
    {
        // Arrange — the stored token only holds the hash, so a match proves the handler hashed the input.
        User user = CreateUser();
        UserToken oldToken = IssueValidToken(user);
        SetupUsers(user);

        IssuedToken issued = CreateIssuedToken();
        _tokenProvider.CreateToken(Arg.Any<TokenRequest>()).Returns(issued);

        // Act
        Result<IdentityToken> result = await _handler.Handle(new RefreshTokenCommand(PresentedRefreshToken), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().Be(issued.Identity);
        oldToken.IsRevoked.Should().BeTrue();
        user.Tokens.Should().ContainSingle(t =>
            t.AccessTokenId == issued.AccessTokenId &&
            t.RefreshTokenHash == issued.RefreshTokenHash &&
            !t.IsRevoked);
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnInvalidRefreshToken_When_RefreshTokenIsUnknown()
    {
        // Arrange — the user has a token, but not the presented one.
        User user = CreateUser();
        user.IssueToken("other-access-token-id", "other-refresh-token-hash", UtcNow.AddMinutes(60), UtcNow.AddDays(7), UtcNow);
        SetupUsers(user);

        // Act
        Result<IdentityToken> result = await _handler.Handle(new RefreshTokenCommand(PresentedRefreshToken), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(AuthenticationErrors.InvalidRefreshToken);
        result.Error.ErrorType.Should().Be(BaseErrorType.Unauthorized);
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnInvalidRefreshToken_When_RefreshTokenIsReplayed()
    {
        // Arrange — the token was already rotated by an earlier refresh.
        User user = CreateUser();
        UserToken oldToken = IssueValidToken(user);
        user.RevokeToken(oldToken.UserTokenId);
        SetupUsers(user);

        // Act
        Result<IdentityToken> result = await _handler.Handle(new RefreshTokenCommand(PresentedRefreshToken), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(AuthenticationErrors.InvalidRefreshToken);
        _tokenProvider.DidNotReceive().CreateToken(Arg.Any<TokenRequest>());
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnInvalidRefreshToken_When_RefreshTokenIsExpired()
    {
        // Arrange
        User user = CreateUser();
        user.IssueToken(
            "old-access-token-id",
            PresentedRefreshTokenHash,
            UtcNow.AddDays(-9),
            UtcNow.AddDays(-3),
            UtcNow.AddDays(-10));
        SetupUsers(user);

        // Act
        Result<IdentityToken> result = await _handler.Handle(new RefreshTokenCommand(PresentedRefreshToken), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(AuthenticationErrors.InvalidRefreshToken);
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnInvalidRefreshToken_When_UserIsInactive()
    {
        // Arrange
        User user = CreateUser();
        IssueValidToken(user);
        user.Deactivate(UtcNow);
        SetupUsers(user);

        // Act
        Result<IdentityToken> result = await _handler.Handle(new RefreshTokenCommand(PresentedRefreshToken), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(AuthenticationErrors.InvalidRefreshToken);
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnInvalidRefreshToken_When_TokenIsRedeemedConcurrently()
    {
        // Arrange — another request revoked the same row first, so the xmin check fails on save.
        User user = CreateUser();
        IssueValidToken(user);
        SetupUsers(user);
        _tokenProvider.CreateToken(Arg.Any<TokenRequest>()).Returns(CreateIssuedToken());
        _dbContext.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new DbUpdateConcurrencyException("user_tokens row changed"));

        // Act
        Result<IdentityToken> result = await _handler.Handle(new RefreshTokenCommand(PresentedRefreshToken), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(AuthenticationErrors.InvalidRefreshToken);
    }

    [Fact]
    public async Task Should_ReturnFailure_When_NewTokenExpirationIsInvalid()
    {
        // Arrange — CreateToken returns an access token that already expires at issuance time.
        User user = CreateUser();
        IssueValidToken(user);
        SetupUsers(user);
        _tokenProvider.CreateToken(Arg.Any<TokenRequest>()).Returns(CreateIssuedToken(accessTokenExpiresOnUtc: UtcNow));

        // Act
        Result<IdentityToken> result = await _handler.Handle(new RefreshTokenCommand(PresentedRefreshToken), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(UserTokenErrors.InvalidExpiration);
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_PassUserRolesIntoTokenRequest_When_UserHasRoles()
    {
        // Arrange
        User user = CreateUser();
        user.AssignRole(Role.Administrator);
        IssueValidToken(user);
        SetupUsers(user);
        _tokenProvider.CreateToken(Arg.Any<TokenRequest>()).Returns(CreateIssuedToken());

        // Act
        await _handler.Handle(new RefreshTokenCommand(PresentedRefreshToken), CancellationToken.None);

        // Assert
        _tokenProvider.Received(1).CreateToken(Arg.Is<TokenRequest>(r =>
            r.UserId == user.Id &&
            r.Email == user.Email.Value &&
            r.Roles.Contains(Role.Administrator.Name)));
    }

    private void SetupUsers(params User[] users)
    {
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(users);
        _dbContext.Users.Returns(usersMock);
    }

    private static User CreateUser()
    {
        Email email = Email.Create("john.doe@example.com").Value;
        Password password = Password.Create(HashedPassword).Value;
        return User.Create("John", "Doe", email, password, UtcNow).Value;
    }

    private static UserToken IssueValidToken(User user) => user.IssueToken(
        "old-access-token-id",
        PresentedRefreshTokenHash,
        UtcNow.AddMinutes(60),
        UtcNow.AddDays(7),
        UtcNow).Value;

    private static IssuedToken CreateIssuedToken(DateTime? accessTokenExpiresOnUtc = null) => new(
        new IdentityToken(
            AccessToken: "new-access-token",
            AccessTokenExpiresOn: new DateTimeOffset(accessTokenExpiresOnUtc ?? UtcNow.AddMinutes(60), TimeSpan.Zero),
            RefreshToken: "new-refresh-token",
            RefreshTokenExpiresOn: new DateTimeOffset(UtcNow.AddDays(7), TimeSpan.Zero)),
        AccessTokenId: "new-access-token-id",
        RefreshTokenHash: "new-refresh-token-hash");
}
```

- [ ] **Step 2: Write the failing logout handler tests**

Sustituir `test/Aurora.Flowboard.Application.UnitTests/Authentication/LogoutHandlerTests.cs` completo por:

```csharp
using Aurora.Flowboard.Application.Authentication.Logout;
using NSubstitute.ExceptionExtensions;

namespace Aurora.Flowboard.Application.UnitTests.Authentication;

public sealed class LogoutHandlerTests
{
    private const string HashedPassword = "hashed_password_123";
    private const string PresentedRefreshToken = "presented-refresh-token";
    private const string PresentedRefreshTokenHash = "presented-refresh-token-hash";
    private static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly IApplicationDbContext _dbContext;
    private readonly ITokenProvider _tokenProvider;
    private readonly IUserContext _userContext;
    private readonly LogoutHandler _handler;

    public LogoutHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _tokenProvider = Substitute.For<ITokenProvider>();
        _userContext = Substitute.For<IUserContext>();

        _handler = new LogoutHandler(_dbContext, _tokenProvider, _userContext);

        _tokenProvider.HashRefreshToken(PresentedRefreshToken).Returns(PresentedRefreshTokenHash);
    }

    [Fact]
    public async Task Should_RevokeToken_When_RefreshTokenBelongsToCurrentUser()
    {
        // Arrange
        User user = CreateUser();
        UserToken token = IssueToken(user);
        SetupUsers(user);
        _userContext.UserId.Returns(user.Id);

        // Act
        Result result = await _handler.Handle(new LogoutCommand(PresentedRefreshToken), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        token.IsRevoked.Should().BeTrue();
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_RefreshTokenIsUnknown()
    {
        // Arrange
        User user = CreateUser();
        user.IssueToken("other-access-token-id", "other-refresh-token-hash", UtcNow.AddMinutes(60), UtcNow.AddDays(7), UtcNow);
        SetupUsers(user);
        _userContext.UserId.Returns(user.Id);

        // Act
        Result result = await _handler.Handle(new LogoutCommand(PresentedRefreshToken), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_RefreshTokenBelongsToAnotherUser()
    {
        // Arrange
        User user = CreateUser();
        UserToken token = IssueToken(user);
        SetupUsers(user);
        _userContext.UserId.Returns(Guid.NewGuid());

        // Act
        Result result = await _handler.Handle(new LogoutCommand(PresentedRefreshToken), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        token.IsRevoked.Should().BeFalse();
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnSuccessWithoutSaving_When_TokenIsAlreadyRevoked()
    {
        // Arrange
        User user = CreateUser();
        UserToken token = IssueToken(user);
        user.RevokeToken(token.UserTokenId);
        SetupUsers(user);
        _userContext.UserId.Returns(user.Id);

        // Act
        Result result = await _handler.Handle(new LogoutCommand(PresentedRefreshToken), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        token.IsRevoked.Should().BeTrue();
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_RevokeToken_When_UserIsInactive()
    {
        // Arrange
        User user = CreateUser();
        UserToken token = IssueToken(user);
        user.Deactivate(UtcNow);
        SetupUsers(user);
        _userContext.UserId.Returns(user.Id);

        // Act
        Result result = await _handler.Handle(new LogoutCommand(PresentedRefreshToken), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        token.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_TokenIsRevokedConcurrently()
    {
        // Arrange — a concurrent refresh or logout changed the row first; the session is closed either way.
        User user = CreateUser();
        IssueToken(user);
        SetupUsers(user);
        _userContext.UserId.Returns(user.Id);
        _dbContext.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new DbUpdateConcurrencyException("user_tokens row changed"));

        // Act
        Result result = await _handler.Handle(new LogoutCommand(PresentedRefreshToken), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }

    private void SetupUsers(params User[] users)
    {
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(users);
        _dbContext.Users.Returns(usersMock);
    }

    private static User CreateUser()
    {
        Email email = Email.Create("john.doe@example.com").Value;
        Password password = Password.Create(HashedPassword).Value;
        return User.Create("John", "Doe", email, password, UtcNow).Value;
    }

    private static UserToken IssueToken(User user) => user.IssueToken(
        "access-token-id",
        PresentedRefreshTokenHash,
        UtcNow.AddMinutes(60),
        UtcNow.AddDays(7),
        UtcNow).Value;
}
```

- [ ] **Step 3: Update the login and change password handler tests**

`LoginHandlerTests.cs`:
- En `Should_ReturnIdentityToken_When_CredentialsAreValid`, sustituir desde `IdentityToken issued = CreateIdentityToken();` hasta el final del método por:

```csharp
        IssuedToken issued = CreateIssuedToken();
        _tokenProvider.CreateToken(Arg.Any<TokenRequest>()).Returns(issued);

        var command = new LoginCommand("john.doe@example.com", PlainPassword);

        // Act
        Result<IdentityToken> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().Be(issued.Identity);
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        user.Tokens.Should().ContainSingle(t =>
            t.AccessTokenId == issued.AccessTokenId &&
            t.RefreshTokenHash == issued.RefreshTokenHash);
    }
```

- En `Should_PassUserRolesIntoTokenRequest_When_UserHasRoles`, cambiar `.Returns(CreateIdentityToken());` por `.Returns(CreateIssuedToken());`.
- Sustituir el helper `CreateIdentityToken()` por:

```csharp
    private static IssuedToken CreateIssuedToken() => new(
        new IdentityToken(
            AccessToken: "access-token-value",
            AccessTokenExpiresOn: new DateTimeOffset(UtcNow.AddMinutes(60), TimeSpan.Zero),
            RefreshToken: "refresh-token-value",
            RefreshTokenExpiresOn: new DateTimeOffset(UtcNow.AddDays(7), TimeSpan.Zero)),
        AccessTokenId: "access-token-id",
        RefreshTokenHash: "refresh-token-hash");
```

`ChangePasswordHandlerTests.cs`:
- Añadir `using NSubstitute.ExceptionExtensions;` debajo de los `using` existentes.
- En `Should_RevokeAllActiveTokens_When_PasswordChanged`, sustituir las dos llamadas `user.IssueToken("access-1", "refresh-1", ...)` / `("access-2", "refresh-2", ...)` por `("access-token-id-1", "refresh-token-hash-1", ...)` / `("access-token-id-2", "refresh-token-hash-2", ...)`, con los mismos argumentos de fecha.
- Añadir al final de la clase:

```csharp
    [Fact]
    public async Task Should_NotRevokeExpiredTokens_When_PasswordChanged()
    {
        // Arrange
        User user = ChangePasswordCommandData.GetUser();
        UserToken expiredToken = user.IssueToken(
            "access-token-id-old",
            "refresh-token-hash-old",
            ChangePasswordCommandData.UtcNow.AddDays(-9),
            ChangePasswordCommandData.UtcNow.AddDays(-3),
            ChangePasswordCommandData.UtcNow.AddDays(-10)).Value;

        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        _dbContext.Users.Returns(usersMock);
        _userContext.UserId.Returns(user.Id);

        _passwordHasher
            .VerifyHashedPassword(ChangePasswordCommandData.CurrentPasswordHash, ChangePasswordCommandData.CurrentPlainPassword)
            .Returns(true);

        // Act
        Result result = await _handler.Handle(ChangePasswordCommandData.GetCommand(), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        expiredToken.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task Should_ReturnSessionChangedConcurrently_When_TokensChangeDuringSave()
    {
        // Arrange — a refresh rotated one of the tokens between the read and the save.
        User user = ChangePasswordCommandData.GetUser();
        user.IssueToken(
            "access-token-id-1",
            "refresh-token-hash-1",
            ChangePasswordCommandData.UtcNow.AddMinutes(60),
            ChangePasswordCommandData.UtcNow.AddDays(7),
            ChangePasswordCommandData.UtcNow);

        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        _dbContext.Users.Returns(usersMock);
        _userContext.UserId.Returns(user.Id);

        _passwordHasher
            .VerifyHashedPassword(ChangePasswordCommandData.CurrentPasswordHash, ChangePasswordCommandData.CurrentPlainPassword)
            .Returns(true);
        _dbContext.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new DbUpdateConcurrencyException("user_tokens row changed"));

        // Act
        Result result = await _handler.Handle(ChangePasswordCommandData.GetCommand(), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(AuthenticationErrors.SessionChangedConcurrently);
        result.Error.ErrorType.Should().Be(BaseErrorType.Conflict);
    }
```

- [ ] **Step 4: Run the build to verify the tests fail**

Run: `dotnet build "Aurora Flowboard.slnx"`
Expected: FAIL. Errores esperados: `CS0246: 'IssuedToken' could not be found`, `CS1061: 'ITokenProvider' does not contain a definition for 'HashRefreshToken'`, `CS1729` en el constructor de `LogoutHandler`, `CS0117` por `SessionChangedConcurrently`, y los errores de los handlers y de `UserTokenConfiguration` sobre `AccessToken` / `RefreshToken`.

- [ ] **Step 5: Add `IssuedToken` and extend `ITokenProvider`**

`src/Aurora.Flowboard.Application/Abstractions/Authentication/IssuedToken.cs`:

```csharp
namespace Aurora.Flowboard.Application.Abstractions.Authentication;

/// <summary>
/// What <see cref="ITokenProvider"/> issues: the pair returned to the client, plus the values that
/// are persisted instead of the secrets (the JWT id and the refresh token hash).
/// </summary>
public sealed record IssuedToken(
    IdentityToken Identity,
    string AccessTokenId,
    string RefreshTokenHash);
```

`src/Aurora.Flowboard.Application/Abstractions/Authentication/ITokenProvider.cs`:

```csharp
namespace Aurora.Flowboard.Application.Abstractions.Authentication;

public interface ITokenProvider
{
    IssuedToken CreateToken(TokenRequest tokenRequest);

    string HashRefreshToken(string refreshToken);
}
```

- [ ] **Step 6: Generate the `jti` once and hash the refresh token in `JwtTokenProvider`**

En `src/Aurora.Flowboard.Infrastructure/Authentication/JwtTokenProvider.cs`, sustituir `CreateToken` por:

```csharp
    public IssuedToken CreateToken(TokenRequest tokenRequest)
    {
        DateTime utcNow = _dateTimeProvider.UtcNow;
        string accessTokenId = Guid.NewGuid().ToString("N");

        (string accessToken, DateTimeOffset accessTokenExpiresOn) = GenerateAccessToken(tokenRequest, accessTokenId, utcNow);
        (string refreshToken, DateTimeOffset refreshTokenExpiresOn) = GenerateRefreshToken(utcNow);

        var identityToken = new IdentityToken(
            accessToken,
            accessTokenExpiresOn,
            refreshToken,
            refreshTokenExpiresOn);

        return new IssuedToken(identityToken, accessTokenId, HashRefreshToken(refreshToken));
    }

    // The refresh token is 64 random bytes, so a plain SHA-256 cannot be brute-forced; no salt or pepper needed.
    public string HashRefreshToken(string refreshToken) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
```

Cambiar la firma de `GenerateAccessToken` a `GenerateAccessToken(TokenRequest tokenRequest, string accessTokenId, DateTime utcNow)` y el claim `jti` a:

```csharp
            new(JwtRegisteredClaimNames.Jti, accessTokenId),
```

- [ ] **Step 7: Map the new columns, the indexes and `xmin`**

`src/Aurora.Flowboard.Infrastructure/Configurations/UserTokenConfiguration.cs`:

```csharp
using Aurora.Flowboard.Domain.Users;

namespace Aurora.Flowboard.Infrastructure.Configurations;

internal sealed class UserTokenConfiguration : IEntityTypeConfiguration<UserToken>
{
    // Postgres' xmin system column, used as an optimistic concurrency token: two requests that
    // redeem the same refresh token cannot both update the row. No migration column is generated.
    private const string VersionPropertyName = "Version";
    private const string VersionColumnName = "xmin";

    public void Configure(EntityTypeBuilder<UserToken> builder)
    {
        builder.ToTable("user_tokens");

        builder.HasKey(x => x.UserTokenId);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.AccessTokenId)
            .IsRequired()
            .HasMaxLength(UserToken.MaxAccessTokenIdLength);

        builder.Property(x => x.RefreshTokenHash)
            .IsRequired()
            .HasMaxLength(UserToken.RefreshTokenHashLength)
            .IsFixedLength();

        builder.Property(x => x.AccessTokenExpiresOnUtc)
            .IsRequired();

        builder.Property(x => x.RefreshTokenExpiresOnUtc)
            .IsRequired();

        builder.Property(x => x.IssuedOnUtc)
            .IsRequired();

        builder.Property(x => x.IsRevoked)
            .IsRequired();

        // Explicit column name so EFCore.NamingConventions does not rename it to "version".
        builder.Property<uint>(VersionPropertyName)
            .IsRowVersion()
            .HasColumnName(VersionColumnName);

        builder.HasOne<User>(x => x.User)
            .WithMany(u => u.Tokens)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UserId);

        builder.HasIndex(x => x.RefreshTokenHash)
            .IsUnique();

        // Supports the UserTokenCleanupJob range delete.
        builder.HasIndex(x => x.RefreshTokenExpiresOnUtc);
    }
}
```

- [ ] **Step 8: Add `SessionChangedConcurrently` and the 409 on the endpoint**

En `AuthenticationErrors.cs`, añadir después de `InvalidCurrentPassword`:

```csharp
    public static readonly BaseError SessionChangedConcurrently = BaseError.Conflict(
        "Auth.SessionChangedConcurrently",
        "The user's sessions changed while the request was processed. Try again.");
```

En `src/Aurora.Flowboard.Api/Endpoints/Authentication/ChangePassword.cs`, añadir después de la línea `.Produces<ProblemDetails>(StatusCodes.Status404NotFound)`:

```csharp
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
```

- [ ] **Step 9: Adapt `LoginHandler`**

En `LoginHandler.cs`, sustituir desde `IdentityToken identityToken = tokenProvider.CreateToken(...)` hasta `return identityToken;` por:

```csharp
        IssuedToken issuedToken = tokenProvider.CreateToken(new TokenRequest(
            user.Id,
            user.Email.Value,
            user.FirstName,
            user.LastName,
            roles));

        Result<UserToken> issueResult = user.IssueToken(
            issuedToken.AccessTokenId,
            issuedToken.RefreshTokenHash,
            issuedToken.Identity.AccessTokenExpiresOn.UtcDateTime,
            issuedToken.Identity.RefreshTokenExpiresOn.UtcDateTime,
            dateTimeProvider.UtcNow);

        if (!issueResult.IsSuccessful)
        {
            return Result.Fail<IdentityToken>(issueResult.Error);
        }

        dbContext.UserTokens.Add(issueResult.Value);

        await dbContext.SaveChangesAsync(cancellationToken);

        return issuedToken.Identity;
```

- [ ] **Step 10: Rewrite `RefreshTokenHandler`**

`src/Aurora.Flowboard.Application/Authentication/RefreshToken/RefreshTokenHandler.cs`:

```csharp
using Aurora.Flowboard.Domain.Shared;

namespace Aurora.Flowboard.Application.Authentication.RefreshToken;

internal sealed class RefreshTokenHandler(
    IApplicationDbContext dbContext,
    ITokenProvider tokenProvider,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<RefreshTokenCommand, IdentityToken>
{
    public async Task<Result<IdentityToken>> Handle(
        RefreshTokenCommand command,
        CancellationToken cancellationToken)
    {
        string refreshTokenHash = tokenProvider.HashRefreshToken(command.RefreshToken);
        DateTime utcNow = dateTimeProvider.UtcNow;

        // Loads only the presented token, not the user's whole token history.
        User? user = await dbContext
            .Users
            .Include(u => u.Roles)
            .Include(u => u.Tokens.Where(t => t.RefreshTokenHash == refreshTokenHash))
            .SingleOrDefaultAsync(
                u => u.Tokens.Any(t => t.RefreshTokenHash == refreshTokenHash),
                cancellationToken);

        UserToken? userToken = user?.Tokens.SingleOrDefault(t => t.RefreshTokenHash == refreshTokenHash);

        // A revoked token here is a replay of an already rotated token: rejected like any other.
        if (user is null || userToken is null || !user.IsActive || !userToken.IsRefreshTokenValid(utcNow))
        {
            return Result.Fail<IdentityToken>(AuthenticationErrors.InvalidRefreshToken);
        }

        Result revokeResult = user.RevokeToken(userToken.UserTokenId);

        if (!revokeResult.IsSuccessful)
        {
            return Result.Fail<IdentityToken>(revokeResult.Error);
        }

        List<string> roles = [.. user.Roles.Select(r => r.Name)];

        IssuedToken issuedToken = tokenProvider.CreateToken(new TokenRequest(
            user.Id,
            user.Email.Value,
            user.FirstName,
            user.LastName,
            roles));

        Result<UserToken> issueResult = user.IssueToken(
            issuedToken.AccessTokenId,
            issuedToken.RefreshTokenHash,
            issuedToken.Identity.AccessTokenExpiresOn.UtcDateTime,
            issuedToken.Identity.RefreshTokenExpiresOn.UtcDateTime,
            utcNow);

        if (!issueResult.IsSuccessful)
        {
            return Result.Fail<IdentityToken>(issueResult.Error);
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another request redeemed the same token between our read and our write (xmin changed).
            // SaveChanges' transaction also rolled back the insert of the new token.
            return Result.Fail<IdentityToken>(AuthenticationErrors.InvalidRefreshToken);
        }

        return issuedToken.Identity;
    }
}
```

- [ ] **Step 11: Rewrite `LogoutHandler`**

`src/Aurora.Flowboard.Application/Authentication/Logout/LogoutHandler.cs`:

```csharp
namespace Aurora.Flowboard.Application.Authentication.Logout;

internal sealed class LogoutHandler(
    IApplicationDbContext dbContext,
    ITokenProvider tokenProvider,
    IUserContext userContext) : ICommandHandler<LogoutCommand>
{
    public async Task<Result> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        string refreshTokenHash = tokenProvider.HashRefreshToken(command.RefreshToken);
        Guid currentUserId = userContext.UserId;

        // Loads only the presented token, and only if it belongs to the caller.
        User? user = await dbContext
            .Users
            .Include(u => u.Tokens.Where(t => t.RefreshTokenHash == refreshTokenHash))
            .SingleOrDefaultAsync(
                u => u.Id == currentUserId && u.Tokens.Any(t => t.RefreshTokenHash == refreshTokenHash),
                cancellationToken);

        UserToken? userToken = user?.Tokens.SingleOrDefault(t => t.RefreshTokenHash == refreshTokenHash);

        if (user is null || userToken is null)
        {
            return Result.Ok();
        }

        // Logout is idempotent: an already revoked token is not an error and needs no write.
        Result revokeResult = user.RevokeToken(userToken.UserTokenId);

        if (!revokeResult.IsSuccessful)
        {
            return Result.Ok();
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // A concurrent refresh or logout changed the row first: the session is closed either way.
            return Result.Ok();
        }

        return Result.Ok();
    }
}
```

- [ ] **Step 12: Rewrite `ChangePasswordHandler`**

`src/Aurora.Flowboard.Application/Authentication/ChangePassword/ChangePasswordHandler.cs`:

```csharp
namespace Aurora.Flowboard.Application.Authentication.ChangePassword;

internal sealed class ChangePasswordHandler(
    IApplicationDbContext dbContext,
    IPasswordHasher passwordHasher,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ICommandHandler<ChangePasswordCommand>
{
    public async Task<Result> Handle(
        ChangePasswordCommand command,
        CancellationToken cancellationToken)
    {
        DateTime utcNow = dateTimeProvider.UtcNow;

        // Only tokens that can still be redeemed; revoked and expired ones need no revocation.
        User? user = await dbContext
            .Users
            .Include(u => u.Tokens.Where(t => !t.IsRevoked && t.RefreshTokenExpiresOnUtc > utcNow))
            .SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Fail(UserErrors.NotFound);
        }

        if (!user.IsActive)
        {
            return Result.Fail(UserErrors.Inactive);
        }

        if (!user.VerifyPassword(passwordHasher, command.CurrentPassword))
        {
            return Result.Fail(AuthenticationErrors.InvalidCurrentPassword);
        }

        string passwordHash = passwordHasher.HashPassword(command.NewPassword);

        Result<Password> passwordResult = Password.Create(passwordHash);
        if (!passwordResult.IsSuccessful)
        {
            return Result.Fail(passwordResult.Error);
        }

        Result changePasswordResult = user.ChangePassword(passwordResult.Value, utcNow);
        if (!changePasswordResult.IsSuccessful)
        {
            return Result.Fail(changePasswordResult.Error);
        }

        Result revokeResult = user.RevokeAllActiveTokens(utcNow);
        if (!revokeResult.IsSuccessful)
        {
            return Result.Fail(revokeResult.Error);
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // A refresh rotated one of these tokens meanwhile, so the rotated token would survive the
            // password change. Nothing was saved; the client retries and the retry revokes it too.
            return Result.Fail(AuthenticationErrors.SessionChangedConcurrently);
        }

        return Result.Ok();
    }
}
```

- [ ] **Step 13: Run the full build and the three test suites**

Run:
```bash
dotnet build "Aurora Flowboard.slnx"
./test/Aurora.Flowboard.Domain.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Domain.UnitTests.exe
./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests.exe
./test/Aurora.Flowboard.ArchitectureTests/bin/Debug/net10.0/Aurora.Flowboard.ArchitectureTests.exe
```
Expected: build limpio, sin warnings, y los tres ejecutables en verde con su número de tests. Si `ArchitectureTests` falla por `IssuedToken`, revisar que es `public sealed record` en el namespace `Aurora.Flowboard.Application.Abstractions.Authentication`, igual que `IdentityToken`.

- [ ] **Step 14: Commit (incluye la Task 2)**

```bash
git add src/Aurora.Flowboard.Domain/Users test/Aurora.Flowboard.Domain.UnitTests/Users src/Aurora.Flowboard.Application src/Aurora.Flowboard.Infrastructure/Authentication/JwtTokenProvider.cs src/Aurora.Flowboard.Infrastructure/Configurations/UserTokenConfiguration.cs src/Aurora.Flowboard.Api/Endpoints/Authentication/ChangePassword.cs test/Aurora.Flowboard.Application.UnitTests/Authentication
git commit -m "Store refresh token hashes and reject concurrent token reuse

Persist the JWT id and the SHA-256 of the refresh token instead of the raw
values, load only the tokens each handler needs, and use xmin as a
concurrency token so a refresh token can be redeemed only once.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 4: Migración `HashRefreshTokens`

**Files:**
- Create: `src/Aurora.Flowboard.Infrastructure/Database/Migrations/<timestamp>_HashRefreshTokens.cs`
- Create: `src/Aurora.Flowboard.Infrastructure/Database/Migrations/<timestamp>_HashRefreshTokens.Designer.cs`
- Modify: `src/Aurora.Flowboard.Infrastructure/Database/Migrations/ApplicationDbContextModelSnapshot.cs`

**Interfaces:**
- Consumes (Task 3): el mapeo de `UserTokenConfiguration`.
- Produces: el esquema `user_tokens` con `access_token_id`, `refresh_token_hash` y los índices nuevos, sin filas previas.

- [ ] **Step 1: Generate the migration**

Run:
```bash
dotnet ef migrations add HashRefreshTokens --project src/Aurora.Flowboard.Infrastructure --startup-project src/Aurora.Flowboard.Api --output-dir Database/Migrations
```
Expected: `Done.` Los dos archivos nuevos quedan en `Database/Migrations/` y se actualiza el snapshot.

- [ ] **Step 2: Check the generated operations**

Abrir `<timestamp>_HashRefreshTokens.cs`. `Up` tiene que contener exactamente estas operaciones sobre `flowboard.user_tokens`, y ninguna para `xmin`:
- `DropIndex` de `ix_user_tokens_refresh_token`.
- `DropColumn` de `access_token` y de `refresh_token`.
- `AddColumn<string>` de `access_token_id` (`character varying(64)`) y de `refresh_token_hash` (`character(64)`, `fixedLength: true`).
- `CreateIndex` de `ix_user_tokens_refresh_token_hash` (`unique: true`) y de `ix_user_tokens_refresh_token_expires_on_utc`.

Si aparece alguna operación sobre `xmin` o `version`, parar: el `HasColumnName("xmin")` de la Task 3 no se está aplicando.

- [ ] **Step 3: Delete the existing sessions before the schema change**

Como primera sentencia de `Up`, antes de cualquier operación generada:

```csharp
            // Existing rows hold raw refresh tokens and JWTs. They cannot be hashed in place, so every
            // session is invalidated and users log in again once.
            migrationBuilder.Sql("DELETE FROM flowboard.user_tokens;");
```

En los dos `AddColumn` de `Up`, quitar el argumento `defaultValue: ""` si EF lo generó. La tabla queda vacía, así que no hace falta, y así la columna no se queda con `DEFAULT ''`.

- [ ] **Step 4: Review the SQL and pending model changes**

Run:
```bash
dotnet ef migrations script AddMilestoneColor HashRefreshTokens --project src/Aurora.Flowboard.Infrastructure --startup-project src/Aurora.Flowboard.Api
dotnet ef migrations has-pending-model-changes --project src/Aurora.Flowboard.Infrastructure --startup-project src/Aurora.Flowboard.Api
dotnet build "Aurora Flowboard.slnx"
```
Expected:
- El script empieza con `DELETE FROM flowboard.user_tokens;`, continúa con `DROP INDEX`, `ALTER TABLE ... DROP COLUMN` (x2), `ADD COLUMN access_token_id character varying(64) NOT NULL`, `ADD COLUMN refresh_token_hash character(64) NOT NULL`, y los dos `CREATE INDEX`. No menciona `xmin`.
- `has-pending-model-changes` responde que no hay cambios pendientes.
- Build limpio.

- [ ] **Step 5: Verify against a real database (Review Focus 1, 3 y 5)**

1. Antes de arrancar la rama nueva, con la base de datos del AppHost tal como está, hacer `POST /api/v1/flowboard/auth/login` y guardar el `refreshToken` como `OLD_RT`.
2. Arrancar la rama nueva: `dotnet run --project "src/Aurora.Flowboard.AppHost"`. La migración se aplica al arrancar.
3. `POST auth/refresh-token` con `OLD_RT` → **401** con `Auth.InvalidRefreshToken`; no 500 (Review Focus 3).
4. `POST auth/login` → en psql, `SELECT access_token_id, refresh_token_hash, length(refresh_token_hash) FROM flowboard.user_tokens;` → una fila con el hash de 64 caracteres; ninguna columna con el token en claro. Decodificar el `accessToken` (por ejemplo, la parte central en base64) y comprobar que su claim `jti` es igual a `access_token_id` (Review Focus 5).
5. Lanzar dos refresh a la vez con el mismo token (Review Focus 1):
   ```bash
   RT="<refreshToken del login>"
   for i in 1 2; do curl -s -o /dev/null -w "%{http_code}\n" -X POST "$API/api/v1/flowboard/auth/refresh-token" -H "Content-Type: application/json" -d "{\"refreshToken\":\"$RT\"}" & done; wait
   ```
   Expected: un `200` y un `401`. Si salen dos `200`, el concurrency token no está mapeado: revisar el SQL del `UPDATE` en el log, que debe llevar `AND xmin = @p`.
6. Repetir el refresh con el `RT` ya usado → `401`.

- [ ] **Step 6: Commit**

```bash
git add src/Aurora.Flowboard.Infrastructure/Database/Migrations
git commit -m "Add HashRefreshTokens migration

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 5: Job `UserTokenCleanup`

**Files:**
- Create: `src/Aurora.Flowboard.Infrastructure/Authentication/UserTokenCleanupOptions.cs`
- Create: `src/Aurora.Flowboard.Infrastructure/Authentication/UserTokenCleanupJob.cs`
- Create: `src/Aurora.Flowboard.Infrastructure/Authentication/ConfigureUserTokenCleanupJob.cs`
- Modify: `src/Aurora.Flowboard.Infrastructure/DependencyInjection.cs:16-27,120-126`
- Modify: `src/Aurora.Flowboard.Api/appsettings.json`

**Interfaces:**
- Consumes (Task 3): índice sobre `RefreshTokenExpiresOnUtc`; `ApplicationDbContext.UserTokens`.
- Produces: `UserTokenCleanupOptions { const string SectionName = "UserTokenCleanup"; int IntervalInHours; int RetentionDays; int BatchSize; }`.

- [ ] **Step 1: Add the options**

`src/Aurora.Flowboard.Infrastructure/Authentication/UserTokenCleanupOptions.cs`:

```csharp
namespace Aurora.Flowboard.Infrastructure.Authentication;

public sealed class UserTokenCleanupOptions
{
    public const string SectionName = "UserTokenCleanup";

    public int IntervalInHours { get; init; }
    public int RetentionDays { get; init; }
    public int BatchSize { get; init; }
}
```

`src/Aurora.Flowboard.Api/appsettings.json`, añadir después de la sección `Outbox`:

```json
  "UserTokenCleanup": {
    "IntervalInHours": 24,
    "RetentionDays": 7,
    "BatchSize": 1000
  }
```

(Añadir la coma que corresponda tras el cierre de `Outbox`.)

- [ ] **Step 2: Add the job**

`src/Aurora.Flowboard.Infrastructure/Authentication/UserTokenCleanupJob.cs`:

```csharp
using Aurora.Flowboard.Infrastructure.Database;
using Microsoft.Extensions.Logging;

namespace Aurora.Flowboard.Infrastructure.Authentication;

/// <summary>
/// Deletes user tokens whose refresh token expired more than <see cref="UserTokenCleanupOptions.RetentionDays"/>
/// ago. Revoked tokens expire on the same schedule as active ones, so expiry alone covers both.
/// Rows are deleted in batches to avoid one long-running DELETE locking the table.
/// </summary>
[DisallowConcurrentExecution]
internal sealed class UserTokenCleanupJob(
    ApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IOptions<UserTokenCleanupOptions> options,
    ILogger<UserTokenCleanupJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        UserTokenCleanupOptions cleanupOptions = options.Value;
        DateTime cutoff = dateTimeProvider.UtcNow.AddDays(-cleanupOptions.RetentionDays);

        int totalDeleted = 0;
        int deleted;

        do
        {
            deleted = await dbContext.UserTokens
                .Where(t => t.RefreshTokenExpiresOnUtc < cutoff)
                .OrderBy(t => t.RefreshTokenExpiresOnUtc)
                .Take(cleanupOptions.BatchSize)
                .ExecuteDeleteAsync(context.CancellationToken);

            totalDeleted += deleted;
        }
        while (deleted == cleanupOptions.BatchSize);

        logger.LogInformation("Deleted {Count} user tokens that expired before {Cutoff}.", totalDeleted, cutoff);
    }
}
```

- [ ] **Step 3: Add the Quartz configuration and register it**

`src/Aurora.Flowboard.Infrastructure/Authentication/ConfigureUserTokenCleanupJob.cs`:

```csharp
namespace Aurora.Flowboard.Infrastructure.Authentication;

internal sealed class ConfigureUserTokenCleanupJob(IOptions<UserTokenCleanupOptions> options) : IConfigureOptions<QuartzOptions>
{
    private readonly UserTokenCleanupOptions _cleanupOptions = options.Value;

    public void Configure(QuartzOptions options)
    {
        string jobName = typeof(UserTokenCleanupJob).FullName!;

        options
            .AddJob<UserTokenCleanupJob>(cfg => cfg.WithIdentity(jobName))
            .AddTrigger(cfg => cfg
                .ForJob(jobName)
                .WithSimpleSchedule(s => s
                    .WithIntervalInHours(_cleanupOptions.IntervalInHours)
                    .RepeatForever()));
    }
}
```

En `DependencyInjection.cs`, añadir `.AddUserTokenCleanupJob()` a la cadena de `AddInfrastructureServices`, justo antes de `.AddQuartzServices()`, y el método después de `AddOutboxPatternImplementation`:

```csharp
    private static IServiceCollection AddUserTokenCleanupJob(this IServiceCollection services)
    {
        services.AddOptions<UserTokenCleanupOptions>().BindConfiguration(UserTokenCleanupOptions.SectionName);
        services.ConfigureOptions<ConfigureUserTokenCleanupJob>();

        return services;
    }
```

- [ ] **Step 4: Build and run the architecture tests**

Run:
```bash
dotnet build "Aurora Flowboard.slnx"
./test/Aurora.Flowboard.ArchitectureTests/bin/Debug/net10.0/Aurora.Flowboard.ArchitectureTests.exe
```
Expected: build limpio; tests de arquitectura en verde.

- [ ] **Step 5: Run the job once and read its SQL**

Arrancar con `dotnet run --project "src/Aurora.Flowboard.AppHost"`. El trigger dispara la primera ejecución al arrancar. En los logs de la API (dashboard de Aspire):
- El `DELETE` traducido debe acotar el lote, con `LIMIT` dentro de una subconsulta.
- Debe aparecer `Deleted N user tokens that expired before ...`.

**Si la traducción falla** con `InvalidOperationException ... could not be translated`, sustituir el bucle `do` por SQL crudo, siguiendo el mismo patrón que `ProcessOutboxJob`:

```csharp
        do
        {
            deleted = await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                DELETE FROM flowboard.user_tokens
                WHERE user_token_id IN (
                    SELECT user_token_id
                    FROM flowboard.user_tokens
                    WHERE refresh_token_expires_on_utc < {cutoff}
                    ORDER BY refresh_token_expires_on_utc
                    LIMIT {cleanupOptions.BatchSize})
                """,
                context.CancellationToken);

            totalDeleted += deleted;
        }
        while (deleted == cleanupOptions.BatchSize);
```

- [ ] **Step 6: Verify it never deletes live tokens (Review Focus 4)**

1. Hacer `POST auth/login` dos veces, para tener dos filas.
2. En psql, envejecer solo una de ellas: `UPDATE flowboard.user_tokens SET refresh_token_expires_on_utc = now() - interval '8 days', access_token_expires_on_utc = now() - interval '9 days', issued_on_utc = now() - interval '10 days' WHERE user_token_id = '<id de una fila>';`
3. Reiniciar la API (el job se ejecuta al arrancar).
4. `SELECT user_token_id FROM flowboard.user_tokens;` → solo queda la fila no envejecida. Su refresh token sigue funcionando: `POST auth/refresh-token` → 200.

- [ ] **Step 7: Commit**

```bash
git add src/Aurora.Flowboard.Infrastructure/Authentication src/Aurora.Flowboard.Infrastructure/DependencyInjection.cs src/Aurora.Flowboard.Api/appsettings.json
git commit -m "Add a Quartz job that deletes expired user tokens

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 6: Rule de autenticación y verificación final de la rama

**Files:**
- Create: `.claude/rules/authentication.md`
- Modify: `CLAUDE.md` (tabla "Area-specific rules")

**Interfaces:**
- Consumes: todo lo anterior.

- [ ] **Step 1: Write the rule**

`.claude/rules/authentication.md`:

```markdown
---
paths:
  - "src/Aurora.Flowboard.Application/Authentication/**/*.cs"
  - "src/Aurora.Flowboard.Infrastructure/Authentication/**/*.cs"
  - "src/Aurora.Flowboard.Infrastructure/Configurations/UserTokenConfiguration.cs"
  - "src/Aurora.Flowboard.Domain/Users/**/*.cs"
---

# Authentication and user tokens

## No secrets in `user_tokens`

- The refresh token is stored only as `RefreshTokenHash`: SHA-256, lowercase hex, 64 chars, unique index. Handlers hash the presented value with `ITokenProvider.HashRefreshToken` and look up by hash. The token is 64 random bytes, so no salt or pepper is needed.
- The JWT is never stored; `AccessTokenId` holds its `jti` so access tokens can be revoked later without storing them.
- `ITokenProvider.CreateToken` returns `IssuedToken` (the `IdentityToken` for the client plus the id and the hash to persist). Do not add persistence fields to `IdentityToken`: it is serialized as the login/refresh response body.

## Redeeming a refresh token once

- `UserToken` has a shadow `uint` property `Version` mapped to Postgres' `xmin` (`IsRowVersion()` + explicit `HasColumnName("xmin")`, otherwise EFCore.NamingConventions renames it). It produces no migration column.
- Refresh, logout and change password catch `DbUpdateConcurrencyException` on `SaveChangesAsync`:
  - refresh → `InvalidRefreshToken` (another request redeemed the token first);
  - logout → `Ok` (the session is closed either way);
  - change password → `SessionChangedConcurrently` (409), so the client retries and the retry revokes the rotated token.
- A replayed (already revoked) refresh token is only rejected. It does not revoke the user's other sessions or a token family; `LoggingBehavior` already logs the failure code.

## Load only the tokens you need

Never `.Include(u => u.Tokens)` unfiltered: the history grows with every login and refresh. Use a filtered include — the presented token (`t.RefreshTokenHash == hash`) or the active ones (`!t.IsRevoked && t.RefreshTokenExpiresOnUtc > utcNow`). `MockDbSetHelper` ignores `Include`, so check the SQL log when changing these queries.

## Cleanup

`UserTokenCleanupJob` (Quartz, section `UserTokenCleanup`) deletes in batches the rows whose `RefreshTokenExpiresOnUtc` is older than `RetentionDays`. Revoked tokens expire on the same schedule, so there is no `RevokedOnUtc` column.

## Status codes

- `InvalidCredentials` and `InvalidRefreshToken` are `BaseErrorType.Unauthorized` → 401.
- A wrong current password in change password is `InvalidCurrentPassword` (`Validation`, 400), not 401: the caller is authenticated, and a 401 would make clients with automatic session renewal believe the session expired.
```

- [ ] **Step 2: Register the rule in `CLAUDE.md`**

En la tabla "Area-specific rules" de `CLAUDE.md`, añadir después de la fila de `domain-model.md`:

```markdown
| `authentication.md` | Refresh token hashing, `jti`, `xmin` concurrency on `user_tokens`, token cleanup job, 401 vs 400 |
```

- [ ] **Step 3: Full build and tests**

Run:
```bash
dotnet build "Aurora Flowboard.slnx"
./test/Aurora.Flowboard.Domain.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Domain.UnitTests.exe
./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests.exe
./test/Aurora.Flowboard.ArchitectureTests/bin/Debug/net10.0/Aurora.Flowboard.ArchitectureTests.exe
```
Expected: build limpio; anotar cuántos tests pasan y cuántos fallan en cada ejecutable. Todos en verde.

- [ ] **Step 4: Search for leftovers**

Run:
```bash
grep -rn "\.AccessToken\b\|\.RefreshToken\b\|AccessTokenRequired\|RefreshTokenRequired\|MaxAccessTokenLength\|Include(u => u.Tokens)" --include=*.cs src test | grep -v "/bin/\|/obj/\|Migrations/2026082\|Migrations/2026091\|Identity\.\|identityToken\.\|command\.RefreshToken\|request\.RefreshToken"
```
Expected: ninguna coincidencia. Solo pueden aparecer `IdentityToken.AccessToken` / `RefreshToken` (respuesta HTTP), `command.RefreshToken` / `request.RefreshToken` (entrada) y las migraciones antiguas, que ya están filtradas.

- [ ] **Step 5: Run the review agents**

Lanzar el agente `arch-guard` y el agente `code-reviewer` sobre el diff de la rama contra `staging` (`git diff staging...HEAD`). Corregir lo que confirmen, volver al Step 3 y commitear las correcciones.

- [ ] **Step 6: Manual end-to-end check (Review Focus 1)**

Con `dotnet run --project "src/Aurora.Flowboard.AppHost"`:
1. `login` → 200.
2. `refresh-token` → 200.
3. `refresh-token` con el token anterior → 401.
4. Dos refresh en paralelo con el mismo token → un 200 y un 401 (el script de la Task 4, paso 5).
5. `logout` con el token vigente → 204; un `refresh-token` posterior con ese token → 401.
6. `change-password` con la contraseña actual incorrecta → 400 `Auth.InvalidCurrentPassword`; con la correcta → 204, y luego cualquier refresh token previo → 401.

- [ ] **Step 7: Check the filtered includes in the SQL log (Review Focus 2)**

En los logs de EF del paso anterior (dashboard de Aspire, categoría `Microsoft.EntityFrameworkCore.Database.Command`), comprobar que:
- El SELECT de refresh y logout lleva el `LEFT JOIN` a `user_tokens` con `WHERE ... refresh_token_hash = @...` dentro de la subconsulta del include.
- El de change password lleva `NOT is_revoked AND refresh_token_expires_on_utc > @...`.
- Ninguno carga los tokens sin filtrar.

- [ ] **Step 8: Commit**

```bash
git add .claude/rules/authentication.md CLAUDE.md
git commit -m "Document authentication and user token conventions

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

- [ ] **Step 9: Hand off to the user**

Informar del resultado de los tres ejecutables de tests y de las verificaciones manuales. **No hacer push ni abrir el PR sin confirmación del usuario.** Cuando el PR exista:
- Pasar I3, I4 y M11 a "En curso" en la página de seguimiento (https://claude.ai/artifact/HgWcansbBYbngpQjsF5u2s).
- Anotar el número de PR en las notas de cada uno.
