# Endurecimiento de refresh tokens — Diseño

**Fecha:** 2026-10-07
**Estado:** Aprobado para implementación
**Rama:** `fix/refresh-token-hardening`
**Puntos de la revisión:** I3, I4, M11 (ver `docs/review-remediation-plan.md` y la página de seguimiento)

## 1. Problema

- **I3.** `user_tokens` guarda el refresh token en claro, con índice único sobre ese valor, y el JWT completo (`access_token`, 2000 caracteres) que nunca se vuelve a leer. `RefreshTokenHandler` y `LogoutHandler` buscan por igualdad sobre el valor crudo. Un acceso de lectura a la base de datos (backup, consola SQL) entrega sesiones vivas de 7 días.
- **I4.** `RefreshTokenHandler` lee el token, comprueba `IsRevoked` y lo revoca con el change tracker, sin concurrency token: dos refresh concurrentes con el mismo token ven `IsRevoked = false` y emiten dos pares nuevos. `RefreshToken`, `Logout` y `ChangePassword` hacen `.Include(u => u.Tokens)` y cargan todo el histórico, que nunca se purga; `RevokeAllActiveTokens` revoca también tokens ya expirados (y emite un evento por cada uno).
- **M11.** `InvalidCredentials` e `InvalidRefreshToken` usan `BaseError.Forbidden` → 403. Los clientes no pueden distinguir "no autenticado" de "sin permiso".

Fuera de alcance: M12 (iteraciones de PBKDF2), que va en otra rama; la validación del JWT contra la BD (`OnTokenValidated`); la detección de reutilización por familia de tokens.

## 2. Decisiones

| # | Decisión | Motivo |
|---|----------|--------|
| 1 | Alcance I3 + I4 + M11 | M11 toca los mismos errores de autenticación; M12 es del hash de contraseñas, no del modelo de tokens. |
| 2 | Guardar `SHA-256` (hex minúscula) del refresh token y buscar por hash | El token tiene 64 bytes aleatorios: un hash sin sal ni pepper no admite diccionario ni fuerza bruta. |
| 3 | Sustituir el JWT almacenado por su `jti` (`AccessTokenId`) | Nada lee el JWT; el `jti` deja la puerta abierta a revocar access tokens sin guardar un secreto. |
| 4 | La migración borra las filas existentes de `user_tokens` | Todos vuelven a iniciar sesión una vez. Más simple y seguro que reproducir el hash en SQL; coste bajo para una API interna. |
| 5 | Concurrencia con `xmin` como concurrency token de `UserToken` (shadow property) | Mantiene el dominio (`Revoke()`) y sus eventos, se prueba con NSubstitute y no bloquea filas. Adelanta I9 solo para `user_tokens`; I9 generalizará el mismo patrón. |
| 6 | Un replay solo se rechaza (`InvalidRefreshToken`) | Es lo que pide I4. Revocar todas las sesiones o la familia del token queda fuera de alcance. |
| 7 | Sin logger propio en `RefreshTokenHandler` | `LoggingBehavior` ya registra un warning con el código de error. |
| 8 | Cargar solo los tokens necesarios con `Include` filtrado | El coste de refresh, logout y cambio de contraseña deja de crecer con el histórico. |
| 9 | Purga con un job Quartz cada 24 h, 7 días de margen, borrado por lotes | Reutiliza la infraestructura del outbox; el margen conserva rastro reciente para auditar. |
| 10 | La purga usa `RefreshTokenExpiresOnUtc < UtcNow - RetentionDays` | Un token revocado caduca igual que uno activo (como mucho vive 7 + 7 días), así que no hace falta una columna `RevokedOnUtc`. |
| 11 | Nueva categoría `BaseErrorType.Unauthorized` → 401 | M11. |
| 12 | `ChangePassword` con la contraseña actual incorrecta devuelve `InvalidCurrentPassword` (Validation, 400) | Con 401, un cliente con renovación automática creería que la sesión caducó. El usuario está autenticado: falla un dato de la petición. |
| 13 | `ChangePassword` con conflicto de concurrencia devuelve `SessionChangedConcurrently` (Conflict, 409) | Si un refresh rota un token mientras se cambia la contraseña, el token nuevo sobreviviría. El cliente reintenta y el reintento lo revoca. |
| 14 | El contrato HTTP de `IdentityToken` no cambia | `IdentityToken` se serializa tal cual en las respuestas de login y refresh; los datos internos viajan en un tipo nuevo. |

## 3. Dominio

### 3.1 `UserToken`

- Se eliminan `AccessToken`, `RefreshToken` y `MaxAccessTokenLength`.
- Se añaden:
  - `string AccessTokenId` — el `jti`; constante `MaxAccessTokenIdLength = 64`.
  - `string RefreshTokenHash` — constante `RefreshTokenHashLength = 64`.
- Se mantiene `MaxRefreshTokenLength = 500`: los validadores de `RefreshTokenCommand` y `LogoutCommand` lo usan para acotar la entrada en claro.
- `Create(...)` y el constructor privado cambian sus parámetros en consecuencia.
- La entidad no expone nada de concurrencia; `xmin` vive solo en la configuración de EF.

### 3.2 `User`

```csharp
public Result<UserToken> IssueToken(
    string accessTokenId,
    string refreshTokenHash,
    DateTime accessTokenExpiresOnUtc,
    DateTime refreshTokenExpiresOnUtc,
    DateTime issuedOnUtc)
```

- Valida `accessTokenId` y `refreshTokenHash` no vacíos, y las fechas como hoy.

```csharp
public Result RevokeAllActiveTokens(DateTime utcNow)
```

- Revoca solo los tokens con `!IsRevoked && RefreshTokenExpiresOnUtc > utcNow`, y emite un `UserTokenRevokedDomainEvent` por cada uno. Es correcto aunque se cargue la colección completa.

### 3.3 `UserTokenErrors`

- `AccessTokenRequired` → `AccessTokenIdRequired` (`"UserToken.AccessTokenIdRequired"`).
- `RefreshTokenRequired` → `RefreshTokenHashRequired` (`"UserToken.RefreshTokenHashRequired"`).

### 3.4 `BaseError` / `BaseErrorType`

- Nuevo valor `BaseErrorType.Unauthorized` y fábrica `BaseError.Unauthorized(string code, string message)`.

## 4. Application

### 4.1 Abstracciones (`Application/Abstractions/Authentication/`)

Nuevo record:

```csharp
public sealed record IssuedToken(
    IdentityToken Identity,
    string AccessTokenId,
    string RefreshTokenHash);
```

`ITokenProvider`:

```csharp
public interface ITokenProvider
{
    IssuedToken CreateToken(TokenRequest tokenRequest);

    string HashRefreshToken(string refreshToken);
}
```

`IdentityToken` no cambia.

### 4.2 `AuthenticationErrors`

| Error | Categoría | HTTP |
|---|---|---|
| `InvalidCredentials` | `Unauthorized` (antes `Forbidden`) | 401 |
| `InvalidRefreshToken` | `Unauthorized` (antes `Forbidden`) | 401 |
| `InvalidCurrentPassword` (nuevo, `"Auth.InvalidCurrentPassword"`) | `Validation` | 400 |
| `SessionChangedConcurrently` (nuevo, `"Auth.SessionChangedConcurrently"`) | `Conflict` | 409 |
| `NewPasswordMustDiffer` | `Validation` (sin cambios) | 400 |

### 4.3 `RefreshTokenHandler`

1. `string hash = tokenProvider.HashRefreshToken(command.RefreshToken);`
2. Una sola consulta, que sustituye las dos actuales:
   ```csharp
   User? user = await dbContext.Users
       .Include(u => u.Roles)
       .Include(u => u.Tokens.Where(t => t.RefreshTokenHash == hash))
       .SingleOrDefaultAsync(u => u.Tokens.Any(t => t.RefreshTokenHash == hash), cancellationToken);
   ```
3. Devuelve `InvalidRefreshToken`, sin llamar a `SaveChanges`, si no hay usuario, si el usuario está inactivo o si el token no es válido (`IsRefreshTokenValid(utcNow)` falso: revocado — replay — o caducado).
4. `user.RevokeToken(token.UserTokenId)` → `tokenProvider.CreateToken(...)` → `user.IssueToken(issued.AccessTokenId, issued.RefreshTokenHash, ...)`.
5. `SaveChangesAsync` dentro de `try`; `catch (DbUpdateConcurrencyException)` → `InvalidRefreshToken`. Otra petición canjeó el mismo token en paralelo; la transacción implícita de `SaveChanges` deshace también el insert del token nuevo.
6. Devuelve `issued.Identity`.

### 4.4 `LogoutHandler`

- Calcula el hash y usa la misma consulta, añadiendo `u.Id == userContext.UserId` al predicado.
- Sigue siendo idempotente: devuelve `Ok` si el token no existe, pertenece a otro usuario o ya está revocado (se ignora el resultado de `RevokeToken`, como hoy), y también ante `DbUpdateConcurrencyException` (otro proceso ya lo revocó).

### 4.5 `ChangePasswordHandler`

- Carga solo los tokens activos:
  ```csharp
  DateTime utcNow = dateTimeProvider.UtcNow;
  .Include(u => u.Tokens.Where(t => !t.IsRevoked && t.RefreshTokenExpiresOnUtc > utcNow))
  ```
- Con la contraseña actual incorrecta devuelve `InvalidCurrentPassword` (en lugar de `InvalidCredentials`).
- Llama a `user.RevokeAllActiveTokens(utcNow)`.
- `SaveChangesAsync` dentro de `try`; `catch (DbUpdateConcurrencyException)` → `SessionChangedConcurrently`.

### 4.6 `LoginHandler`

- Solo se adapta a `IssuedToken`: `user.IssueToken(issued.AccessTokenId, issued.RefreshTokenHash, ...)` y devuelve `issued.Identity`. Sigue con `AsNoTracking()` y `dbContext.UserTokens.Add(...)` explícito.

### 4.7 Validadores

Sin cambios: `RefreshTokenValidator` y `LogoutValidator` siguen acotando con `UserToken.MaxRefreshTokenLength`.

## 5. Infrastructure

### 5.1 `JwtTokenProvider`

- Genera el `jti` una sola vez y lo usa tanto en el claim como en `IssuedToken.AccessTokenId`.
- `HashRefreshToken(string refreshToken)` → `Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)))`.
- `CreateToken` devuelve `new IssuedToken(identity, jti, HashRefreshToken(refreshToken))`.

### 5.2 `UserTokenConfiguration`

- `AccessTokenId`: requerido, `HasMaxLength(UserToken.MaxAccessTokenIdLength)`.
- `RefreshTokenHash`: requerido, `HasMaxLength(UserToken.RefreshTokenHashLength).IsFixedLength()`, índice único.
- Índice sobre `RefreshTokenExpiresOnUtc` (para la purga).
- Concurrency token:
  ```csharp
  builder.Property<uint>("Version")
      .IsRowVersion()
      .HasColumnName("xmin");
  ```
  `HasColumnName("xmin")` es explícito para que `EFCore.NamingConventions` no lo convierta en `version`. Npgsql no genera operaciones de migración para `xmin` (columna de sistema).

### 5.3 Migración `HashRefreshTokens`

En este orden:
1. `DELETE FROM flowboard.user_tokens;` (`migrationBuilder.Sql`, antes de las operaciones de esquema).
2. Eliminar el índice único sobre `refresh_token` y las columnas `access_token` y `refresh_token`.
3. Añadir `access_token_id varchar(64) NOT NULL` y `refresh_token_hash char(64) NOT NULL`.
4. Crear el índice único sobre `refresh_token_hash` y el índice sobre `refresh_token_expires_on_utc`.

`Down` recrea las columnas antiguas sin datos; las sesiones no se recuperan en ningún sentido.

### 5.4 Job de purga (`Infrastructure/Authentication/`)

- `UserTokenPurgeOptions` (sección `UserTokenPurge`): `IntervalInHours`, `RetentionDays`, `BatchSize`.
- `appsettings.json`:
  ```json
  "UserTokenPurge": {
    "IntervalInHours": 24,
    "RetentionDays": 7,
    "BatchSize": 1000
  }
  ```
- `ConfigurePurgeExpiredUserTokensJob : IConfigureOptions<QuartzOptions>`: `SimpleSchedule` con `WithIntervalInHours(...)`.`RepeatForever()`, igual que `ConfigureProcessOutboxJob`. La primera ejecución ocurre al arrancar.
- `PurgeExpiredUserTokensJob`, marcado con `[DisallowConcurrentExecution]`:
  - `cutoff = UtcNow - RetentionDays`.
  - En bucle, borra hasta `BatchSize` filas con `RefreshTokenExpiresOnUtc < cutoff` mediante `ExecuteDeleteAsync`; si Npgsql no traduce `Take` dentro de `ExecuteDelete`, usa SQL crudo `DELETE ... WHERE user_token_id IN (SELECT user_token_id ... LIMIT n)` como el outbox.
  - Termina cuando un lote borra menos de `BatchSize` filas, y registra un `LogInformation` con el total borrado.
- El borrado no pasa por el dominio ni emite eventos: son filas muertas, no revocaciones.
- Con varias instancias de la API cada una ejecuta su job; el DELETE es idempotente.
- Se registra en `DependencyInjection` junto a la configuración del outbox.

## 6. Api

- `ApiResponses`: `BaseErrorType.Unauthorized` → `401`, type `https://tools.ietf.org/html/rfc7235#section-3.1`.
- `Login` y `RefreshToken`: `.Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)` en lugar de `403`.
- `ChangePassword`: declara `204`, `400`, `401` (no autenticado, vía `RequireAuthorization`), `404`, `409` y `500`; se retira `403`, porque tras el cambio ningún camino lo produce (`InvalidCurrentPassword` y `UserErrors.Inactive` son `Validation`, `UserErrors.NotFound` es 404).

## 7. Pruebas

### 7.1 Dominio (`UserTests` / `UserData`)

- `IssueToken` falla con `AccessTokenIdRequired` y `RefreshTokenHashRequired`; sigue validando las fechas.
- `RevokeAllActiveTokens(utcNow)` revoca solo los tokens activos y no caducados, y emite exactamente un evento por cada uno.
- Ajustar los tests existentes que usan `AccessToken` / `RefreshToken`.

### 7.2 Application

- `RefreshTokenHandlerTests`:
  - busca por el hash que devuelve `HashRefreshToken`;
  - revocado (replay), caducado o usuario inactivo → `InvalidRefreshToken` con `ErrorType == Unauthorized`, sin `SaveChanges`;
  - `SaveChangesAsync` lanza `DbUpdateConcurrencyException` → `InvalidRefreshToken`;
  - el token nuevo lleva el `AccessTokenId` y el `RefreshTokenHash` de `IssuedToken`;
  - la respuesta es `issued.Identity`.
- `LogoutHandlerTests`: busca por hash; `Ok` si el token es desconocido, de otro usuario, ya revocado o ante `DbUpdateConcurrencyException`.
- `ChangePasswordHandlerTests`: contraseña actual incorrecta → `InvalidCurrentPassword`; `DbUpdateConcurrencyException` → `SessionChangedConcurrently`; solo se revocan los tokens activos.
- `LoginHandlerTests`: credenciales inválidas → `ErrorType == Unauthorized`; se añade un token con `AccessTokenId` y `RefreshTokenHash`.

### 7.3 Lo que los tests unitarios no cubren

`MockDbSetHelper` usa LINQ-to-Objects (ignora los `Include` filtrados y no traduce a SQL) y no hay proyecto de tests de Infrastructure. Verificación manual con Aspire (`dotnet run --project src/Aurora.Flowboard.AppHost`):

1. `dotnet ef migrations script` y revisar el SQL: borra filas, elimina columnas y crea los índices.
2. `POST auth/login` → en `user_tokens` solo hay hash y `jti`, ningún valor en claro.
3. `POST auth/refresh-token` dos veces con el mismo token → 200 y luego 401.
4. Dos `refresh-token` en paralelo con el mismo token → uno 200 y otro 401.
5. `POST auth/logout`; `change-password` con la contraseña actual incorrecta → 400; revisar en el log de SQL que los `Include` filtrados generan el `WHERE` esperado.
6. Job de purga con `RetentionDays: 0` → borra las filas caducadas.

## 8. Cierre de la rama

- `dotnet build "Aurora Flowboard.slnx"` y los tres ejecutables de tests, informando cuántos pasan y cuántos fallan.
- Agentes `arch-guard` y `code-reviewer`.
- Nuevo rule `.claude/rules/authentication.md` con: hash del refresh token, `jti`, `xmin` en `user_tokens`, el replay solo se rechaza, la purga y su criterio, y 401 frente a 400 en `ChangePassword`.
- Al abrir el PR: I3, I4 y M11 pasan a "En curso" en la página de seguimiento, con el número de PR en las notas.
- No incluir en los commits los cambios locales ajenos (`appsettings.Development.json`, `.claude/settings.local.json`).
