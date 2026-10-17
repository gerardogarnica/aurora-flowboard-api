# Concurrencia optimista y conflictos de persistencia — Plan de implementación

**Fecha:** 2026-10-16
**Estado:** Aprobado
**Tickets:** I9
**Rama:** `fix/optimistic-concurrency` · **PR:** —
**Spec:** `docs/specs/2026-10-16-optimistic-concurrency-design.md`

**Objetivo:** que las carreras entre peticiones devuelvan 409 en vez de 500 o de romper invariantes (proyecto sin admin, milestone cerrado o componente retirado con trabajo abierto, sistema sin Administrator).

**Arquitectura:** `xmin` de Postgres como concurrency token en los aggregate roots editables (`Project`, `Milestone`, `Component`, `WorkItem`, `User`); transacciones `Serializable` (SSI) en los handlers con *write skew* entre agregados o filas; y `PersistenceConflictBehavior`, el decorador más interno de los comandos, que traduce `DbUpdateConcurrencyException`, `40001` y `23505` a un `Result` con `BaseError.Conflict` (409). Decisión registrada en `docs/architecture/adr/0001-optimistic-concurrency.md`.

## Restricciones globales

- Rama `fix/optimistic-concurrency`; el PR va a `staging`.
- Reglas de `CLAUDE.md`: no añadir capas ni frameworks; pasar `CancellationToken` en toda llamada asíncrona; sin sync-over-async.
- `Directory.Build.props` trata los warnings como errores (SonarAnalyzer, `AnalysisMode` `All`): cada build tiene que salir limpio.
- **Nunca `dotnet test`.** Compilar el proyecto de tests y ejecutar su `.exe` de `test/*/bin/Debug/net10.0/`; comprobar que el runner imprime un número de tests.
- Rules aplicables: `.claude/rules/ef-core-queries.md` (handlers cargan con tracking, SQL crudo permitido), `.claude/rules/unit-tests.md` (asignar el `DbSet` simulado a una local antes de `Returns`), `.claude/rules/authentication.md`, `.claude/rules/milestones-components.md`.
- Los handlers no capturan `DbUpdateConcurrencyException` ni `DbUpdateException` salvo `RefreshTokenHandler` y `LogoutHandler`, que no se tocan.
- `IsolationLevel` es `System.Data.IsolationLevel`; `IDbContextTransaction` es `Microsoft.EntityFrameworkCore.Storage` (global en Application, no en los tests).
- Cada tarea se implementa siguiendo la skill indicada en su línea **Skill:**. Invócala (Skill tool) antes de escribir el código de la tarea, aunque empieces sin contexto.
- Se ejecuta con la skill `execute-plan`. Aprobar el plan (`**Estado:** Aprobado`) autoriza un commit por tarea; nunca push ni PR sin que el usuario lo pida.
- Añadir los ficheros al commit uno por uno (nunca `git add -A`). No incluir nunca `src/Aurora.Flowboard.Api/appsettings.Development.json` ni `.claude/settings.local.json` (cambios locales del usuario).
- Cada commit termina con la línea `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- Si la API está corriendo, el build falla con `MSB3021`/`MSB3027` (fichero bloqueado): pedir al usuario que la detenga.

Rutas de los ejecutables de tests (desde la raíz del repo):

```bash
DOMAIN_TESTS=./test/Aurora.Flowboard.Domain.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Domain.UnitTests.exe
APP_TESTS=./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests.exe
ARCH_TESTS=./test/Aurora.Flowboard.ArchitectureTests/bin/Debug/net10.0/Aurora.Flowboard.ArchitectureTests.exe
```

## Mapa de ficheros

| Fichero | Acción | Responsabilidad |
|---|---|---|
| `src/Aurora.Flowboard.Domain/Projects/Project.cs` | Modificar | `AddMember` fija `UpdatedOnUtc` para que el alta escriba la fila del root |
| `test/Aurora.Flowboard.Domain.UnitTests/Projects/ProjectTests.cs` | Modificar | Test de `AddMember` → `UpdatedOnUtc` |
| `src/Aurora.Flowboard.Application/Abstractions/Data/PersistenceErrors.cs` | Crear | Errores `Conflict` de persistencia y constantes de SqlState |
| `src/Aurora.Flowboard.Application/Abstractions/Behaviors/PersistenceConflictBehavior.cs` | Crear | Decorador de comandos que traduce las excepciones de conflicto a `Result` |
| `src/Aurora.Flowboard.Application/DependencyInjection.cs` | Modificar | Registra el behavior como decorador más interno |
| `test/Aurora.Flowboard.Application.UnitTests/Abstractions/FakeDbException.cs` | Crear | `DbException` con SqlState configurable para los tests |
| `test/Aurora.Flowboard.Application.UnitTests/Abstractions/Behaviors/PersistenceConflictBehaviorTests.cs` | Crear | Tests del behavior (ambas variantes) |
| `src/Aurora.Flowboard.Application/Abstractions/Data/IApplicationDbContext.cs` | Modificar | Sobrecarga `BeginTransactionAsync(IsolationLevel, …)` |
| `src/Aurora.Flowboard.Infrastructure/Database/ApplicationDbContext.cs` | Modificar | Implementa la sobrecarga |
| `src/Aurora.Flowboard.Application/Milestones/ChangeStatus/ChangeMilestoneStatusHandler.cs` | Modificar | Transacción `Serializable` |
| `src/Aurora.Flowboard.Application/Components/Retire/RetireComponentHandler.cs` | Modificar | Transacción `Serializable` |
| `test/Aurora.Flowboard.Application.UnitTests/Milestones/ChangeMilestoneStatusHandlerTests.cs` | Modificar | Commit / no commit de la transacción |
| `test/Aurora.Flowboard.Application.UnitTests/Components/RetireComponentHandlerTests.cs` | Modificar | Commit / no commit de la transacción |
| `src/Aurora.Flowboard.Application/WorkItems/Shared/WorkItemFieldUpdateHandler.cs` | Modificar | `TransactionIsolationLevel` virtual y transacción opcional |
| `src/Aurora.Flowboard.Application/WorkItems/ChangeMilestone/ChangeWorkItemMilestoneHandler.cs` | Modificar | Sobrescribe `TransactionIsolationLevel` con `Serializable` |
| `src/Aurora.Flowboard.Application/WorkItems/ChangeComponent/ChangeWorkItemComponentHandler.cs` | Modificar | Sobrescribe `TransactionIsolationLevel` con `Serializable` |
| `src/Aurora.Flowboard.Application/WorkItems/Create/CreateWorkItemHandler.cs` | Modificar | Su transacción pasa a `Serializable` |
| `test/Aurora.Flowboard.Application.UnitTests/WorkItems/ChangeWorkItemMilestoneHandlerTests.cs` | Modificar | Commit / no commit de la transacción |
| `test/Aurora.Flowboard.Application.UnitTests/WorkItems/ChangeWorkItemComponentHandlerTests.cs` | Modificar | Commit / no commit de la transacción |
| `src/Aurora.Flowboard.Application/Users/ChangeRole/ChangeUserRoleHandler.cs` | Modificar | Transacción `Serializable` al degradar a un Administrator activo |
| `test/Aurora.Flowboard.Application.UnitTests/Users/ChangeUserRoleHandlerTests.cs` | Modificar | Transacción solo al degradar; commit / no commit |
| `src/Aurora.Flowboard.Application/Authentication/ChangePassword/ChangePasswordHandler.cs` | Modificar | Quita el `catch`; el behavior traduce el conflicto |
| `src/Aurora.Flowboard.Application/Authentication/AuthenticationErrors.cs` | Modificar | Elimina `SessionChangedConcurrently` |
| `test/Aurora.Flowboard.Application.UnitTests/Authentication/ChangePasswordHandlerTests.cs` | Modificar | La excepción se propaga |
| `src/Aurora.Flowboard.Infrastructure/Database/ConcurrencyTokenExtensions.cs` | Crear | `HasXminConcurrencyToken()` |
| `src/Aurora.Flowboard.Infrastructure/Configurations/{Project,Milestone,Component,WorkItem,User,UserToken}Configuration.cs` | Modificar | Llaman a `HasXminConcurrencyToken()` |
| `src/Aurora.Flowboard.Infrastructure/Database/Migrations/*_AddConcurrencyTokens.cs` (+ `.Designer.cs`, snapshot) | Crear / Modificar | Migración sin SQL efectivo |
| `src/Aurora.Flowboard.Api/Endpoints/**` (30 endpoints de comandos) | Modificar | `.Produces<ProblemDetails>(StatusCodes.Status409Conflict)` |
| `.claude/rules/concurrency.md` | Crear | Convenciones de concurrencia |
| `.claude/rules/authentication.md` | Modificar | Change password ya no captura la excepción |
| `CLAUDE.md` | Modificar | Pipeline y fila de la rule nueva |
| `.claude/skills/create-feature/SKILL.md` | Modificar | Pipeline |
| `.claude/skills/domain-entity/SKILL.md`, `.claude/skills/domain-entity/references/ef-configuration.md` | Modificar | `HasXminConcurrencyToken()` en vez de `byte[] row_version` |

`WorkItemFieldUpdateHandler.cs` crece con la transacción opcional; sigue teniendo una sola responsabilidad (el flujo común de las actualizaciones de campo).

## Tareas

### Tarea 0: Commit de spec, ADR y plan

**Skill:** —

**Ficheros:**
- Añadir: `docs/specs/2026-10-16-optimistic-concurrency-design.md`, `docs/architecture/adr/0001-optimistic-concurrency.md`, `docs/plans/2026-10-16-optimistic-concurrency-plan.md`, `docs/README.md`

- [x] Commit:

```bash
git add docs/specs/2026-10-16-optimistic-concurrency-design.md
git add docs/architecture/adr/0001-optimistic-concurrency.md
git add docs/plans/2026-10-16-optimistic-concurrency-plan.md
git add docs/README.md
git commit -m "Add optimistic concurrency spec, ADR and plan" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarea 1: `Project.AddMember` escribe la fila del root

**Skill:** `domain-entity`

**Ficheros:**
- Modificar: `src/Aurora.Flowboard.Domain/Projects/Project.cs`
- Test: `test/Aurora.Flowboard.Domain.UnitTests/Projects/ProjectTests.cs`

- [x] **Escribir el test que falla.** En `ProjectTests.cs`, dentro de `public sealed class AddMember : BaseTest`, justo después del test `Should_AddMemberAddedChangeLog_When_MemberAdded`, añadir:

```csharp
        [Fact]
        public void Should_SetUpdatedOnUtc_When_MemberAdded()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            User newMember = UserData.GetActiveUser();

            // Act
            project.AddMember(newMember, ProjectRole.Developer, admin, ProjectData.UpdatedOnUtc);

            // Assert
            project.UpdatedOnUtc.Should().Be(ProjectData.UpdatedOnUtc);
        }
```

- [x] **Verlo fallar:**

```bash
dotnet build test/Aurora.Flowboard.Domain.UnitTests/Aurora.Flowboard.Domain.UnitTests.csproj
./test/Aurora.Flowboard.Domain.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Domain.UnitTests.exe -method "*Should_SetUpdatedOnUtc_When_MemberAdded"
```

Esperado: 1 test, FAIL — `Expected project.UpdatedOnUtc to be <2026-06-01 ...>, but found <null>`.

- [x] **Implementar.** En `Project.AddMember`, después de `_members.Add(member);`:

```csharp
        ProjectMember member = ProjectMember.Create(Id, user.Id, role, joinedOnUtc);
        _members.Add(member);

        // Writes the projects row, so its xmin makes a concurrent member add/remove conflict.
        UpdatedOnUtc = joinedOnUtc;

        _changeLogs.Add(ProjectChangeLog.Create(this, changedBy, ProjectChangeType.MemberAdded, user.Id, joinedOnUtc));
```

- [x] **Verlo pasar:** el mismo comando → 1 test, PASS. Después, la clase entera de dominio de proyectos para descartar regresiones:

```bash
./test/Aurora.Flowboard.Domain.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Domain.UnitTests.exe -class "Aurora.Flowboard.Domain.UnitTests.Projects.ProjectTests+AddMember"
```

Si el filtro `+` no encuentra tests, ejecutar el `.exe` sin filtros y comprobar que no hay fallos.

- [x] **Commit:**

```bash
git add src/Aurora.Flowboard.Domain/Projects/Project.cs
git add test/Aurora.Flowboard.Domain.UnitTests/Projects/ProjectTests.cs
git add docs/plans/2026-10-16-optimistic-concurrency-plan.md
git commit -m "Touch the project root when adding a member" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarea 2: `PersistenceConflictBehavior` traduce los conflictos a 409

**Skill:** `create-feature` (pipeline de behaviors) — tests con las convenciones de `unit-testing`

**Ficheros:**
- Crear: `src/Aurora.Flowboard.Application/Abstractions/Data/PersistenceErrors.cs`
- Crear: `src/Aurora.Flowboard.Application/Abstractions/Behaviors/PersistenceConflictBehavior.cs`
- Modificar: `src/Aurora.Flowboard.Application/DependencyInjection.cs`
- Crear: `test/Aurora.Flowboard.Application.UnitTests/Abstractions/FakeDbException.cs`
- Test: `test/Aurora.Flowboard.Application.UnitTests/Abstractions/Behaviors/PersistenceConflictBehaviorTests.cs`

- [x] **Escribir el helper de test** `test/Aurora.Flowboard.Application.UnitTests/Abstractions/FakeDbException.cs`:

```csharp
using System.Data.Common;

namespace Aurora.Flowboard.Application.UnitTests.Abstractions;

// DbException is abstract, and Npgsql's PostgresException is not referenced by Application tests.
public sealed class FakeDbException(string sqlState) : DbException("Simulated database error")
{
    public override string SqlState { get; } = sqlState;
}
```

- [x] **Escribir los tests que fallan** `test/Aurora.Flowboard.Application.UnitTests/Abstractions/Behaviors/PersistenceConflictBehaviorTests.cs`:

```csharp
using Aurora.Flowboard.Application.Abstractions.Behaviors;
using Aurora.Flowboard.Application.Abstractions.Messaging;
using Aurora.Flowboard.Application.UnitTests.Milestones;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;

namespace Aurora.Flowboard.Application.UnitTests.Abstractions.Behaviors;

public sealed class PersistenceConflictBehaviorTests
{
    private const string ForeignKeyViolationSqlState = "23503";
    private static readonly Guid MilestoneId = new("3b2f6a1c-8d4e-4f5a-9b7c-1e2d3c4b5a69");
    private static readonly Guid ProjectId = new("7c1d2e3f-4a5b-4c6d-8e7f-9a0b1c2d3e4f");

    private readonly ChangeMilestoneStatusCommand _command =
        MilestoneCommandData.GetChangeStatusCommand(MilestoneId, MilestoneStatus.Active);
    private readonly ICommandHandler<ChangeMilestoneStatusCommand> _innerHandler =
        Substitute.For<ICommandHandler<ChangeMilestoneStatusCommand>>();
    private readonly CapturingLogger<PersistenceConflictBehavior.CommandBaseHandler<ChangeMilestoneStatusCommand>> _logger = new();
    private readonly PersistenceConflictBehavior.CommandBaseHandler<ChangeMilestoneStatusCommand> _behavior;

    private readonly CreateMilestoneCommand _commandWithResponse = MilestoneCommandData.GetCreateCommand(ProjectId);
    private readonly ICommandHandler<CreateMilestoneCommand, Guid> _innerHandlerWithResponse =
        Substitute.For<ICommandHandler<CreateMilestoneCommand, Guid>>();
    private readonly PersistenceConflictBehavior.CommandHandler<CreateMilestoneCommand, Guid> _behaviorWithResponse;

    public PersistenceConflictBehaviorTests()
    {
        _behavior = new PersistenceConflictBehavior.CommandBaseHandler<ChangeMilestoneStatusCommand>(_innerHandler, _logger);
        _behaviorWithResponse = new PersistenceConflictBehavior.CommandHandler<CreateMilestoneCommand, Guid>(
            _innerHandlerWithResponse,
            new CapturingLogger<PersistenceConflictBehavior.CommandHandler<CreateMilestoneCommand, Guid>>());
    }

    [Fact]
    public async Task Should_ReturnConcurrencyConflict_When_HandlerThrowsDbUpdateConcurrencyException()
    {
        // Arrange
        _innerHandler.Handle(_command, Arg.Any<CancellationToken>())
            .ThrowsAsync(new DbUpdateConcurrencyException("milestones row changed"));

        // Act
        Result result = await _behavior.Handle(_command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(PersistenceErrors.ConcurrencyConflict);
        result.Error.ErrorType.Should().Be(BaseErrorType.Conflict);
    }

    [Fact]
    public async Task Should_ReturnConcurrencyConflict_When_QueryOrCommitThrowsSerializationFailure()
    {
        // Arrange — a serialization failure raised by a query or CommitAsync is not wrapped.
        _innerHandler.Handle(_command, Arg.Any<CancellationToken>())
            .ThrowsAsync(new FakeDbException(PersistenceErrors.SerializationFailureSqlState));

        // Act
        Result result = await _behavior.Handle(_command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(PersistenceErrors.ConcurrencyConflict);
    }

    [Fact]
    public async Task Should_ReturnConcurrencyConflict_When_SaveChangesWrapsSerializationFailure()
    {
        // Arrange
        _innerHandler.Handle(_command, Arg.Any<CancellationToken>())
            .ThrowsAsync(new DbUpdateException(
                "save failed",
                new FakeDbException(PersistenceErrors.SerializationFailureSqlState)));

        // Act
        Result result = await _behavior.Handle(_command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(PersistenceErrors.ConcurrencyConflict);
    }

    [Fact]
    public async Task Should_ReturnUniqueViolation_When_SaveChangesWrapsUniqueViolation()
    {
        // Arrange
        _innerHandler.Handle(_command, Arg.Any<CancellationToken>())
            .ThrowsAsync(new DbUpdateException(
                "save failed",
                new FakeDbException(PersistenceErrors.UniqueViolationSqlState)));

        // Act
        Result result = await _behavior.Handle(_command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(PersistenceErrors.UniqueViolation);
        result.Error.ErrorType.Should().Be(BaseErrorType.Conflict);
    }

    [Fact]
    public async Task Should_Rethrow_When_DbUpdateExceptionHasAnotherSqlState()
    {
        // Arrange
        _innerHandler.Handle(_command, Arg.Any<CancellationToken>())
            .ThrowsAsync(new DbUpdateException("save failed", new FakeDbException(ForeignKeyViolationSqlState)));

        // Act
        Func<Task> act = () => _behavior.Handle(_command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Should_Rethrow_When_HandlerThrowsAnotherException()
    {
        // Arrange
        _innerHandler.Handle(_command, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("unexpected"));

        // Act
        Func<Task> act = () => _behavior.Handle(_command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Should_ReturnHandlerResult_When_HandlerDoesNotThrow()
    {
        // Arrange
        _innerHandler.Handle(_command, Arg.Any<CancellationToken>())
            .Returns(Result.Fail(MilestoneErrors.NotFound));

        // Act
        Result result = await _behavior.Handle(_command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(MilestoneErrors.NotFound);
    }

    [Fact]
    public async Task Should_LogWarning_When_ConflictIsTranslated()
    {
        // Arrange
        _innerHandler.Handle(_command, Arg.Any<CancellationToken>())
            .ThrowsAsync(new DbUpdateConcurrencyException("milestones row changed"));

        // Act
        await _behavior.Handle(_command, CancellationToken.None);

        // Assert
        _logger.Entries.Should().ContainSingle(e =>
            e.Level == LogLevel.Warning
            && e.Contains(nameof(ChangeMilestoneStatusCommand))
            && e.Contains(PersistenceErrors.ConcurrencyConflict.Code));
    }

    [Fact]
    public async Task Should_ReturnConcurrencyConflict_When_HandlerWithResponseThrowsDbUpdateConcurrencyException()
    {
        // Arrange
        _innerHandlerWithResponse.Handle(_commandWithResponse, Arg.Any<CancellationToken>())
            .ThrowsAsync(new DbUpdateConcurrencyException("projects row changed"));

        // Act
        Result<Guid> result = await _behaviorWithResponse.Handle(_commandWithResponse, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(PersistenceErrors.ConcurrencyConflict);
    }

    [Fact]
    public async Task Should_ReturnUniqueViolation_When_HandlerWithResponseThrowsUniqueViolation()
    {
        // Arrange
        _innerHandlerWithResponse.Handle(_commandWithResponse, Arg.Any<CancellationToken>())
            .ThrowsAsync(new DbUpdateException(
                "save failed",
                new FakeDbException(PersistenceErrors.UniqueViolationSqlState)));

        // Act
        Result<Guid> result = await _behaviorWithResponse.Handle(_commandWithResponse, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(PersistenceErrors.UniqueViolation);
    }

    [Fact]
    public async Task Should_ReturnHandlerValue_When_HandlerWithResponseSucceeds()
    {
        // Arrange
        _innerHandlerWithResponse.Handle(_commandWithResponse, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(MilestoneId));

        // Act
        Result<Guid> result = await _behaviorWithResponse.Handle(_commandWithResponse, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().Be(MilestoneId);
    }
}
```

- [x] **Verlo fallar (compilación).** `dotnet build test/Aurora.Flowboard.Application.UnitTests/Aurora.Flowboard.Application.UnitTests.csproj` → `CS0234`/`CS0103`: no existen `PersistenceConflictBehavior` ni `PersistenceErrors`. Para ver fallar las assertions, crear los stubs:

`src/Aurora.Flowboard.Application/Abstractions/Data/PersistenceErrors.cs` (definitivo, no cambia en el paso de implementación):

```csharp
namespace Aurora.Flowboard.Application.Abstractions.Data;

public static class PersistenceErrors
{
    // Postgres SqlStates, read through System.Data.Common.DbException so Application does not depend on Npgsql.
    public const string UniqueViolationSqlState = "23505";
    public const string SerializationFailureSqlState = "40001";

    public static readonly BaseError ConcurrencyConflict = BaseError.Conflict(
        "Persistence.ConcurrencyConflict",
        "The resource was modified by another request. Reload it and try again.");

    public static readonly BaseError UniqueViolation = BaseError.Conflict(
        "Persistence.UniqueViolation",
        "A resource with the same unique value already exists.");
}
```

`src/Aurora.Flowboard.Application/Abstractions/Behaviors/PersistenceConflictBehavior.cs` (stub sin comportamiento):

```csharp
using Microsoft.Extensions.Logging;

namespace Aurora.Flowboard.Application.Abstractions.Behaviors;

internal static class PersistenceConflictBehavior
{
    internal sealed class CommandHandler<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> innerHandler,
        ILogger<CommandHandler<TCommand, TResponse>> logger) : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        public async Task<Result<TResponse>> Handle(
            TCommand command,
            CancellationToken cancellationToken)
        {
            _ = logger;
            return await innerHandler.Handle(command, cancellationToken);
        }
    }

    internal sealed class CommandBaseHandler<TCommand>(
        ICommandHandler<TCommand> innerHandler,
        ILogger<CommandBaseHandler<TCommand>> logger) : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
        {
            _ = logger;
            return await innerHandler.Handle(command, cancellationToken);
        }
    }
}
```

```bash
dotnet build test/Aurora.Flowboard.Application.UnitTests/Aurora.Flowboard.Application.UnitTests.csproj
./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests.exe -class "Aurora.Flowboard.Application.UnitTests.Abstractions.Behaviors.PersistenceConflictBehaviorTests"
```

Esperado: 11 tests; FAIL 7 — los 6 de traducción (la excepción se propaga en vez de devolver el error) y el del log; PASS 4: `Should_Rethrow_*` (2), `Should_ReturnHandlerResult_When_HandlerDoesNotThrow` y `Should_ReturnHandlerValue_When_HandlerWithResponseSucceeds`.

- [x] **Implementar.** Sustituir `PersistenceConflictBehavior.cs` por:

```csharp
using System.Data.Common;
using Microsoft.Extensions.Logging;

namespace Aurora.Flowboard.Application.Abstractions.Behaviors;

internal static class PersistenceConflictBehavior
{
    internal sealed class CommandHandler<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> innerHandler,
        ILogger<CommandHandler<TCommand, TResponse>> logger) : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        public async Task<Result<TResponse>> Handle(
            TCommand command,
            CancellationToken cancellationToken)
        {
            try
            {
                return await innerHandler.Handle(command, cancellationToken);
            }
            catch (Exception exception) when (GetConflictError(exception) is { } error)
            {
                LogConflict(logger, typeof(TCommand).Name, error, exception);

                return Result.Fail<TResponse>(error);
            }
        }
    }

    internal sealed class CommandBaseHandler<TCommand>(
        ICommandHandler<TCommand> innerHandler,
        ILogger<CommandBaseHandler<TCommand>> logger) : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
        {
            try
            {
                return await innerHandler.Handle(command, cancellationToken);
            }
            catch (Exception exception) when (GetConflictError(exception) is { } error)
            {
                LogConflict(logger, typeof(TCommand).Name, error, exception);

                return Result.Fail(error);
            }
        }
    }

    // A serialization failure surfaces directly from a query or CommitAsync, or wrapped by SaveChanges.
    // DbException (System.Data.Common) exposes SqlState without depending on Npgsql.
    private static BaseError? GetConflictError(Exception exception) => exception switch
    {
        DbUpdateConcurrencyException => PersistenceErrors.ConcurrencyConflict,
        DbException { SqlState: PersistenceErrors.SerializationFailureSqlState } => PersistenceErrors.ConcurrencyConflict,
        DbUpdateException { InnerException: DbException { SqlState: PersistenceErrors.SerializationFailureSqlState } }
            => PersistenceErrors.ConcurrencyConflict,
        DbUpdateException { InnerException: DbException { SqlState: PersistenceErrors.UniqueViolationSqlState } }
            => PersistenceErrors.UniqueViolation,
        _ => null
    };

    // The exception keeps the constraint name and the failed statement, which the Result loses.
    private static void LogConflict(ILogger logger, string commandName, BaseError error, Exception exception) =>
        logger.LogWarning(exception, "Persistence conflict in {Name}: {ErrorCode}", commandName, error.Code);
}
```

En `src/Aurora.Flowboard.Application/DependencyInjection.cs`, al principio de `AddBehaviors`:

```csharp
    private static IServiceCollection AddBehaviors(this IServiceCollection services)
    {
        // Registered first, so it is the innermost decorator: it only sees exceptions thrown by the
        // handler, and the outer behaviors log and time the translated Result. Commands only: queries don't write.
        services.Decorate(typeof(ICommandHandler<,>), typeof(PersistenceConflictBehavior.CommandHandler<,>));
        services.Decorate(typeof(ICommandHandler<>), typeof(PersistenceConflictBehavior.CommandBaseHandler<>));

        services.Decorate(typeof(IQueryHandler<,>), typeof(ValidationBehavior.QueryHandler<,>));
```

(el resto del método no cambia).

- [x] **Verlo pasar:** los mismos dos comandos → 11 tests, PASS. Después la suite de arquitectura (behavior nuevo en `Abstractions/Behaviors`, sellado, visibilidad):

```bash
dotnet build test/Aurora.Flowboard.ArchitectureTests/Aurora.Flowboard.ArchitectureTests.csproj
./test/Aurora.Flowboard.ArchitectureTests/bin/Debug/net10.0/Aurora.Flowboard.ArchitectureTests.exe
```

Esperado: todos PASS.

- [x] **Commit:**

```bash
git add src/Aurora.Flowboard.Application/Abstractions/Data/PersistenceErrors.cs
git add src/Aurora.Flowboard.Application/Abstractions/Behaviors/PersistenceConflictBehavior.cs
git add src/Aurora.Flowboard.Application/DependencyInjection.cs
git add test/Aurora.Flowboard.Application.UnitTests/Abstractions/FakeDbException.cs
git add test/Aurora.Flowboard.Application.UnitTests/Abstractions/Behaviors/PersistenceConflictBehaviorTests.cs
git add docs/plans/2026-10-16-optimistic-concurrency-plan.md
git commit -m "Translate persistence conflicts to 409 in a command behavior" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarea 3: `BeginTransactionAsync` con nivel de aislamiento

**Skill:** — (firma en el contexto; la cubren los tests de handler de las tareas 4–6)

**Ficheros:**
- Modificar: `src/Aurora.Flowboard.Application/Abstractions/Data/IApplicationDbContext.cs`
- Modificar: `src/Aurora.Flowboard.Infrastructure/Database/ApplicationDbContext.cs`

- [x] **Interfaz.** En `IApplicationDbContext.cs`, añadir `using System.Data;` como primera línea (antes de `namespace`) y, debajo de la sobrecarga existente:

```csharp
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task<IDbContextTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken = default);
```

- [x] **Implementación.** En `ApplicationDbContext.cs`, añadir `using System.Data;` como primera línea y, debajo del `BeginTransactionAsync` existente:

```csharp
    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        Database.BeginTransactionAsync(cancellationToken);

    public Task<IDbContextTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken = default) =>
        Database.BeginTransactionAsync(isolationLevel, cancellationToken);
```

- [x] **Compilar** (no hay test que falle: es una delegación al `DatabaseFacade`):

```bash
dotnet build src/Aurora.Flowboard.Infrastructure/Aurora.Flowboard.Infrastructure.csproj
dotnet build test/Aurora.Flowboard.Application.UnitTests/Aurora.Flowboard.Application.UnitTests.csproj
./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests.exe
```

Esperado: build limpio y todos los tests de Application PASS.

- [x] **Commit:**

```bash
git add src/Aurora.Flowboard.Application/Abstractions/Data/IApplicationDbContext.cs
git add src/Aurora.Flowboard.Infrastructure/Database/ApplicationDbContext.cs
git add docs/plans/2026-10-16-optimistic-concurrency-plan.md
git commit -m "Add isolation level overload to BeginTransactionAsync" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarea 4: cierre de milestone y retirada de componente en `Serializable`

**Skill:** `create-feature` (modificar handlers existentes) — tests con `unit-testing`

**Ficheros:**
- Modificar: `src/Aurora.Flowboard.Application/Milestones/ChangeStatus/ChangeMilestoneStatusHandler.cs`
- Modificar: `src/Aurora.Flowboard.Application/Components/Retire/RetireComponentHandler.cs`
- Test: `test/Aurora.Flowboard.Application.UnitTests/Milestones/ChangeMilestoneStatusHandlerTests.cs`
- Test: `test/Aurora.Flowboard.Application.UnitTests/Components/RetireComponentHandlerTests.cs`

- [x] **Escribir los tests.** En `ChangeMilestoneStatusHandlerTests.cs`, cambiar la cabecera a:

```csharp
using System.Data;
using Aurora.Flowboard.Application.UnitTests.WorkItems;
using Microsoft.EntityFrameworkCore.Storage;
```

y añadir al final de la clase (antes de la `}` de cierre):

```csharp
    [Fact]
    public async Task Should_CommitSerializableTransaction_When_CommandIsValid()
    {
        // Arrange
        User admin = MilestoneCommandData.GetAdmin();
        MilestoneCommandData.GetProjectWithMilestone(admin, out Milestone milestone);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(MilestoneCommandData.UtcNow);
        IDbContextTransaction transaction = SetupSerializableTransaction();

        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet([milestone]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<WorkItem>());
        _dbContext.Milestones.Returns(milestonesMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.WorkItems.Returns(workItemsMock);

        ChangeMilestoneStatusCommand command = MilestoneCommandData.GetChangeStatusCommand(milestone.Id, MilestoneStatus.Active);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_NotCommit_When_StatusTransitionIsInvalid()
    {
        // Arrange
        User admin = MilestoneCommandData.GetAdmin();
        MilestoneCommandData.GetProjectWithMilestone(admin, out Milestone milestone);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(MilestoneCommandData.UtcNow);
        IDbContextTransaction transaction = SetupSerializableTransaction();

        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet([milestone]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<WorkItem>());
        _dbContext.Milestones.Returns(milestonesMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.WorkItems.Returns(workItemsMock);

        ChangeMilestoneStatusCommand command = MilestoneCommandData.GetChangeStatusCommand(milestone.Id, MilestoneStatus.Completed);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    private IDbContextTransaction SetupSerializableTransaction()
    {
        IDbContextTransaction transaction = Substitute.For<IDbContextTransaction>();
        _dbContext.BeginTransactionAsync(Arg.Is(IsolationLevel.Serializable), Arg.Any<CancellationToken>())
            .Returns(transaction);
        return transaction;
    }
```

En `RetireComponentHandlerTests.cs`, cambiar la cabecera a:

```csharp
using System.Data;
using Aurora.Flowboard.Application.UnitTests.WorkItems;
using Microsoft.EntityFrameworkCore.Storage;
```

y añadir al final de la clase:

```csharp
    [Fact]
    public async Task Should_CommitSerializableTransaction_When_CommandIsValid()
    {
        // Arrange
        User admin = ComponentCommandData.GetAdmin();
        ComponentCommandData.GetProjectWithComponent(admin, out Component component);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(ComponentCommandData.UtcNow);
        IDbContextTransaction transaction = SetupSerializableTransaction();

        DbSet<Component> componentsMock = MockDbSetHelper.CreateMockDbSet([component]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<WorkItem>());
        _dbContext.Components.Returns(componentsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.WorkItems.Returns(workItemsMock);

        RetireComponentCommand command = ComponentCommandData.GetRetireCommand(component.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_NotCommit_When_ComponentAlreadyRetired()
    {
        // Arrange
        User admin = ComponentCommandData.GetAdmin();
        ComponentCommandData.GetProjectWithComponent(admin, out Component component);
        component.Retire(admin, 0, ComponentCommandData.UtcNow);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(ComponentCommandData.UtcNow);
        IDbContextTransaction transaction = SetupSerializableTransaction();

        DbSet<Component> componentsMock = MockDbSetHelper.CreateMockDbSet([component]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<WorkItem>());
        _dbContext.Components.Returns(componentsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.WorkItems.Returns(workItemsMock);

        RetireComponentCommand command = ComponentCommandData.GetRetireCommand(component.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    private IDbContextTransaction SetupSerializableTransaction()
    {
        IDbContextTransaction transaction = Substitute.For<IDbContextTransaction>();
        _dbContext.BeginTransactionAsync(Arg.Is(IsolationLevel.Serializable), Arg.Any<CancellationToken>())
            .Returns(transaction);
        return transaction;
    }
```

- [x] **Verlo fallar:**

```bash
dotnet build test/Aurora.Flowboard.Application.UnitTests/Aurora.Flowboard.Application.UnitTests.csproj
./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests.exe -class "Aurora.Flowboard.Application.UnitTests.Milestones.ChangeMilestoneStatusHandlerTests" -class "Aurora.Flowboard.Application.UnitTests.Components.RetireComponentHandlerTests"
```

Esperado: FAIL los dos `Should_CommitSerializableTransaction_When_CommandIsValid` (`Expected to receive exactly 1 call matching: CommitAsync(any CancellationToken)`). Los `Should_NotCommit_*` ya pasan (hoy no hay transacción); quedan como protección.

- [x] **Implementar.** `ChangeMilestoneStatusHandler.cs` completo:

```csharp
using System.Data;

namespace Aurora.Flowboard.Application.Milestones.ChangeStatus;

internal sealed class ChangeMilestoneStatusHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ICommandHandler<ChangeMilestoneStatusCommand>
{
    public async Task<Result> Handle(
        ChangeMilestoneStatusCommand command,
        CancellationToken cancellationToken)
    {
        // Serializable (SSI): closing counts the open work items and writes the milestone, while
        // ChangeWorkItemMilestone and CreateWorkItem read the milestone status and write work_items.
        // Postgres aborts one of two concurrent requests with 40001 (PersistenceConflictBehavior → 409).
        await using IDbContextTransaction transaction =
            await dbContext.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        Milestone? milestone = await dbContext
            .Milestones
            .Include(m => m.Project)
            .ThenInclude(p => p.Members)
            .AsSplitQuery()
            .SingleOrDefaultAsync(m => m.Id == command.MilestoneId, cancellationToken);

        if (milestone is null)
        {
            return Result.Fail(MilestoneErrors.NotFound);
        }

        User? changedBy = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);

        if (changedBy is null)
        {
            return Result.Fail(UserErrors.NotFound);
        }

        int openWorkItemCount = await dbContext
            .WorkItems
            .Where(w => w.MilestoneId == milestone.Id
                && w.FlowState.Category != FlowStateCategory.Completed
                && w.FlowState.Category != FlowStateCategory.Cancelled)
            .CountAsync(cancellationToken);

        Result result = milestone.ChangeStatus(command.NewStatus, changedBy, openWorkItemCount, dateTimeProvider.UtcNow);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Ok();
    }
}
```

`RetireComponentHandler.cs` completo:

```csharp
using System.Data;

namespace Aurora.Flowboard.Application.Components.Retire;

internal sealed class RetireComponentHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ICommandHandler<RetireComponentCommand>
{
    public async Task<Result> Handle(
        RetireComponentCommand command,
        CancellationToken cancellationToken)
    {
        // Serializable (SSI): retiring counts the open work items and writes the component, while
        // ChangeWorkItemComponent and CreateWorkItem read the component status and write work_items.
        // Postgres aborts one of two concurrent requests with 40001 (PersistenceConflictBehavior → 409).
        await using IDbContextTransaction transaction =
            await dbContext.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        Component? component = await dbContext
            .Components
            .Include(c => c.Project)
            .ThenInclude(p => p.Members)
            .SingleOrDefaultAsync(c => c.Id == command.ComponentId, cancellationToken);

        if (component is null)
        {
            return Result.Fail(ComponentErrors.NotFound);
        }

        User? changedBy = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);

        if (changedBy is null)
        {
            return Result.Fail(UserErrors.NotFound);
        }

        int openWorkItemCount = await dbContext
            .WorkItems
            .Where(w => w.ComponentId == component.Id
                && w.FlowState.Category != FlowStateCategory.Completed
                && w.FlowState.Category != FlowStateCategory.Cancelled)
            .CountAsync(cancellationToken);

        Result result = component.Retire(changedBy, openWorkItemCount, dateTimeProvider.UtcNow);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Ok();
    }
}
```

Antes de sustituir, comparar con el fichero actual: si alguna línea (usings, consultas) difiere de lo que aquí se copia, conservar la del fichero y añadir solo la transacción, el comentario y el `CommitAsync`.

- [x] **Verlo pasar:** el mismo comando → todas PASS (incluidos los tests existentes de ambas clases).

- [x] **Commit:**

```bash
git add src/Aurora.Flowboard.Application/Milestones/ChangeStatus/ChangeMilestoneStatusHandler.cs
git add src/Aurora.Flowboard.Application/Components/Retire/RetireComponentHandler.cs
git add test/Aurora.Flowboard.Application.UnitTests/Milestones/ChangeMilestoneStatusHandlerTests.cs
git add test/Aurora.Flowboard.Application.UnitTests/Components/RetireComponentHandlerTests.cs
git add docs/plans/2026-10-16-optimistic-concurrency-plan.md
git commit -m "Run milestone status changes and component retirement as serializable" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarea 5: cambio de milestone/componente de un work item y su creación en `Serializable`

**Skill:** `create-feature` (modificar handlers existentes) — tests con `unit-testing`

**Ficheros:**
- Modificar: `src/Aurora.Flowboard.Application/WorkItems/Shared/WorkItemFieldUpdateHandler.cs`
- Modificar: `src/Aurora.Flowboard.Application/WorkItems/ChangeMilestone/ChangeWorkItemMilestoneHandler.cs`
- Modificar: `src/Aurora.Flowboard.Application/WorkItems/ChangeComponent/ChangeWorkItemComponentHandler.cs`
- Modificar: `src/Aurora.Flowboard.Application/WorkItems/Create/CreateWorkItemHandler.cs`
- Test: `test/Aurora.Flowboard.Application.UnitTests/WorkItems/ChangeWorkItemMilestoneHandlerTests.cs`
- Test: `test/Aurora.Flowboard.Application.UnitTests/WorkItems/ChangeWorkItemComponentHandlerTests.cs`

- [x] **Escribir los tests.** En `ChangeWorkItemMilestoneHandlerTests.cs`, añadir antes de `namespace`:

```csharp
using System.Data;
using Microsoft.EntityFrameworkCore.Storage;
```

y al final de la clase:

```csharp
    [Fact]
    public async Task Should_CommitSerializableTransaction_When_CommandIsValid()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        WorkItem workItem = WorkItemCommandData.GetWorkItemWithProjectMilestone(admin, out Milestone milestone);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(WorkItemCommandData.UtcNow);
        IDbContextTransaction transaction = SetupSerializableTransaction();

        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet([milestone]);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.Milestones.Returns(milestonesMock);

        ChangeWorkItemMilestoneCommand command = new(workItem.Id, milestone.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_NotCommit_When_MilestoneDoesNotExist()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        WorkItem workItem = WorkItemCommandData.GetWorkItem(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(WorkItemCommandData.UtcNow);
        IDbContextTransaction transaction = SetupSerializableTransaction();

        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Milestone>());
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.Milestones.Returns(milestonesMock);

        ChangeWorkItemMilestoneCommand command = new(workItem.Id, Guid.NewGuid());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    private IDbContextTransaction SetupSerializableTransaction()
    {
        IDbContextTransaction transaction = Substitute.For<IDbContextTransaction>();
        _dbContext.BeginTransactionAsync(Arg.Is(IsolationLevel.Serializable), Arg.Any<CancellationToken>())
            .Returns(transaction);
        return transaction;
    }
```

En `ChangeWorkItemComponentHandlerTests.cs`, los mismos `using` antes de `namespace` y al final de la clase:

```csharp
    [Fact]
    public async Task Should_CommitSerializableTransaction_When_CommandIsValid()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        WorkItem workItem = WorkItemCommandData.GetWorkItemWithProjectComponent(admin, out Component component);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(WorkItemCommandData.UtcNow);
        IDbContextTransaction transaction = SetupSerializableTransaction();

        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<Component> componentsMock = MockDbSetHelper.CreateMockDbSet([component]);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.Components.Returns(componentsMock);

        ChangeWorkItemComponentCommand command = new(workItem.Id, component.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_NotCommit_When_ComponentDoesNotExist()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        WorkItem workItem = WorkItemCommandData.GetWorkItem(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(WorkItemCommandData.UtcNow);
        IDbContextTransaction transaction = SetupSerializableTransaction();

        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<Component> componentsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Component>());
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.Components.Returns(componentsMock);

        ChangeWorkItemComponentCommand command = new(workItem.Id, Guid.NewGuid());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    private IDbContextTransaction SetupSerializableTransaction()
    {
        IDbContextTransaction transaction = Substitute.For<IDbContextTransaction>();
        _dbContext.BeginTransactionAsync(Arg.Is(IsolationLevel.Serializable), Arg.Any<CancellationToken>())
            .Returns(transaction);
        return transaction;
    }
```

- [x] **Verlo fallar:**

```bash
dotnet build test/Aurora.Flowboard.Application.UnitTests/Aurora.Flowboard.Application.UnitTests.csproj
./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests.exe -class "Aurora.Flowboard.Application.UnitTests.WorkItems.ChangeWorkItemMilestoneHandlerTests" -class "Aurora.Flowboard.Application.UnitTests.WorkItems.ChangeWorkItemComponentHandlerTests"
```

Esperado: FAIL los dos `Should_CommitSerializableTransaction_When_CommandIsValid`; los `Should_NotCommit_*` ya pasan.

- [x] **Implementar.** `WorkItemFieldUpdateHandler.cs` completo:

```csharp
using System.Data;

namespace Aurora.Flowboard.Application.WorkItems.Shared;

internal abstract class WorkItemFieldUpdateHandler<TCommand>(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    protected IApplicationDbContext DbContext => dbContext;

    // Updates that read another aggregate's state to decide (milestone, component status) race with the
    // handler that closes it; they return Serializable so Postgres (SSI) aborts one of the two requests.
    protected virtual IsolationLevel? TransactionIsolationLevel => null;

    public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
    {
        if (TransactionIsolationLevel is not { } isolationLevel)
        {
            return await UpdateAsync(command, cancellationToken);
        }

        await using IDbContextTransaction transaction =
            await dbContext.BeginTransactionAsync(isolationLevel, cancellationToken);

        Result result = await UpdateAsync(command, cancellationToken);

        if (result.IsSuccessful)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return result;
    }

    protected abstract Guid GetWorkItemId(TCommand command);

    protected abstract Task<Result> ApplyAsync(
        WorkItem workItem,
        TCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken);

    private async Task<Result> UpdateAsync(TCommand command, CancellationToken cancellationToken)
    {
        Guid workItemId = GetWorkItemId(command);

        WorkItem? workItem = await dbContext
            .WorkItems
            .Include(w => w.Project)
            .ThenInclude(p => p.Members)
            .AsSplitQuery()
            .SingleOrDefaultAsync(w => w.Id == workItemId, cancellationToken);

        if (workItem is null)
        {
            return Result.Fail(WorkItemErrors.NotFound);
        }

        User? changedBy = await dbContext
            .Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);

        if (changedBy is null)
        {
            return Result.Fail(UserErrors.NotFound);
        }

        Result result = await ApplyAsync(
            workItem,
            command,
            changedBy,
            dateTimeProvider.UtcNow,
            cancellationToken);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
```

`ChangeWorkItemMilestoneHandler.cs`: añadir `using System.Data;` como primera línea y, debajo de `GetWorkItemId`:

```csharp
    protected override Guid GetWorkItemId(ChangeWorkItemMilestoneCommand command) => command.Id;

    // The milestone status read in ApplyAsync races with ChangeMilestoneStatus closing it.
    protected override IsolationLevel? TransactionIsolationLevel => IsolationLevel.Serializable;
```

`ChangeWorkItemComponentHandler.cs`: añadir `using System.Data;` como primera línea y, debajo de `GetWorkItemId`:

```csharp
    protected override Guid GetWorkItemId(ChangeWorkItemComponentCommand command) => command.Id;

    // The component status read in ApplyAsync races with RetireComponent retiring it.
    protected override IsolationLevel? TransactionIsolationLevel => IsolationLevel.Serializable;
```

`CreateWorkItemHandler.cs` (sin tests de handler: usa `FromSqlRaw`, que el `DbSet` simulado no soporta): añadir `using System.Data;` como primera línea y sustituir

```csharp
        await using IDbContextTransaction transaction =
            await dbContext.BeginTransactionAsync(cancellationToken);
```

por

```csharp
        // Serializable (SSI): the milestone and component status checks below race with
        // ChangeMilestoneStatus / RetireComponent, which count the open work items this inserts.
        await using IDbContextTransaction transaction =
            await dbContext.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
```

(el `FOR UPDATE` sobre `projects` y el resto del handler no cambian).

- [x] **Verlo pasar:** el mismo comando → todas PASS. Después toda la suite de Application (los ocho handlers de campo heredan del base) y la de arquitectura (`WorkItemFieldUpdateHandler` es un handler abstracto incluido en sus reglas):

```bash
./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests.exe
dotnet build test/Aurora.Flowboard.ArchitectureTests/Aurora.Flowboard.ArchitectureTests.csproj
./test/Aurora.Flowboard.ArchitectureTests/bin/Debug/net10.0/Aurora.Flowboard.ArchitectureTests.exe
```

- [x] **Commit:**

```bash
git add src/Aurora.Flowboard.Application/WorkItems/Shared/WorkItemFieldUpdateHandler.cs
git add src/Aurora.Flowboard.Application/WorkItems/ChangeMilestone/ChangeWorkItemMilestoneHandler.cs
git add src/Aurora.Flowboard.Application/WorkItems/ChangeComponent/ChangeWorkItemComponentHandler.cs
git add src/Aurora.Flowboard.Application/WorkItems/Create/CreateWorkItemHandler.cs
git add test/Aurora.Flowboard.Application.UnitTests/WorkItems/ChangeWorkItemMilestoneHandlerTests.cs
git add test/Aurora.Flowboard.Application.UnitTests/WorkItems/ChangeWorkItemComponentHandlerTests.cs
git add docs/plans/2026-10-16-optimistic-concurrency-plan.md
git commit -m "Run work item milestone and component changes as serializable" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarea 6: degradar a un Administrator en `Serializable`

**Skill:** `create-feature` (modificar handler existente) — tests con `unit-testing`

**Ficheros:**
- Modificar: `src/Aurora.Flowboard.Application/Users/ChangeRole/ChangeUserRoleHandler.cs`
- Test: `test/Aurora.Flowboard.Application.UnitTests/Users/ChangeUserRoleHandlerTests.cs`

- [x] **Escribir los tests.** En `ChangeUserRoleHandlerTests.cs`, cambiar la cabecera a:

```csharp
using System.Data;
using Aurora.Flowboard.Application.Users.ChangeRole;
using Microsoft.EntityFrameworkCore.Storage;
```

y añadir antes de `private static User GetAdministrator(string email)`:

```csharp
    [Fact]
    public async Task Should_CommitSerializableTransaction_When_DemotingAdministratorWithAnotherActiveAdministrator()
    {
        // Arrange
        User administrator = GetAdministrator(CreateUserCommandData.EmailAddress);
        User otherAdministrator = GetAdministrator(OtherEmailAddress);
        _dateTimeProvider.UtcNow.Returns(CreateUserCommandData.UtcNow);
        IDbContextTransaction transaction = SetupSerializableTransaction();
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([administrator, otherAdministrator]);
        _dbContext.Users.Returns(usersMock);

        var command = new ChangeUserRoleCommand(administrator.Id, Role.Member.Name);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_NotCommit_When_DemotingTheOnlyAdministrator()
    {
        // Arrange
        User administrator = GetAdministrator(CreateUserCommandData.EmailAddress);
        IDbContextTransaction transaction = SetupSerializableTransaction();
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([administrator]);
        _dbContext.Users.Returns(usersMock);

        var command = new ChangeUserRoleCommand(administrator.Id, Role.Member.Name);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_NotOpenTransaction_When_NotDemotingAnAdministrator()
    {
        // Arrange
        User user = CreateUserCommandData.GetExistingUser();
        _dateTimeProvider.UtcNow.Returns(CreateUserCommandData.UtcNow);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        _dbContext.Users.Returns(usersMock);

        var command = new ChangeUserRoleCommand(user.Id, Role.Member.Name);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive()
            .BeginTransactionAsync(Arg.Any<IsolationLevel>(), Arg.Any<CancellationToken>());
    }

    private IDbContextTransaction SetupSerializableTransaction()
    {
        IDbContextTransaction transaction = Substitute.For<IDbContextTransaction>();
        _dbContext.BeginTransactionAsync(Arg.Is(IsolationLevel.Serializable), Arg.Any<CancellationToken>())
            .Returns(transaction);
        return transaction;
    }
```

- [x] **Verlo fallar:**

```bash
dotnet build test/Aurora.Flowboard.Application.UnitTests/Aurora.Flowboard.Application.UnitTests.csproj
./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests.exe -class "Aurora.Flowboard.Application.UnitTests.Users.ChangeUserRoleHandlerTests"
```

Esperado: FAIL `Should_CommitSerializableTransaction_When_DemotingAdministratorWithAnotherActiveAdministrator`. `Should_NotCommit_When_DemotingTheOnlyAdministrator` y `Should_NotOpenTransaction_When_NotDemotingAnAdministrator` ya pasan: protegen que la transacción solo se abra al degradar y nunca se confirme con un error.

- [x] **Implementar.** `ChangeUserRoleHandler.cs` completo:

```csharp
using System.Data;

namespace Aurora.Flowboard.Application.Users.ChangeRole;

internal sealed class ChangeUserRoleHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<ChangeUserRoleCommand>
{
    public async Task<Result> Handle(
        ChangeUserRoleCommand command,
        CancellationToken cancellationToken)
    {
        Result<Role> roleResult = Role.FromName(command.Role.Trim());
        if (!roleResult.IsSuccessful)
        {
            return Result.Fail(roleResult.Error);
        }

        User? user = await dbContext
            .Users
            .Include(u => u.Roles)
            .SingleOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Fail(UserErrors.NotFound);
        }

        Role newRole = roleResult.Value;

        bool isDemotingActiveAdministrator = user.IsActive
            && user.IsAdministrator
            && newRole.Name != Role.Administrator.Name;

        // Only demoting an active Administrator can leave the system without one.
        if (!isDemotingActiveAdministrator)
        {
            return await ChangeRoleAsync(user, newRole, hasOtherActiveAdministrator: true, cancellationToken);
        }

        // Serializable (SSI): two concurrent demotions of two different Administrators each see the other
        // as active and write different rows (write skew); Postgres aborts one with 40001 (→ 409).
        await using IDbContextTransaction transaction =
            await dbContext.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        // IsAdministrator is not translatable to SQL, hence the explicit Roles.Any below.
        bool hasOtherActiveAdministrator = await dbContext
            .Users
            .AnyAsync(
                u => u.Id != user.Id && u.IsActive && u.Roles.Any(r => r.Name == Role.Administrator.Name),
                cancellationToken);

        Result result = await ChangeRoleAsync(user, newRole, hasOtherActiveAdministrator, cancellationToken);

        if (result.IsSuccessful)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return result;
    }

    private async Task<Result> ChangeRoleAsync(
        User user,
        Role newRole,
        bool hasOtherActiveAdministrator,
        CancellationToken cancellationToken)
    {
        Result changeRoleResult = user.ChangeRole(newRole, hasOtherActiveAdministrator, dateTimeProvider.UtcNow);

        if (!changeRoleResult.IsSuccessful)
        {
            return changeRoleResult;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
```

- [x] **Verlo pasar:** el mismo comando → todas PASS (incluidos los tests existentes de último Administrator).

- [x] **Commit:**

```bash
git add src/Aurora.Flowboard.Application/Users/ChangeRole/ChangeUserRoleHandler.cs
git add test/Aurora.Flowboard.Application.UnitTests/Users/ChangeUserRoleHandlerTests.cs
git add docs/plans/2026-10-16-optimistic-concurrency-plan.md
git commit -m "Run administrator demotion as serializable" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarea 7: change password delega el conflicto en el behavior

**Skill:** `create-feature` (modificar handler existente) — tests con `unit-testing`

**Ficheros:**
- Modificar: `src/Aurora.Flowboard.Application/Authentication/ChangePassword/ChangePasswordHandler.cs`
- Modificar: `src/Aurora.Flowboard.Application/Authentication/AuthenticationErrors.cs`
- Test: `test/Aurora.Flowboard.Application.UnitTests/Authentication/ChangePasswordHandlerTests.cs`

- [x] **Cambiar el test.** En `ChangePasswordHandlerTests.cs`, sustituir el test `Should_ReturnSessionChangedConcurrently_When_TokensChangeDuringSave` completo por:

```csharp
    [Fact]
    public async Task Should_PropagateConcurrencyException_When_TokensChangeDuringSave()
    {
        // Arrange — a refresh rotated one of the tokens between the read and the save.
        // PersistenceConflictBehavior turns the exception into Persistence.ConcurrencyConflict (409).
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
        Func<Task> act = () => _handler.Handle(ChangePasswordCommandData.GetCommand(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }
```

- [x] **Verlo fallar:**

```bash
dotnet build test/Aurora.Flowboard.Application.UnitTests/Aurora.Flowboard.Application.UnitTests.csproj
./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests.exe -class "Aurora.Flowboard.Application.UnitTests.Authentication.ChangePasswordHandlerTests"
```

Esperado: FAIL `Should_PropagateConcurrencyException_When_TokensChangeDuringSave` (`Expected a <DbUpdateConcurrencyException> to be thrown, but no exception was thrown`).

- [x] **Implementar.** En `ChangePasswordHandler.cs`, sustituir

```csharp
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
```

por

```csharp
        // If a refresh rotated one of these tokens meanwhile, xmin makes SaveChanges throw and nothing is
        // saved; PersistenceConflictBehavior returns a 409, and the client's retry revokes the rotated token too.
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
```

En `AuthenticationErrors.cs`, eliminar el bloque:

```csharp
    public static readonly BaseError SessionChangedConcurrently = BaseError.Conflict(
        "Auth.SessionChangedConcurrently",
        "The user's sessions changed while the request was processed. Try again.");

```

Comprobar que no queda ninguna referencia en código:

```bash
grep -rn "SessionChangedConcurrently" src test --include=*.cs
```

Esperado: sin resultados.

- [x] **Verlo pasar:** el mismo comando de tests → todas PASS.

- [x] **Commit:**

```bash
git add src/Aurora.Flowboard.Application/Authentication/ChangePassword/ChangePasswordHandler.cs
git add src/Aurora.Flowboard.Application/Authentication/AuthenticationErrors.cs
git add test/Aurora.Flowboard.Application.UnitTests/Authentication/ChangePasswordHandlerTests.cs
git add docs/plans/2026-10-16-optimistic-concurrency-plan.md
git commit -m "Let the pipeline translate the change password concurrency conflict" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarea 8: `xmin` en los aggregate roots y migración

**Skill:** `domain-entity` (configuración de EF y migración)

**Ficheros:**
- Crear: `src/Aurora.Flowboard.Infrastructure/Database/ConcurrencyTokenExtensions.cs`
- Modificar: `src/Aurora.Flowboard.Infrastructure/Configurations/ProjectConfiguration.cs`, `MilestoneConfiguration.cs`, `ComponentConfiguration.cs`, `WorkItemConfiguration.cs`, `UserConfiguration.cs`, `UserTokenConfiguration.cs`
- Crear: `src/Aurora.Flowboard.Infrastructure/Database/Migrations/<timestamp>_AddConcurrencyTokens.cs` y `.Designer.cs`
- Modificar: `src/Aurora.Flowboard.Infrastructure/Database/Migrations/ApplicationDbContextModelSnapshot.cs`

Sin tests unitarios (Infrastructure no tiene proyecto de tests); se verifica con el build, el SQL de la migración y `has-pending-model-changes`.

- [x] **Crear la extensión** `src/Aurora.Flowboard.Infrastructure/Database/ConcurrencyTokenExtensions.cs`:

```csharp
namespace Aurora.Flowboard.Infrastructure.Database;

internal static class ConcurrencyTokenExtensions
{
    // Postgres' xmin system column as an optimistic concurrency token: an UPDATE or DELETE of a row that
    // changed after it was read affects 0 rows, and EF throws DbUpdateConcurrencyException.
    // Npgsql emits no SQL for xmin, so the migration only changes the model snapshot.
    private const string VersionPropertyName = "Version";
    private const string VersionColumnName = "xmin";

    internal static EntityTypeBuilder<TEntity> HasXminConcurrencyToken<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        // Explicit column name so EFCore.NamingConventions does not rename it to "version".
        builder.Property<uint>(VersionPropertyName)
            .IsRowVersion()
            .HasColumnName(VersionColumnName);

        return builder;
    }
}
```

- [x] **Llamarla en las configuraciones.** En cada uno de `ProjectConfiguration.cs`, `MilestoneConfiguration.cs`, `ComponentConfiguration.cs`, `WorkItemConfiguration.cs` y `UserConfiguration.cs`:
  - añadir `using Aurora.Flowboard.Infrastructure.Database;` al final del bloque de `using`;
  - justo después de `builder.HasKey(x => x.Id);`, añadir una línea en blanco y `builder.HasXminConcurrencyToken();`. Ejemplo en `ProjectConfiguration.cs`:

```csharp
        builder.ToTable("projects");

        builder.HasKey(x => x.Id);

        builder.HasXminConcurrencyToken();
```

En `UserTokenConfiguration.cs`:
  - añadir `using Aurora.Flowboard.Infrastructure.Database;` debajo de `using Aurora.Flowboard.Domain.Users;`;
  - eliminar el comentario y las dos constantes del principio de la clase:

```csharp
    // Postgres' xmin system column, used as an optimistic concurrency token: two requests that
    // redeem the same refresh token cannot both update the row. No migration column is generated.
    private const string VersionPropertyName = "Version";
    private const string VersionColumnName = "xmin";

```

  - sustituir

```csharp
        // Explicit column name so EFCore.NamingConventions does not rename it to "version".
        builder.Property<uint>(VersionPropertyName)
            .IsRowVersion()
            .HasColumnName(VersionColumnName);
```

por

```csharp
        // Two requests that redeem the same refresh token cannot both update the row.
        builder.HasXminConcurrencyToken();
```

- [x] **Compilar:**

```bash
dotnet build src/Aurora.Flowboard.Api/Aurora.Flowboard.Api.csproj
```

Esperado: build limpio.

- [x] **Generar la migración:**

```bash
dotnet ef migrations add AddConcurrencyTokens --project src/Aurora.Flowboard.Infrastructure --startup-project src/Aurora.Flowboard.Api
```

Esperado en `<timestamp>_AddConcurrencyTokens.cs`: en `Up`, cinco `migrationBuilder.AddColumn<uint>(name: "xmin", ..., type: "xid", rowVersion: true, nullable: false, defaultValue: 0u)` para `projects`, `milestones`, `components`, `work_items` y `users` (schema `flowboard`); en `Down`, los cinco `DropColumn` de `xmin`. Nada para `user_tokens` (ya estaba en el snapshot desde `HashRefreshTokens`). Si aparece cualquier otra operación, parar: el modelo ha cambiado más de lo previsto.

- [x] **Comprobar que no emite SQL:**

```bash
dotnet ef migrations script HashRefreshTokens AddConcurrencyTokens --project src/Aurora.Flowboard.Infrastructure --startup-project src/Aurora.Flowboard.Api
```

Esperado: solo `START TRANSACTION;`, el `INSERT INTO flowboard."__EFMigrationsHistory" ... 'AddConcurrencyTokens' ...` y `COMMIT;`. Ningún `ALTER TABLE`. Si aparece un `ALTER TABLE ... ADD xmin`, parar e informar: aplicarlo fallaría (columna de sistema).

- [x] **Comprobar el modelo:**

```bash
dotnet build src/Aurora.Flowboard.Api/Aurora.Flowboard.Api.csproj
dotnet ef migrations has-pending-model-changes --project src/Aurora.Flowboard.Infrastructure --startup-project src/Aurora.Flowboard.Api
```

Esperado: `No changes have been made to the model since the last migration.`

- [x] **Tests** (que nada se haya roto en las capas que compilan contra Infrastructure):

```bash
dotnet build "Aurora Flowboard.slnx"
./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests.exe
./test/Aurora.Flowboard.ArchitectureTests/bin/Debug/net10.0/Aurora.Flowboard.ArchitectureTests.exe
```

- [x] **Commit:**

```bash
git add src/Aurora.Flowboard.Infrastructure/Database/ConcurrencyTokenExtensions.cs
git add src/Aurora.Flowboard.Infrastructure/Configurations/ProjectConfiguration.cs
git add src/Aurora.Flowboard.Infrastructure/Configurations/MilestoneConfiguration.cs
git add src/Aurora.Flowboard.Infrastructure/Configurations/ComponentConfiguration.cs
git add src/Aurora.Flowboard.Infrastructure/Configurations/WorkItemConfiguration.cs
git add src/Aurora.Flowboard.Infrastructure/Configurations/UserConfiguration.cs
git add src/Aurora.Flowboard.Infrastructure/Configurations/UserTokenConfiguration.cs
git add src/Aurora.Flowboard.Infrastructure/Database/Migrations/*_AddConcurrencyTokens.cs
git add src/Aurora.Flowboard.Infrastructure/Database/Migrations/*_AddConcurrencyTokens.Designer.cs
git add src/Aurora.Flowboard.Infrastructure/Database/Migrations/ApplicationDbContextModelSnapshot.cs
git add docs/plans/2026-10-16-optimistic-concurrency-plan.md
git commit -m "Map xmin as concurrency token on aggregate roots" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Tarea 9: los endpoints de comandos declaran el 409

**Skill:** `create-feature` (endpoints existentes)

**Ficheros:** modificar los 30 endpoints de comandos que hoy no declaran `Status409Conflict` (`Login`, `Logout` y `RefreshToken` quedan fuera: sus conflictos no dan 409):

`Components/RetireComponent.cs`, `Milestones/{ActivateMilestone,ArchiveMilestone,CompleteMilestone,HoldMilestone,UpdateMilestone}.cs`, `Projects/{ActivateProject,ArchiveProject,ChangeProjectKind,CompleteProject,MaintenanceProject,RemoveProjectMember,UpdateProject}.cs`, `WorkItems/{AddWorkItemComment,AddWorkItemTag,AssignWorkItem,ChangeWorkItemComponent,ChangeWorkItemMilestone,LogWorkItemTime,MoveWorkItem,RemoveWorkItemComment,RemoveWorkItemTag,UnassignWorkItem,UpdateWorkItemComment,UpdateWorkItemDescription,UpdateWorkItemEstimatedCompletionDate,UpdateWorkItemEstimatedPoints,UpdateWorkItemPriority,UpdateWorkItemTitle,UpdateWorkItemType}.cs` (todos bajo `src/Aurora.Flowboard.Api/Endpoints/`).

Sin tests (la API no tiene proyecto de tests); se verifica con grep, build y Swagger.

- [x] **Añadir la línea.** Cada uno tiene exactamente una línea `.Produces<ProblemDetails>(StatusCodes.Status404NotFound)`; insertar debajo `.Produces<ProblemDetails>(StatusCodes.Status409Conflict)` con la misma sangría (el patrón respeta ficheros CRLF y LF):

```bash
cd src/Aurora.Flowboard.Api/Endpoints
FILES="Components/RetireComponent.cs Milestones/ActivateMilestone.cs Milestones/ArchiveMilestone.cs Milestones/CompleteMilestone.cs Milestones/HoldMilestone.cs Milestones/UpdateMilestone.cs Projects/ActivateProject.cs Projects/ArchiveProject.cs Projects/ChangeProjectKind.cs Projects/CompleteProject.cs Projects/MaintenanceProject.cs Projects/RemoveProjectMember.cs Projects/UpdateProject.cs WorkItems/AddWorkItemComment.cs WorkItems/AddWorkItemTag.cs WorkItems/AssignWorkItem.cs WorkItems/ChangeWorkItemComponent.cs WorkItems/ChangeWorkItemMilestone.cs WorkItems/LogWorkItemTime.cs WorkItems/MoveWorkItem.cs WorkItems/RemoveWorkItemComment.cs WorkItems/RemoveWorkItemTag.cs WorkItems/UnassignWorkItem.cs WorkItems/UpdateWorkItemComment.cs WorkItems/UpdateWorkItemDescription.cs WorkItems/UpdateWorkItemEstimatedCompletionDate.cs WorkItems/UpdateWorkItemEstimatedPoints.cs WorkItems/UpdateWorkItemPriority.cs WorkItems/UpdateWorkItemTitle.cs WorkItems/UpdateWorkItemType.cs"
sed -i 's|^\( *\)\.Produces<ProblemDetails>(StatusCodes\.Status404NotFound)\(\r\?\)$|&\n\1.Produces<ProblemDetails>(StatusCodes.Status409Conflict)\2|' $FILES
grep -L "Status409Conflict" $FILES
grep -c "Status409Conflict" $FILES | grep -v ":1$"
cd ../../../..
```

Esperado: los dos `grep` no imprimen nada (todos tienen exactamente un 409). `git diff --stat src/Aurora.Flowboard.Api/Endpoints` → 30 ficheros, 30 inserciones.

Resultado en `CompleteMilestone.cs`:

```csharp
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
```

- [x] **Compilar:**

```bash
dotnet build src/Aurora.Flowboard.Api/Aurora.Flowboard.Api.csproj
./test/Aurora.Flowboard.ArchitectureTests/bin/Debug/net10.0/Aurora.Flowboard.ArchitectureTests.exe
```

- [ ] **Verificación manual:** arrancar la API (`dotnet run --project "src/Aurora.Flowboard.AppHost"`), abrir Swagger y comprobar que `PATCH /api/v1/flowboard/milestones/{id}/complete` y `DELETE /api/v1/flowboard/projects/{id}/members/{userId}` listan la respuesta 409. Detener la API.

> Desviación: el `sed -i` del plan se sustituyó por `perl -pi` con el mismo patrón (en Git Bash, `sed -i` reescribe los ficheros CRLF como LF). La verificación en Swagger queda pendiente del usuario (requiere arrancar Aspire); se incluye en la verificación manual final.

- [x] **Commit:**

```bash
for f in $(git diff --name-only src/Aurora.Flowboard.Api/Endpoints); do git add "$f"; done
git add docs/plans/2026-10-16-optimistic-concurrency-plan.md
git commit -m "Declare 409 on command endpoints" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

Antes de commitear, `git status` debe mostrar solo los 30 endpoints y el plan como staged.

---

### Tarea 10: documentación de las convenciones

**Skill:** —

**Ficheros:**
- Crear: `.claude/rules/concurrency.md`
- Modificar: `.claude/rules/authentication.md`, `CLAUDE.md`, `.claude/skills/create-feature/SKILL.md`, `.claude/skills/domain-entity/SKILL.md`, `.claude/skills/domain-entity/references/ef-configuration.md`

- [x] **Crear `.claude/rules/concurrency.md`:**

````markdown
---
paths:
  - "src/Aurora.Flowboard.Application/**/*.cs"
  - "src/Aurora.Flowboard.Infrastructure/Configurations/**/*.cs"
  - "src/Aurora.Flowboard.Infrastructure/Database/**/*.cs"
---

# Concurrency and persistence conflicts

Decision record: `docs/architecture/adr/0001-optimistic-concurrency.md`.

## `xmin` on aggregate roots

- `Project`, `Milestone`, `Component`, `WorkItem`, `User` and `UserToken` call `builder.HasXminConcurrencyToken()` (`Infrastructure/Database/ConcurrencyTokenExtensions.cs`): a shadow `uint Version` mapped to Postgres' `xmin` with `IsRowVersion()`. Npgsql emits no SQL for it; adding it to an entity only changes the model snapshot (check with `dotnet ef migrations script`).
- A new aggregate root that can be edited over HTTP calls it too. `TemplateFlow` does not: only the seeder writes it.
- `xmin` only guards the rows EF updates. A domain method that changes children an invariant of the root depends on must also write the root (`UpdatedOnUtc`), as `Project.AddMember` and `Project.RemoveMember` do; otherwise two concurrent changes to the children never compete for the root row. Append-only children with no invariant on the root (work item comments, time entries) don't.

## Serializable for write skew

`xmin` cannot see a race where each request reads one row and writes another. Those handlers open `dbContext.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)` before their first read, save, and `CommitAsync`; a domain error returns without committing. Postgres (SSI) aborts one side with `40001`.

| Reads to decide | Writes | Handlers (all Serializable) |
|---|---|---|
| Open work items of a milestone / the milestone status | Milestone / work item | `ChangeMilestoneStatusHandler`, `ChangeWorkItemMilestoneHandler`, `CreateWorkItemHandler` |
| Open work items of a component / the component status | Component / work item | `RetireComponentHandler`, `ChangeWorkItemComponentHandler`, `CreateWorkItemHandler` |
| Other active Administrators | The demoted user | `ChangeUserRoleHandler`, only when demoting an active Administrator |

- SSI only protects transactions that are **all** serializable: a new handler on either side of these races must open one too.
- `WorkItemFieldUpdateHandler<TCommand>` opens the transaction when a subclass overrides `TransactionIsolationLevel`.
- Handler tests substitute `IDbContextTransaction` for `BeginTransactionAsync(Arg.Is(IsolationLevel.Serializable), ...)` and assert `CommitAsync` on success and no commit on a domain error. `CreateWorkItemHandler` has no handler tests (`FromSqlRaw` can't run on the mocked `DbSet`).

## Translation to 409

`PersistenceConflictBehavior` is the innermost command decorator (`Logging → Performance → Validation → PersistenceConflict → Handler`); queries don't have it.

| Exception | Error (409) |
|---|---|
| `DbUpdateConcurrencyException` | `PersistenceErrors.ConcurrencyConflict` |
| `DbException` with SqlState `40001`, direct (query, `CommitAsync`) or inside a `DbUpdateException` | `PersistenceErrors.ConcurrencyConflict` |
| `DbUpdateException` with an inner `DbException` with SqlState `23505` | `PersistenceErrors.UniqueViolation` |

- Anything else propagates to `GlobalExceptionHandler` (500). The behavior logs a warning with the exception, which keeps the constraint name.
- Handlers don't catch these exceptions themselves, except when the answer is not a 409: `RefreshTokenHandler` (→ `InvalidRefreshToken`, 401) and `LogoutHandler` (→ `Ok`).
- Uniqueness pre-checks (`AnyAsync` on prefix, email, names) stay: they return the specific domain error in the normal case; the behavior only covers the race.
````

- [x] **`.claude/rules/authentication.md`.** Sustituir

```markdown
- `UserToken` has a shadow `uint` property `Version` mapped to Postgres' `xmin` (`IsRowVersion()` + explicit `HasColumnName("xmin")`, otherwise EFCore.NamingConventions renames it). EF scaffolds an `AddColumn` for it in the migration, but Npgsql emits no SQL for `xmin`.
- Refresh, logout and change password catch `DbUpdateConcurrencyException` on `SaveChangesAsync`:
  - refresh → `InvalidRefreshToken` (another request redeemed the token first);
  - logout → `Ok` (the session is closed either way);
  - change password → `SessionChangedConcurrently` (409), so the client retries and the retry revokes the rotated token.
```

por

```markdown
- `UserToken` maps Postgres' `xmin` as concurrency token with `HasXminConcurrencyToken()`, like every aggregate root (see `concurrency.md`).
- Refresh and logout catch `DbUpdateConcurrencyException` on `SaveChangesAsync`, because their answer is not a 409:
  - refresh → `InvalidRefreshToken` (another request redeemed the token first);
  - logout → `Ok` (the session is closed either way).
- Change password does not catch it: `PersistenceConflictBehavior` returns `Persistence.ConcurrencyConflict` (409), the client retries and the retry revokes the rotated token.
```

- [x] **`CLAUDE.md`.** Sustituir en la viñeta **CQRS**

```markdown
Pipeline: `LoggingBehavior → PerformanceBehavior → ValidationBehavior → Handler`.
```

por

```markdown
Pipeline: `LoggingBehavior → PerformanceBehavior → ValidationBehavior → PersistenceConflictBehavior → Handler` (the last one, commands only, turns concurrency and unique-index conflicts into a 409 `Result`).
```

y, en la tabla de "Area-specific rules", debajo de la fila de `authentication.md`, añadir:

```markdown
| `concurrency.md` | `xmin` on aggregate roots, writing the root for child changes, `Serializable` for write skew, 409 translation in `PersistenceConflictBehavior` |
```

- [x] **`.claude/skills/create-feature/SKILL.md`.** En la sección `## The behavior pipeline`, sustituir

````markdown
```
LoggingBehavior → PerformanceBehavior → ValidationBehavior → handler
```

Logging wraps validation so the log captures the full round-trip, including validation failures. All three decorate commands **and** queries.
````

por

````markdown
```
LoggingBehavior → PerformanceBehavior → ValidationBehavior → PersistenceConflictBehavior → handler
```

Logging wraps validation so the log captures the full round-trip, including validation failures. The first three decorate commands **and** queries. `PersistenceConflictBehavior` decorates commands only and turns `DbUpdateConcurrencyException`, `40001` and `23505` into a 409 `Result`, so a handler doesn't catch them itself. A handler that reads another row or aggregate to decide (a milestone's status, "another active Administrator") runs in a `Serializable` transaction — see `.claude/rules/concurrency.md`.
````

- [x] **`.claude/skills/domain-entity/SKILL.md`.** En la tabla de "Persistence consequences", añadir al final:

```markdown
| Aggregate root editable over HTTP | `builder.HasXminConcurrencyToken()` — optimistic concurrency on Postgres' `xmin` (`.claude/rules/concurrency.md`) |
```

- [x] **`.claude/skills/domain-entity/references/ef-configuration.md`.** En `## Concurrency, shadow properties, ignoring`, sustituir

```csharp
builder.Property<byte[]>("row_version")
    .IsRowVersion();
```

por

```csharp
// Optimistic concurrency on an aggregate root: Postgres' xmin (Infrastructure/Database/ConcurrencyTokenExtensions.cs).
// Never a byte[] row_version: Postgres has no rowversion type. See .claude/rules/concurrency.md.
builder.HasXminConcurrencyToken();
```

- [x] **Comprobar enlaces y nombres:**

```bash
grep -rn "SessionChangedConcurrently\|row_version" .claude CLAUDE.md
```

Esperado: sin resultados.

> Desviación: queda un único resultado, intencionado: el comentario nuevo de `ef-configuration.md` ("Never a byte[] row_version") que el propio plan introduce.

- [x] **Commit:**

```bash
git add .claude/rules/concurrency.md
git add .claude/rules/authentication.md
git add CLAUDE.md
git add .claude/skills/create-feature/SKILL.md
git add .claude/skills/domain-entity/SKILL.md
git add .claude/skills/domain-entity/references/ef-configuration.md
git add docs/plans/2026-10-16-optimistic-concurrency-plan.md
git commit -m "Document optimistic concurrency conventions" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

## Verificación final

- [ ] `dotnet-test-runner` limpio (build + los tres ejecutables de tests).
- [ ] `dotnet ef migrations has-pending-model-changes --project src/Aurora.Flowboard.Infrastructure --startup-project src/Aurora.Flowboard.Api` → sin cambios pendientes.
- [ ] `arch-guard` y `code-reviewer` en paralelo, con la lista de ficheros del mapa, el diseño del spec y su "Fuera de alcance".
- [ ] Verificación manual contra Postgres. Arrancar con `dotnet run --project "src/Aurora.Flowboard.AppHost"` (aplica la migración al arrancar), obtener un token con `POST /api/v1/flowboard/auth/login` del Administrator sembrado (credenciales de la sección `Bootstrap` de la configuración local) y abrir `psql` contra la base del contenedor de Aspire (cadena de conexión en el dashboard).
  1. **`xmin` → 409.** Con un proyecto con dos admins `A` y `B`:
     - `psql`: `BEGIN; SELECT id FROM flowboard.projects WHERE id = '<projectId>' FOR UPDATE;`
     - Lanzar `DELETE /api/v1/flowboard/projects/<projectId>/members/<B>` como `A`: queda esperando (su `UPDATE projects` espera el bloqueo).
     - `psql`: `UPDATE flowboard.projects SET updated_on_utc = now() WHERE id = '<projectId>'; COMMIT;`
     - Esperado: la petición responde **409** con `"title": "The result of the request is a Persistence.ConcurrencyConflict failure"`; `B` sigue siendo miembro.
  2. **`23505` → 409.**
     - `psql`: `BEGIN; INSERT INTO flowboard.users (id, first_name, last_name, email, password_hash, is_active, created_on_utc) VALUES (gen_random_uuid(), 'Race', 'Test', 'race.test@example.com', 'x', true, now());` (sin `COMMIT`).
     - Lanzar `POST /api/v1/flowboard/users` con el email `race.test@example.com`: la comprobación previa no ve la fila sin confirmar y el `INSERT` espera en el índice único.
     - `psql`: `COMMIT;`
     - Esperado: **409** `Persistence.UniqueViolation`. Limpieza: `DELETE FROM flowboard.users WHERE email = 'race.test@example.com';`
  3. **SSI protege el cierre del milestone.** Con un milestone `M` en `Active` sin work items abiertos y un work item abierto `W` sin milestone del mismo proyecto:
     - `psql`: `BEGIN ISOLATION LEVEL SERIALIZABLE; SELECT status FROM flowboard.milestones WHERE id = '<M>'; UPDATE flowboard.work_items SET milestone_id = '<M>' WHERE id = '<W>';` (sin `COMMIT`).
     - Lanzar `PATCH /api/v1/flowboard/milestones/<M>/complete`: responde 202 (no ve la asignación sin confirmar).
     - `psql`: `COMMIT;`
     - Esperado: `ERROR: could not serialize access due to read/write dependencies among transactions` (SQLSTATE `40001`); `W` queda sin milestone y `M` en `Completed`. Si la API no usara `Serializable`, el `COMMIT` pasaría y el milestone quedaría cerrado con un work item abierto. Cuando es la transacción de la API la que aborta, el `40001` llega al behavior y sale como 409; ese camino lo cubren `PersistenceConflictBehaviorTests`.
