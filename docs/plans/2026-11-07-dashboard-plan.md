# Dashboard y búsqueda de work items — Plan de implementación

**Fecha:** 2026-11-07
**Estado:** Aprobado
**Tickets:** —
**Rama:** `feature/dashboard` · **PR:** —
**Spec:** `docs/specs/2026-11-07-dashboard-design.md`

**Objetivo:** que la API calcule, con una sola definición de abierto, backlog, en vuelo y estancado, los datos del Home y de My Issues: `GET dashboard`, `GET work-items` y los campos nuevos del board y del detalle.

**Arquitectura:** enfoque A del spec. `WorkItem` guarda `EnteredCurrentStateOnUtc`, y la etapa se deduce de los estados `Active` con `FlowState.GetBacklogStateId`, sin persistirla. `GET dashboard` hace 8 lecturas, y todos los contadores de trabajo abierto salen de un único `GROUP BY` que `DashboardCalculator`, una función pura, convierte en la respuesta. Hay dos conjuntos de proyectos, mis proyectos y mis proyectos activos, definidos una sola vez en `MyProjectsQueryExtensions`.

## Restricciones globales

- Reglas de `CLAUDE.md` y de `.claude/rules/`, en especial `ef-core-queries.md` (conteos proyectados, no proyectar `User`, ternarios en vez de `switch` en árboles de expresión, paginación con `ToPagedResponseAsync`), `unit-tests.md` (`_dbContext.Seed(...)`, paginación probada a través de un límite de página), `work-items.md` y `architecture-tests.md`.
- Warnings como errores, con SonarAnalyzer y `AnalysisMode` `All`: si el analizador marca algo en el código de este plan, se corrige respetando la intención, y se anota en el commit.
- **Nunca `dotnet test`.** Se compila el proyecto y se ejecuta su `.exe`, y hay que confirmar que el runner imprimió un número de tests.
- Cada tarea se implementa siguiendo la skill indicada en su línea **Skill:**. Invócala (Skill tool) antes de escribir el código de la tarea, aunque empieces sin contexto.
- Se ejecuta con la skill `execute-plan`. Aprobar el plan (`**Estado:** Aprobado`) autoriza un commit por tarea; nunca push ni PR sin que el usuario lo pida.
- **No añadir nunca al commit** `.claude/settings.local.json` ni `src/Aurora.Flowboard.Api/appsettings.Development.json`: son cambios locales del usuario que ya estaban antes de este plan.
- Los comandos se ejecutan desde la raíz del repo (`C:\SourcesGG\aurora-flowboard-api`) con la herramienta Bash, no PowerShell.

## Mapa de ficheros

| Fichero | Acción | Responsabilidad |
|---|---|---|
| `src/Aurora.Flowboard.Domain/WorkItems/WorkItem.cs` | Modificar | `EnteredCurrentStateOnUtc`, que fijan `Create` y `Move`, y `StalledThresholdDays` |
| `src/Aurora.Flowboard.Domain/Projects/FlowStateStage.cs` | Crear | Enum `Backlog` / `InFlight` |
| `src/Aurora.Flowboard.Domain/Projects/FlowState.cs` | Modificar | `GetBacklogStateId`, la regla pura de la etapa |
| `src/Aurora.Flowboard.Infrastructure/Configurations/WorkItemConfiguration.cs` | Modificar | Columna requerida e índice `(flow_state_id, entered_current_state_on_utc)` |
| `src/Aurora.Flowboard.Infrastructure/Database/Migrations/<timestamp>_AddWorkItemEnteredCurrentState.cs` | Crear (generar y editar) | Columna nullable, backfill, `NOT NULL` y cambio de índice |
| `src/Aurora.Flowboard.Application/Projects/Shared/MyProjectsQueryExtensions.cs` | Crear | Los dos conjuntos de proyectos y el orden del Sidebar |
| `src/Aurora.Flowboard.Application/Projects/Shared/FlowStateStageResolver.cs` | Crear | Ids de backlog de varios proyectos a la vez |
| `src/Aurora.Flowboard.Application/Users/GetMySummary/GetMySummaryHandler.cs` | Modificar | `myOpenIssues` sobre mis proyectos activos |
| `src/Aurora.Flowboard.Application/Projects/GetBoard/BoardColumnResponse.cs` | Modificar | `Stage` |
| `src/Aurora.Flowboard.Application/Projects/GetBoard/BoardWorkItemResponse.cs` | Modificar | `EnteredCurrentStateOnUtc` |
| `src/Aurora.Flowboard.Application/Projects/GetBoard/GetProjectBoardHandler.cs` | Modificar | Calcula `Stage` y proyecta la fecha |
| `src/Aurora.Flowboard.Application/WorkItems/GetByCode/WorkItemResponse.cs` | Modificar | `EnteredCurrentStateOnUtc` |
| `src/Aurora.Flowboard.Application/WorkItems/GetByCode/GetWorkItemByCodeHandler.cs` | Modificar | Proyecta la fecha |
| `src/Aurora.Flowboard.Application/WorkItems/GetWorkItems/GetWorkItemsQuery.cs` | Crear | Query paginada con los filtros como `string?` |
| `src/Aurora.Flowboard.Application/WorkItems/GetWorkItems/WorkItemSearchFilters.cs` | Crear | Valores aceptados y su parseo |
| `src/Aurora.Flowboard.Application/WorkItems/GetWorkItems/GetWorkItemsValidator.cs` | Crear | 400 para cualquier valor inválido |
| `src/Aurora.Flowboard.Application/WorkItems/GetWorkItems/WorkItemListItemResponse.cs` | Crear | Item de la búsqueda |
| `src/Aurora.Flowboard.Application/WorkItems/GetWorkItems/GetWorkItemsHandler.cs` | Crear | Alcance, filtros, orden y paginación |
| `src/Aurora.Flowboard.Application/Dashboard/GetDashboard/DashboardResponse.cs` | Crear | Contrato de `GET dashboard` |
| `src/Aurora.Flowboard.Application/Dashboard/GetDashboard/DashboardSnapshot.cs` | Crear | Filas leídas por el handler, entrada del calculador |
| `src/Aurora.Flowboard.Application/Dashboard/GetDashboard/DashboardCalculator.cs` | Crear | Función pura que arma la respuesta |
| `src/Aurora.Flowboard.Application/Dashboard/GetDashboard/GetDashboardQuery.cs` | Crear | Query sin parámetros |
| `src/Aurora.Flowboard.Application/Dashboard/GetDashboard/GetDashboardHandler.cs` | Crear | Las 8 lecturas |
| `src/Aurora.Flowboard.Api/Endpoints/EndpointTags.cs` | Modificar | `Dashboard` |
| `src/Aurora.Flowboard.Api/Endpoints/WorkItems/GetWorkItems.cs` | Crear | `GET work-items` |
| `src/Aurora.Flowboard.Api/Endpoints/Dashboard/GetDashboard.cs` | Crear | `GET dashboard` |
| `src/Aurora.Flowboard.Api/Aurora.Flowboard.Api.http` | Modificar | Peticiones de prueba |
| `test/Aurora.Flowboard.Domain.UnitTests/WorkItems/WorkItemTests.cs` | Modificar | Tests de la fecha en `Create` y `Move` |
| `test/Aurora.Flowboard.Domain.UnitTests/Projects/FlowStateTests.cs` | Crear | Tests de `GetBacklogStateId` |
| `test/Aurora.Flowboard.Application.UnitTests/GlobalUsings.cs` | Modificar | Namespaces de los slices nuevos |
| `test/Aurora.Flowboard.Application.UnitTests/Users/GetMySummaryHandlerTests.cs` | Modificar | Proyectos `Completed` y `Maintenance` |
| `test/Aurora.Flowboard.Application.UnitTests/Projects/GetProjectBoardHandlerTests.cs` | Modificar | `Stage` y fecha |
| `test/Aurora.Flowboard.Application.UnitTests/WorkItems/GetWorkItemByCodeHandlerTests.cs` | Modificar | Fecha |
| `test/Aurora.Flowboard.Application.UnitTests/WorkItems/GetWorkItemsValidatorTests.cs` | Crear | Cada valor inválido |
| `test/Aurora.Flowboard.Application.UnitTests/WorkItems/GetWorkItemsQueryData.cs` | Crear | Fixtures entre proyectos, compartidas con el dashboard |
| `test/Aurora.Flowboard.Application.UnitTests/WorkItems/GetWorkItemsHandlerTests.cs` | Crear | Alcance, filtros, orden y paginación |
| `test/Aurora.Flowboard.Application.UnitTests/Dashboard/DashboardQueryData.cs` | Crear | Filas para el calculador |
| `test/Aurora.Flowboard.Application.UnitTests/Dashboard/DashboardCalculatorTests.cs` | Crear | Reglas puras |
| `test/Aurora.Flowboard.Application.UnitTests/Dashboard/GetDashboardHandlerTests.cs` | Crear | Lecturas, límites de los tramos y equivalencia con `my-summary` |
| `.claude/rules/work-items.md` | Modificar | Búsqueda, etapa y estancado |
| `.claude/rules/dashboard.md` | Crear | Conjuntos de proyectos, consulta agregada y límite de consistencia |
| `CLAUDE.md` | Modificar | Fila de `dashboard.md` en la tabla de rules |

`GetWorkItemsQueryData` sirve a la búsqueda y al dashboard, porque los dos necesitan proyectos con varios estados `Active` e items con una antigüedad exacta. `DashboardQueryData` solo construye filas para el calculador. Ningún fichero existente crece de forma preocupante.

## Tareas

### Tarea 1: `WorkItem.EnteredCurrentStateOnUtc`

**Skill:** `domain-entity`

**Ficheros:**
- Modificar: `src/Aurora.Flowboard.Domain/WorkItems/WorkItem.cs`
- Test: `test/Aurora.Flowboard.Domain.UnitTests/WorkItems/WorkItemTests.cs`

- [x] **Escribir los tests que fallan.** Al final de la clase anidada `WorkItemTests.Create`, añade:

```csharp
        [Fact]
        public void Should_SetEnteredCurrentStateOnUtc_ToCreatedOnUtc_When_Created()
        {
            // Arrange
            var (project, admin) = WorkItemData.GetActiveProjectWithFlow();

            // Act
            Result<WorkItem> result = WorkItem.Create(
                WorkItemData.Title,
                WorkItemData.Description,
                WorkItemData.Type,
                WorkItemData.Priority,
                project,
                admin,
                WorkItemData.EstimatedPoints,
                WorkItemData.EstimatedCompletionDate,
                WorkItemData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            result.Value.EnteredCurrentStateOnUtc.Should().Be(WorkItemData.CreatedOnUtc);
        }
```

  Al final de la clase anidada `WorkItemTests.Move`, añade los siguientes. El flujo no genera transiciones que salgan de un estado de cierre, así que la "reapertura" del spec se prueba volviendo a un estado `Active` anterior, que es el mismo caso: la fecha se reinicia en cada movimiento.

```csharp
        [Fact]
        public void Should_SetEnteredCurrentStateOnUtc_When_Moved()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            FlowState toState = project.FlowStates.Single(s => s.Name == "In Progress");

            // Act
            Result result = workItem.Move(toState, admin, null, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.EnteredCurrentStateOnUtc.Should().Be(WorkItemData.UpdatedOnUtc);
        }

        [Fact]
        public void Should_ResetEnteredCurrentStateOnUtc_When_MovedBackToAPreviousState()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            FlowState todo = project.FlowStates.Single(s => s.Name == "Todo");
            FlowState inProgress = project.FlowStates.Single(s => s.Name == "In Progress");
            DateTime movedBackOnUtc = WorkItemData.UpdatedOnUtc.AddDays(3);
            workItem.Move(inProgress, admin, null, WorkItemData.UpdatedOnUtc);

            // Act
            Result result = workItem.Move(todo, admin, null, movedBackOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.EnteredCurrentStateOnUtc.Should().Be(movedBackOnUtc);
        }

        [Fact]
        public void Should_SetEnteredCurrentStateOnUtc_When_MovedToAClosingState()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            FlowState cancelledState = project.FlowStates.Single(s => s.Name == "Cancelled");

            // Act
            workItem.Move(cancelledState, admin, null, WorkItemData.UpdatedOnUtc);

            // Assert
            workItem.EnteredCurrentStateOnUtc.Should().Be(WorkItemData.UpdatedOnUtc);
        }

        [Fact]
        public void Should_KeepEnteredCurrentStateOnUtc_When_MoveIsRejected()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            FlowState doneState = project.FlowStates.Single(s => s.Name == "Done");

            // Act
            Result result = workItem.Move(doneState, admin, null, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            workItem.EnteredCurrentStateOnUtc.Should().Be(WorkItemData.CreatedOnUtc);
        }
```

- [x] **Verlo fallar.** No compila hasta que exista la propiedad. Añade solo el stub en `WorkItem`, debajo de `CompletedOnUtc`:

```csharp
    public DateTime EnteredCurrentStateOnUtc { get; private set; }
```

  y ejecuta:

```bash
dotnet build test/Aurora.Flowboard.Domain.UnitTests/Aurora.Flowboard.Domain.UnitTests.csproj
./test/Aurora.Flowboard.Domain.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Domain.UnitTests.exe -method "*EnteredCurrentStateOnUtc*"
```

  Fallo esperado: los 5 tests fallan con `Expected ... to be <2026-01-01 ...> (or <2026-06-01 ...>), but found <0001-01-01 00:00:00>`.

- [x] **Implementar.** En `WorkItem.cs`:

  1. Debajo de `MaxCodeLength`:

```csharp
    // An open, in-flight work item is stalled from this many whole days in its current state.
    public const int StalledThresholdDays = 7;
```

  2. En el constructor privado, después de `CreatedOnUtc = createdOnUtc;`:

```csharp
        EnteredCurrentStateOnUtc = createdOnUtc;
```

  3. En `Move`, después de `FlowState = toState;`:

```csharp
        EnteredCurrentStateOnUtc = changedOnUtc;
```

  Ningún otro método la escribe.

- [x] **Verlo pasar.** Repite los dos comandos anteriores (5 tests en verde) y ejecuta la suite de Domain completa sin filtro:

```bash
./test/Aurora.Flowboard.Domain.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Domain.UnitTests.exe
```

- [x] `dotnet build "Aurora Flowboard.slnx"`: el modelo de EF queda con un cambio pendiente hasta la tarea 3, pero eso no rompe la compilación.
- [x] Commit: `Track when a work item entered its current state` con `src/Aurora.Flowboard.Domain/WorkItems/WorkItem.cs`, `test/Aurora.Flowboard.Domain.UnitTests/WorkItems/WorkItemTests.cs`, `docs/specs/2026-11-07-dashboard-design.md`, `docs/README.md` y este plan.

  > Desviación (aprobada por el usuario): el spec, `docs/README.md` y el plan se commitearon antes, en `ac5a29e` (commit de documentación propio); este commit lleva solo el código y el plan.

### Tarea 2: etapa de un estado (`FlowStateStage` y `FlowState.GetBacklogStateId`)

**Skill:** `domain-entity`

**Ficheros:**
- Crear: `src/Aurora.Flowboard.Domain/Projects/FlowStateStage.cs`
- Modificar: `src/Aurora.Flowboard.Domain/Projects/FlowState.cs`
- Test: `test/Aurora.Flowboard.Domain.UnitTests/Projects/FlowStateTests.cs`

La regla es un método estático de `FlowState` y no una clase estática aparte: `Errors_Should_BeStaticAndHaveErrorsSuffix` exige que toda clase estática del Domain termine en `Errors`.

- [x] **Escribir el test que falla.** Crea `test/Aurora.Flowboard.Domain.UnitTests/Projects/FlowStateTests.cs`:

```csharp
namespace Aurora.Flowboard.Domain.UnitTests.Projects;

public sealed class FlowStateTests
{
    public sealed class GetBacklogStateId
    {
        [Fact]
        public void Should_ReturnNull_When_ThereAreNoActiveStates()
        {
            // Act
            Guid? backlogStateId = FlowState.GetBacklogStateId([]);

            // Assert
            backlogStateId.Should().BeNull();
        }

        [Fact]
        public void Should_ReturnNull_When_ThereIsASingleActiveState()
        {
            // Act
            Guid? backlogStateId = FlowState.GetBacklogStateId([(Guid.NewGuid(), 1)]);

            // Assert
            backlogStateId.Should().BeNull();
        }

        [Fact]
        public void Should_ReturnLowestSortOrder_When_ThereAreSeveralActiveStates()
        {
            // Arrange
            Guid first = Guid.NewGuid();

            // Act
            Guid? backlogStateId = FlowState.GetBacklogStateId([(first, 1), (Guid.NewGuid(), 2), (Guid.NewGuid(), 3)]);

            // Assert
            backlogStateId.Should().Be(first);
        }

        [Fact]
        public void Should_ReturnLowestSortOrder_When_StatesAreUnordered()
        {
            // Arrange
            Guid first = Guid.NewGuid();

            // Act
            Guid? backlogStateId = FlowState.GetBacklogStateId([(Guid.NewGuid(), 3), (first, 1), (Guid.NewGuid(), 2)]);

            // Assert
            backlogStateId.Should().Be(first);
        }

        [Fact]
        public void Should_ReturnFirstActiveState_OfAProjectFlow()
        {
            // Arrange
            Project project = ProjectData.GetProjectWithFlowStates();
            FlowState todo = project.FlowStates.Single(s => s.Name == "Todo");

            // Act
            Guid? backlogStateId = FlowState.GetBacklogStateId([.. project.FlowStates
                .Where(s => s.Category == FlowStateCategory.Active)
                .Select(s => (s.Id, s.SortOrder))]);

            // Assert
            backlogStateId.Should().Be(todo.Id);
        }
    }
}
```

- [x] **Verlo fallar.** Para que compile, crea el enum (código final, abajo) y añade a `FlowState` un stub ingenuo, que toma el primero tal como llega. Lee el parámetro para que el analizador no lo marque como no usado:

```csharp
    public static Guid? GetBacklogStateId(IReadOnlyCollection<(Guid Id, int SortOrder)> activeStates) =>
        activeStates.Count == 0 ? null : activeStates.First().Id;
```

```bash
dotnet build test/Aurora.Flowboard.Domain.UnitTests/Aurora.Flowboard.Domain.UnitTests.csproj
./test/Aurora.Flowboard.Domain.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Domain.UnitTests.exe -class "Aurora.Flowboard.Domain.UnitTests.Projects.FlowStateTests+GetBacklogStateId"
```

  Fallo esperado: `Should_ReturnNull_When_ThereIsASingleActiveState` (`Expected backlogStateId to be <null>, but found {guid}`) y `Should_ReturnLowestSortOrder_When_StatesAreUnordered`. Los otros tres pasan con el stub.

- [x] **Implementar.** Crea `src/Aurora.Flowboard.Domain/Projects/FlowStateStage.cs`:

```csharp
namespace Aurora.Flowboard.Domain.Projects;

// Where an open work item stands in its project's flow. Not persisted: it follows from the
// project's Active states (FlowState.GetBacklogStateId).
public enum FlowStateStage
{
    Backlog = 0,
    InFlight = 1
}
```

  En `FlowState.cs`, sustituye el stub por:

```csharp
    // The backlog is the project's first Active state. A project with a single Active state has no
    // backlog: that state is in flight, so its work items can become stalled.
    public static Guid? GetBacklogStateId(IReadOnlyCollection<(Guid Id, int SortOrder)> activeStates) =>
        activeStates.Count < MinActiveStatesForBacklog
            ? null
            : activeStates.OrderBy(s => s.SortOrder).ThenBy(s => s.Id).First().Id;
```

  y, debajo de `MaxNameLength`:

```csharp
    private const int MinActiveStatesForBacklog = 2;
```

- [x] **Verlo pasar.** Repite el comando con `-class` (5 en verde) y ejecuta la suite de Domain completa.
- [x] `dotnet build "Aurora Flowboard.slnx"` y `./test/Aurora.Flowboard.ArchitectureTests/bin/Debug/net10.0/Aurora.Flowboard.ArchitectureTests.exe`: hay un tipo nuevo en Domain.
- [x] Commit: `Derive the backlog stage from a project's active flow states` con `src/Aurora.Flowboard.Domain/Projects/FlowStateStage.cs`, `src/Aurora.Flowboard.Domain/Projects/FlowState.cs`, `test/Aurora.Flowboard.Domain.UnitTests/Projects/FlowStateTests.cs` y este plan.

### Tarea 3: mapeo de EF y migración `AddWorkItemEnteredCurrentState`

**Skill:** `domain-entity`

**Ficheros:**
- Modificar: `src/Aurora.Flowboard.Infrastructure/Configurations/WorkItemConfiguration.cs`
- Crear: `src/Aurora.Flowboard.Infrastructure/Database/Migrations/<timestamp>_AddWorkItemEnteredCurrentState.cs` y su `.Designer.cs`
- Modificar: `src/Aurora.Flowboard.Infrastructure/Database/Migrations/ApplicationDbContextModelSnapshot.cs` (lo regenera EF)

Infrastructure no tiene tests unitarios, así que esta tarea se verifica a mano contra Postgres.

- [x] **Configurar.** En `WorkItemConfiguration.Configure`, después de `builder.Property(x => x.CompletedOnUtc);`:

```csharp
        builder.Property(x => x.EnteredCurrentStateOnUtc)
            .IsRequired();
```

  y, junto a los demás `HasIndex`, al final:

```csharp
        // The dashboard and GET work-items filter by state and compare the time in state. The index
        // leads with flow_state_id, so it also serves the foreign key and replaces its own index.
        builder.HasIndex(x => new { x.FlowStateId, x.EnteredCurrentStateOnUtc });
```

- [x] **Generar la migración.**

```bash
dotnet build src/Aurora.Flowboard.Infrastructure/Aurora.Flowboard.Infrastructure.csproj
dotnet ef migrations add AddWorkItemEnteredCurrentState --project src/Aurora.Flowboard.Infrastructure --startup-project src/Aurora.Flowboard.Api --output-dir Database/Migrations
```

  Comprueba que la migración generada **elimina** `ix_work_items_flow_state_id` y crea `ix_work_items_flow_state_id_entered_current_state_on_utc`. Si EF no elimina el índice anterior, para y avisa al usuario: el spec da por hecho que lo hace.

- [x] **Editar a mano `Up` y `Down`.** EF genera la columna `NOT NULL` con `defaultValue: new DateTime(1, 1, 1, ...)`, lo que mostraría todos los items como estancados. Deja la clase así, sin tocar el `.Designer.cs` ni el snapshot:

```csharp
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aurora.Flowboard.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkItemEnteredCurrentState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_work_items_flow_state_id",
                schema: "flowboard",
                table: "work_items");

            // Added nullable, filled, then made required: the existing rows need a real value, and
            // EF's default (0001-01-01) would show every work item as stalled.
            migrationBuilder.AddColumn<DateTime>(
                name: "entered_current_state_on_utc",
                schema: "flowboard",
                table: "work_items",
                type: "timestamp with time zone",
                nullable: true);

            // The last transition is when the item entered its current state; an item never moved is
            // still in the state it was created in.
            migrationBuilder.Sql(
                """
                UPDATE flowboard.work_items w
                SET entered_current_state_on_utc = COALESCE(
                    (SELECT MAX(h.changed_on_utc)
                     FROM flowboard.work_item_state_transition_histories h
                     WHERE h.work_item_id = w.id),
                    w.created_on_utc);
                """);

            migrationBuilder.AlterColumn<DateTime>(
                name: "entered_current_state_on_utc",
                schema: "flowboard",
                table: "work_items",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_work_items_flow_state_id_entered_current_state_on_utc",
                schema: "flowboard",
                table: "work_items",
                columns: new[] { "flow_state_id", "entered_current_state_on_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nothing is lost: the column can be computed again from the state transition history.
            migrationBuilder.DropIndex(
                name: "ix_work_items_flow_state_id_entered_current_state_on_utc",
                schema: "flowboard",
                table: "work_items");

            migrationBuilder.DropColumn(
                name: "entered_current_state_on_utc",
                schema: "flowboard",
                table: "work_items");

            migrationBuilder.CreateIndex(
                name: "ix_work_items_flow_state_id",
                schema: "flowboard",
                table: "work_items",
                column: "flow_state_id");
        }
    }
}
```

- [x] **Compilar y comprobar que el modelo cuadra:**

```bash
dotnet build src/Aurora.Flowboard.Infrastructure/Aurora.Flowboard.Infrastructure.csproj
dotnet build src/Aurora.Flowboard.Api/Aurora.Flowboard.Api.csproj
dotnet ef migrations has-pending-model-changes --project src/Aurora.Flowboard.Infrastructure --startup-project src/Aurora.Flowboard.Api
```

  Esperado: `No changes have been made to the model since the last migration.`

- [x] **Revisar el SQL de subida y de bajada:**

```bash
dotnet ef migrations script AddConcurrencyTokens AddWorkItemEnteredCurrentState --project src/Aurora.Flowboard.Infrastructure --startup-project src/Aurora.Flowboard.Api
dotnet ef migrations script AddWorkItemEnteredCurrentState AddConcurrencyTokens --project src/Aurora.Flowboard.Infrastructure --startup-project src/Aurora.Flowboard.Api
```

  Esperado en la subida: `DROP INDEX`, `ADD entered_current_state_on_utc timestamp with time zone` sin `NOT NULL`, el `UPDATE`, `ALTER COLUMN ... SET NOT NULL` sin `DEFAULT` y `CREATE INDEX` sobre las dos columnas. En la bajada: `DROP INDEX` del compuesto, `DROP COLUMN` y `CREATE INDEX ix_work_items_flow_state_id`.

- [ ] **Verificación manual contra Postgres.** Arranca la app con `dotnet run --project src/Aurora.Flowboard.AppHost`, que aplica la migración al iniciar, y en la base ejecuta:

```sql
-- 0 filas: todo item tiene la fecha de su último movimiento o, sin historial, la de creación.
SELECT w.code
FROM flowboard.work_items w
WHERE w.entered_current_state_on_utc IS DISTINCT FROM COALESCE(
    (SELECT MAX(h.changed_on_utc)
     FROM flowboard.work_item_state_transition_histories h
     WHERE h.work_item_id = w.id),
    w.created_on_utc);

-- is_nullable = NO y column_default vacío.
SELECT is_nullable, column_default
FROM information_schema.columns
WHERE table_schema = 'flowboard' AND table_name = 'work_items' AND column_name = 'entered_current_state_on_utc';

-- Aparece ix_work_items_flow_state_id_entered_current_state_on_utc y no ix_work_items_flow_state_id.
SELECT indexname FROM pg_indexes WHERE schemaname = 'flowboard' AND tablename = 'work_items' ORDER BY indexname;
```

  Si la base no tiene items con y sin historial, crea un item y muévelo una vez antes de aplicar la migración, para que haya un caso de cada tipo. Después, con la app parada, prueba `Down` y vuelve a subir:

```bash
dotnet ef database update AddConcurrencyTokens --project src/Aurora.Flowboard.Infrastructure --startup-project src/Aurora.Flowboard.Api
dotnet ef database update --project src/Aurora.Flowboard.Infrastructure --startup-project src/Aurora.Flowboard.Api
```

  Esperado: tras la bajada la columna ya no existe y vuelve `ix_work_items_flow_state_id`; tras la subida, las tres consultas dan lo mismo que antes.

  > Pendiente: la verificación manual contra Postgres la hace el usuario al final (no se puede ejecutar desde esta sesión). La migración generada es `20261109183609_AddWorkItemEnteredCurrentState`; EF eliminó `ix_work_items_flow_state_id` como esperaba el spec, y el SQL de `migrations script` (subida y bajada) coincide con el esperado.

- [x] Commit: `Persist when work items entered their state, with a backfilled migration` con `WorkItemConfiguration.cs`, los dos ficheros de la migración, `ApplicationDbContextModelSnapshot.cs` y este plan.

### Tarea 4: los dos conjuntos de proyectos y `my-summary`

**Skill:** `create-feature`

**Ficheros:**
- Crear: `src/Aurora.Flowboard.Application/Projects/Shared/MyProjectsQueryExtensions.cs`
- Modificar: `src/Aurora.Flowboard.Application/Users/GetMySummary/GetMySummaryHandler.cs`
- Test: `test/Aurora.Flowboard.Application.UnitTests/Users/GetMySummaryHandlerTests.cs`

- [x] **Escribir los tests.** Añade al final de `GetMySummaryHandlerTests`. El primero falla hoy; el segundo protege que `Maintenance` siga contando.

```csharp
    [Fact]
    public async Task Should_ExcludeOpenIssues_InCompletedProjects_ButKeepListingThem()
    {
        // Arrange
        User caller = GetMySummaryQueryData.GetUser();
        Project completedProject = GetMySummaryQueryData.CreateProject("Done Project", "DNP", ProjectStatus.Completed, caller);
        GetMySummaryQueryData.AddWorkItem(completedProject, caller.Id, FlowStateCategory.Active);
        _dbContext.Seed(c => c.Users, caller);
        _dbContext.Seed(c => c.Projects, completedProject);

        // Act
        Result<MySummaryResponse> result =
            await _handler.Handle(new GetMySummaryQuery(caller.Id), CancellationToken.None);

        // Assert
        result.Value.Counts.MyOpenIssues.Should().Be(0);
        result.Value.Counts.Projects.Should().Be(1);
        result.Value.Projects.Should().ContainSingle(p => p.ProjectId == completedProject.Id);
    }

    [Fact]
    public async Task Should_CountOpenIssues_InMaintenanceProjects()
    {
        // Arrange
        User caller = GetMySummaryQueryData.GetUser();
        Project maintenanceProject = GetMySummaryQueryData.CreateProject("Kept Project", "KPT", ProjectStatus.Maintenance, caller);
        GetMySummaryQueryData.AddWorkItem(maintenanceProject, caller.Id, FlowStateCategory.Active);
        _dbContext.Seed(c => c.Users, caller);
        _dbContext.Seed(c => c.Projects, maintenanceProject);

        // Act
        Result<MySummaryResponse> result =
            await _handler.Handle(new GetMySummaryQuery(caller.Id), CancellationToken.None);

        // Assert
        result.Value.Counts.MyOpenIssues.Should().Be(1);
    }
```

- [x] **Verlo fallar.**

```bash
dotnet build test/Aurora.Flowboard.Application.UnitTests/Aurora.Flowboard.Application.UnitTests.csproj
./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests.exe -class "Aurora.Flowboard.Application.UnitTests.Users.GetMySummaryHandlerTests"
```

  Fallo esperado: `Should_ExcludeOpenIssues_InCompletedProjects_ButKeepListingThem` con `Expected result.Value.Counts.MyOpenIssues to be 0, but found 1`. El resto pasa.

- [x] **Implementar.** Crea `src/Aurora.Flowboard.Application/Projects/Shared/MyProjectsQueryExtensions.cs`:

```csharp
namespace Aurora.Flowboard.Application.Projects.Shared;

// The two project sets of the Home and My Issues (docs/specs/2026-11-07-dashboard-design.md, section 2).
// "My projects" feed the Sidebar, the portfolio rows and the member count. "My active projects" feed
// every open-work number, because the work items of a Completed project can no longer be moved.
internal static class MyProjectsQueryExtensions
{
    public static IQueryable<Project> WhereMyProjects(this IQueryable<Project> projects, Guid userId) =>
        projects.Where(p => p.Status != ProjectStatus.Archived && p.Members.Any(m => m.UserId == userId));

    // The same rule as AcceptsOpenWork, written out because a method call cannot be translated to SQL.
    public static IQueryable<Project> WhereMyActiveProjects(this IQueryable<Project> projects, Guid userId) =>
        projects.Where(p => (p.Status == ProjectStatus.Active || p.Status == ProjectStatus.Maintenance)
            && p.Members.Any(m => m.UserId == userId));

    public static bool AcceptsOpenWork(ProjectStatus status) =>
        status is ProjectStatus.Active or ProjectStatus.Maintenance;

    // Sidebar order: status, then name. Applied in memory: OrdinalIgnoreCase has no SQL translation.
    public static IOrderedEnumerable<T> OrderLikeSidebar<T>(
        this IEnumerable<T> projects,
        Func<T, ProjectStatus> status,
        Func<T, string> name) =>
        projects.OrderBy(status).ThenBy(name, StringComparer.OrdinalIgnoreCase);
}
```

  En `GetMySummaryHandler.Handle`, sustituye todo lo que va desde el comentario `// Three scalar queries...` hasta el cálculo de `orderedProjects` (incluido) por:

```csharp
        // Three scalar queries: nothing loads members or work items into memory. The list and the member
        // count cover my projects; the open issues only my active ones (MyProjectsQueryExtensions).
        IQueryable<Project> myProjects = dbContext
            .Projects
            .AsNoTracking()
            .WhereMyProjects(query.UserId);

        List<MyProjectSummaryResponse> projects = await myProjects
            .Select(p => new MyProjectSummaryResponse(p.Id, p.Name, p.Color.Value, p.Status))
            .ToListAsync(cancellationToken);

        int memberCount = await myProjects
            .SelectMany(p => p.Members)
            .Select(m => m.UserId)
            .Distinct()
            .CountAsync(cancellationToken);

        int myOpenIssues = await dbContext
            .Projects
            .AsNoTracking()
            .WhereMyActiveProjects(query.UserId)
            .SelectMany(p => p.WorkItems)
            .CountAsync(
                wi => wi.AssigneeId == query.UserId && wi.FlowState.Category == FlowStateCategory.Active,
                cancellationToken);

        IReadOnlyCollection<MyProjectSummaryResponse> orderedProjects = [.. projects
            .OrderLikeSidebar(p => p.Status, p => p.Name)];
```

  El `return` final no cambia.

- [x] **Verlo pasar.** Repite los dos comandos (todos en verde, `Should_OrderProjects_ByStatusThenAlphabeticallyByName` incluido).
- [x] Commit: `Count my open issues only in projects that still accept work` con `MyProjectsQueryExtensions.cs`, `GetMySummaryHandler.cs`, `GetMySummaryHandlerTests.cs` y este plan.

### Tarea 5: `stage` en el board y `enteredCurrentStateOnUtc` en board y detalle

**Skill:** `create-feature`

**Ficheros:**
- Modificar: `src/Aurora.Flowboard.Application/Projects/GetBoard/BoardColumnResponse.cs`, `BoardWorkItemResponse.cs`, `GetProjectBoardHandler.cs`
- Modificar: `src/Aurora.Flowboard.Application/WorkItems/GetByCode/WorkItemResponse.cs`, `GetWorkItemByCodeHandler.cs`
- Test: `test/Aurora.Flowboard.Application.UnitTests/Projects/GetProjectBoardHandlerTests.cs`, `test/Aurora.Flowboard.Application.UnitTests/WorkItems/GetWorkItemByCodeHandlerTests.cs`

- [x] **Escribir los tests que fallan.** En `GetProjectBoardHandlerTests`, antes del método privado `ArrangeBoard`:

```csharp
    [Fact]
    public async Task Should_MarkFirstColumnAsBacklog_AndTheRestAsInFlight()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        FlowStateDefinition[] flowStates = FlowStateDefinitionData.GetFlow(
            ("Backlog", FlowStateCategory.Active),
            ("In Progress", FlowStateCategory.Active),
            ("QA", FlowStateCategory.Active),
            ("Done", FlowStateCategory.Completed),
            ("Cancelled", FlowStateCategory.Cancelled));
        Project project = Project.Create("Board Project", "Desc", ProjectCode.Create("BRD").Value, ProjectKind.Product, Color.Create("white").Value, flowStates, admin, WorkItemQueryData.UtcNow).Value;
        ArrangeBoard(project, admin.Id, project.FlowTransitions);

        // Act
        Result<IReadOnlyCollection<BoardColumnResponse>> result =
            await _handler.Handle(new GetProjectBoardQuery(project.Id), CancellationToken.None);

        // Assert
        result.Value.Select(c => (c.FlowStateName, c.Stage)).Should().Equal(
            ("Backlog", FlowStateStage.Backlog),
            ("In Progress", FlowStateStage.InFlight),
            ("QA", FlowStateStage.InFlight));
    }

    [Fact]
    public async Task Should_MarkSingleColumnAsInFlight_When_ProjectHasOneActiveState()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        Project project = WorkItemQueryData.GetActiveProjectWithFlow(admin);
        ArrangeBoard(project, admin.Id, project.FlowTransitions);

        // Act
        Result<IReadOnlyCollection<BoardColumnResponse>> result =
            await _handler.Handle(new GetProjectBoardQuery(project.Id), CancellationToken.None);

        // Assert
        result.Value.Should().ContainSingle().Which.Stage.Should().Be(FlowStateStage.InFlight);
    }

    [Fact]
    public async Task Should_MapEnteredCurrentStateOnUtc_FromTheLastMove()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        FlowStateDefinition[] flowStates = FlowStateDefinitionData.GetFlow(
            ("Backlog", FlowStateCategory.Active),
            ("In Progress", FlowStateCategory.Active),
            ("Done", FlowStateCategory.Completed),
            ("Cancelled", FlowStateCategory.Cancelled));
        Project project = Project.Create("Board Project", "Desc", ProjectCode.Create("BRD").Value, ProjectKind.Product, Color.Create("white").Value, flowStates, admin, WorkItemQueryData.UtcNow).Value;
        WorkItem workItem = WorkItem.Create("Item", null, WorkItemType.Story, Priority.Medium, project, admin, null, null, WorkItemQueryData.UtcNow).Value;
        DateTime movedOnUtc = WorkItemQueryData.UtcNow.AddHours(5);
        workItem.Move(project.FlowStates.Single(s => s.Name == "In Progress"), admin, null, movedOnUtc);

        _userContext.UserId.Returns(admin.Id);
        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.FlowStates, project.FlowStates);
        _dbContext.Seed(c => c.FlowTransitions, project.FlowTransitions);
        _dbContext.Seed(c => c.WorkItems, workItem);
        _dbContext.Seed(c => c.Users, admin);
        _dbContext.Seed(c => c.Components);
        _dbContext.Seed(c => c.Milestones);

        // Act
        Result<IReadOnlyCollection<BoardColumnResponse>> result =
            await _handler.Handle(new GetProjectBoardQuery(project.Id), CancellationToken.None);

        // Assert
        result.Value.Single(c => c.FlowStateName == "In Progress").WorkItems
            .Should().ContainSingle()
            .Which.EnteredCurrentStateOnUtc.Should().Be(movedOnUtc);
    }
```

  Al final de `GetWorkItemByCodeHandlerTests`. Es un item cerrado: el detalle devuelve la fecha en que entró al estado terminal (supuesto 9).

```csharp
    [Fact]
    public async Task Should_MapEnteredCurrentStateOnUtc_FromTheLastMove()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        (Project _, WorkItem workItem) = WorkItemQueryData.GetProjectAndWorkItemWithStateHistory(admin);
        _userContext.UserId.Returns(admin.Id);
        _dbContext.Seed(c => c.WorkItems, workItem);
        _dbContext.Seed(c => c.Users, admin);
        _dbContext.Seed(c => c.FlowTransitions, workItem.Project.FlowTransitions);
        _dbContext.Seed(c => c.FlowStates, workItem.Project.FlowStates);

        // Act
        Result<WorkItemResponse> result =
            await _handler.Handle(new GetWorkItemByCodeQuery(workItem.Code), CancellationToken.None);

        // Assert
        result.Value.EnteredCurrentStateOnUtc.Should().Be(WorkItemQueryData.UtcNow.AddHours(1));
    }
```

- [x] **Verlo fallar.** Para que compile, añade los parámetros a los records (código final, abajo) y pasa valores fijos en los handlers: `FlowStateStage.InFlight` como `Stage` del board y `default` como fecha en los dos handlers.

```bash
dotnet build test/Aurora.Flowboard.Application.UnitTests/Aurora.Flowboard.Application.UnitTests.csproj
./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests.exe -class "Aurora.Flowboard.Application.UnitTests.Projects.GetProjectBoardHandlerTests"
./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests.exe -class "Aurora.Flowboard.Application.UnitTests.WorkItems.GetWorkItemByCodeHandlerTests"
```

  Fallo esperado: `Should_MarkFirstColumnAsBacklog_AndTheRestAsInFlight` (la primera columna sale `InFlight`) y los dos `Should_MapEnteredCurrentStateOnUtc_FromTheLastMove` (`found <0001-01-01 00:00:00>`). `Should_MarkSingleColumnAsInFlight_When_ProjectHasOneActiveState` ya pasa con el stub.

- [x] **Implementar.**

  `BoardColumnResponse.cs`, con `Stage` después de `Color`:

```csharp
namespace Aurora.Flowboard.Application.Projects.GetBoard;

public sealed record BoardColumnResponse(
    Guid FlowStateId,
    string FlowStateName,
    FlowStateCategory Category,
    int SortOrder,
    string Color,
    FlowStateStage Stage,
    IReadOnlyCollection<BoardWorkItemResponse> WorkItems,
    IReadOnlyCollection<WorkItemFlowTransitionResponse> AvailableTransitions);
```

  `BoardWorkItemResponse.cs`: añade `DateTime EnteredCurrentStateOnUtc,` justo después de `DateTime CreatedOnUtc,`.

  `GetProjectBoardHandler.cs`:
  1. Después de `List<Guid> stateIds = [.. orderedStates.Select(s => s.Id)];`:

```csharp
        // The first Active state is the backlog, unless it is the only one (FlowState.GetBacklogStateId).
        Guid? backlogStateId = FlowState.GetBacklogStateId([.. orderedStates.Select(s => (s.Id, s.SortOrder))]);
```

  2. En el `Select` anónimo de `workItemRows`, después de `w.CreatedOnUtc,`: `w.EnteredCurrentStateOnUtc,`.
  3. En `new BoardColumnResponse(...)`, después de `s.Color,`:

```csharp
                s.Id == backlogStateId ? FlowStateStage.Backlog : FlowStateStage.InFlight,
```

  4. En `new BoardWorkItemResponse(...)`, después de `w.CreatedOnUtc,`: `w.EnteredCurrentStateOnUtc,`.

  `WorkItemResponse.cs`: añade `DateTime EnteredCurrentStateOnUtc,` justo después de `DateTime? CompletedOnUtc,`. El detalle no lleva `stage` (supuesto 9).

  `GetWorkItemByCodeHandler.cs`: en `new WorkItemResponse(...)`, después de `w.CompletedOnUtc,`, añade `w.EnteredCurrentStateOnUtc,`.

- [x] **Verlo pasar.** Repite los comandos (todo en verde en las dos clases).
- [x] Commit: `Expose stage on board columns and time in state on board and detail` con los cinco ficheros de `src/`, los dos de tests y este plan.

### Tarea 6: query, filtros y validador de `GET work-items`

**Skill:** `create-feature`

**Ficheros:**
- Crear: `src/Aurora.Flowboard.Application/WorkItems/GetWorkItems/GetWorkItemsQuery.cs`, `WorkItemSearchFilters.cs`, `GetWorkItemsValidator.cs`, `WorkItemListItemResponse.cs`
- Modificar: `test/Aurora.Flowboard.Application.UnitTests/GlobalUsings.cs`
- Test: `test/Aurora.Flowboard.Application.UnitTests/WorkItems/GetWorkItemsValidatorTests.cs`

- [x] **Escribir el test que falla.** En `GlobalUsings.cs` de Application.UnitTests, en orden alfabético:

```csharp
global using Aurora.Flowboard.Application.WorkItems.GetWorkItems;
```

  Crea `test/Aurora.Flowboard.Application.UnitTests/WorkItems/GetWorkItemsValidatorTests.cs`:

```csharp
using Aurora.Flowboard.Application.Abstractions.Pagination;
using FluentValidation.Results;

namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class GetWorkItemsValidatorTests
{
    private readonly GetWorkItemsValidator _validator = new();

    [Fact]
    public void Should_Pass_When_NoFilterIsGiven()
    {
        // Act
        ValidationResult result = _validator.Validate(Search());

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Pass_When_EveryFilterHasAnAcceptedValue()
    {
        // Act
        ValidationResult result = _validator.Validate(
            Search(assignee: "me", createdBy: "me", stage: "InFlight", stalled: "true", priority: "High,Critical", sort: "stageThenTimeInState"));

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Pass_When_PriorityListHasSpacesAroundCommas()
    {
        // Act
        ValidationResult result = _validator.Validate(Search(priority: "High, Critical"));

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_AssigneeIsNotMe()
    {
        // Act
        ValidationResult result = _validator.Validate(Search(assignee: "someone-else"));

        // Assert
        AssertSingleError(result, nameof(GetWorkItemsQuery.Assignee));
    }

    [Fact]
    public void Should_Fail_When_CreatedByIsNotLowercaseMe()
    {
        // Act
        ValidationResult result = _validator.Validate(Search(createdBy: "Me"));

        // Assert
        AssertSingleError(result, nameof(GetWorkItemsQuery.CreatedBy));
    }

    [Fact]
    public void Should_Fail_When_StageIsNotAStage()
    {
        // Act
        ValidationResult result = _validator.Validate(Search(stage: "Done"));

        // Assert
        AssertSingleError(result, nameof(GetWorkItemsQuery.Stage));
    }

    [Fact]
    public void Should_Fail_When_StageHasTheWrongCase()
    {
        // Act
        ValidationResult result = _validator.Validate(Search(stage: "inflight"));

        // Assert
        AssertSingleError(result, nameof(GetWorkItemsQuery.Stage));
    }

    [Fact]
    public void Should_Fail_When_StalledIsNotTrue()
    {
        // Act
        ValidationResult result = _validator.Validate(Search(stalled: "false"));

        // Assert
        AssertSingleError(result, nameof(GetWorkItemsQuery.Stalled));
    }

    [Fact]
    public void Should_Fail_When_PriorityListHasAnUnknownValue()
    {
        // Act
        ValidationResult result = _validator.Validate(Search(priority: "High,Urgent"));

        // Assert
        AssertSingleError(result, nameof(GetWorkItemsQuery.Priority));
    }

    [Fact]
    public void Should_Fail_When_PriorityHasTheWrongCase()
    {
        // Act
        ValidationResult result = _validator.Validate(Search(priority: "high"));

        // Assert
        AssertSingleError(result, nameof(GetWorkItemsQuery.Priority));
    }

    [Fact]
    public void Should_Fail_When_PriorityIsANumber()
    {
        // Act
        ValidationResult result = _validator.Validate(Search(priority: "2"));

        // Assert
        AssertSingleError(result, nameof(GetWorkItemsQuery.Priority));
    }

    [Fact]
    public void Should_Fail_When_PriorityListHasAnEmptyEntry()
    {
        // Act
        ValidationResult result = _validator.Validate(Search(priority: "High,,Low"));

        // Assert
        AssertSingleError(result, nameof(GetWorkItemsQuery.Priority));
    }

    [Fact]
    public void Should_Fail_When_SortIsUnknown()
    {
        // Act
        ValidationResult result = _validator.Validate(Search(sort: "createdOn"));

        // Assert
        AssertSingleError(result, nameof(GetWorkItemsQuery.Sort));
    }

    [Fact]
    public void Should_Fail_When_PageIsZero()
    {
        // Act
        ValidationResult result = _validator.Validate(Search(page: 0));

        // Assert
        AssertSingleError(result, nameof(GetWorkItemsQuery.Page));
    }

    [Fact]
    public void Should_Fail_When_PageSizeExceedsMaxPageSize()
    {
        // Act
        ValidationResult result = _validator.Validate(Search(pageSize: PaginationDefaults.MaxPageSize + 1));

        // Assert
        AssertSingleError(result, nameof(GetWorkItemsQuery.PageSize));
    }

    private static GetWorkItemsQuery Search(
        string? assignee = null,
        string? createdBy = null,
        string? stage = null,
        string? stalled = null,
        string? priority = null,
        string? sort = null,
        int page = PaginationDefaults.DefaultPage,
        int pageSize = PaginationDefaults.DefaultPageSize) =>
        new(assignee, createdBy, stage, stalled, priority, sort, page, pageSize);

    private static void AssertSingleError(ValidationResult result, string propertyName)
    {
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be(propertyName);
    }
}
```

- [x] **Verlo fallar.** Crea `GetWorkItemsQuery.cs`, `WorkItemListItemResponse.cs` y `WorkItemSearchFilters.cs` con su código final (abajo), y un validador vacío:

```csharp
namespace Aurora.Flowboard.Application.WorkItems.GetWorkItems;

internal sealed class GetWorkItemsValidator : AbstractValidator<GetWorkItemsQuery>;
```

```bash
dotnet build test/Aurora.Flowboard.Application.UnitTests/Aurora.Flowboard.Application.UnitTests.csproj
./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests.exe -class "Aurora.Flowboard.Application.UnitTests.WorkItems.GetWorkItemsValidatorTests"
```

  Fallo esperado: los 12 `Should_Fail_*` fallan con `Expected result.IsValid to be false, but found True`. Los 3 `Should_Pass_*` pasan.

- [x] **Implementar.**

  `GetWorkItemsQuery.cs`:

```csharp
namespace Aurora.Flowboard.Application.WorkItems.GetWorkItems;

// Cross-project search for the Home and My Issues: the open work items of my active projects.
// The filters arrive as raw strings so that every invalid value gets the same 400 from GetWorkItemsValidator.
public sealed record GetWorkItemsQuery(
    string? Assignee,
    string? CreatedBy,
    string? Stage,
    string? Stalled,
    string? Priority,
    string? Sort,
    int Page,
    int PageSize) : IQuery<PagedResponse<WorkItemListItemResponse>>, IPagedQuery;
```

  `WorkItemListItemResponse.cs`:

```csharp
namespace Aurora.Flowboard.Application.WorkItems.GetWorkItems;

public sealed record WorkItemListItemResponse(
    Guid WorkItemId,
    string Code,
    string Title,
    WorkItemType Type,
    Priority Priority,
    Guid ProjectId,
    string ProjectName,
    string ProjectColor,
    Guid FlowStateId,
    string FlowStateName,
    FlowStateStage Stage,
    DateTime EnteredCurrentStateOnUtc,
    Guid? AssigneeId,
    string? AssigneeInitials,
    string? AssigneeFullName,
    string? MilestoneName,
    string? MilestoneColor);
```

  `WorkItemSearchFilters.cs`:

```csharp
namespace Aurora.Flowboard.Application.WorkItems.GetWorkItems;

// Values accepted by the GET work-items query string, case-sensitive: they are the values the API
// serializes. GetWorkItemsValidator rejects anything else, so the handler parses without checking again.
internal static class WorkItemSearchFilters
{
    public const string MeValue = "me";
    public const string TrueValue = "true";
    public const string DefaultSort = "stageThenTimeInState";

    private const char PrioritySeparator = ',';

    public static bool IsStage(string value) =>
        Enum.GetNames<FlowStateStage>().Contains(value, StringComparer.Ordinal);

    public static FlowStateStage ParseStage(string value) =>
        Enum.Parse<FlowStateStage>(value);

    // Matched against the names instead of Enum.TryParse, which also accepts numbers ("2").
    public static bool IsPriorityList(string value) =>
        SplitPriorities(value).All(name => Enum.GetNames<Priority>().Contains(name, StringComparer.Ordinal));

    public static List<Priority> ParsePriorities(string value) =>
        [.. SplitPriorities(value).Select(name => Enum.Parse<Priority>(name)).Distinct()];

    private static string[] SplitPriorities(string value) =>
        value.Split(PrioritySeparator, StringSplitOptions.TrimEntries);
}
```

  `GetWorkItemsValidator.cs`:

```csharp
namespace Aurora.Flowboard.Application.WorkItems.GetWorkItems;

internal sealed class GetWorkItemsValidator : AbstractValidator<GetWorkItemsQuery>
{
    public GetWorkItemsValidator()
    {
        RuleFor(x => x.Assignee)
            .Must(value => value == WorkItemSearchFilters.MeValue)
            .WithMessage($"Assignee only accepts '{WorkItemSearchFilters.MeValue}'")
            .When(x => x.Assignee is not null);

        RuleFor(x => x.CreatedBy)
            .Must(value => value == WorkItemSearchFilters.MeValue)
            .WithMessage($"CreatedBy only accepts '{WorkItemSearchFilters.MeValue}'")
            .When(x => x.CreatedBy is not null);

        RuleFor(x => x.Stage)
            .Must(value => WorkItemSearchFilters.IsStage(value!))
            .WithMessage($"Stage must be {nameof(FlowStateStage.Backlog)} or {nameof(FlowStateStage.InFlight)}")
            .When(x => x.Stage is not null);

        RuleFor(x => x.Stalled)
            .Must(value => value == WorkItemSearchFilters.TrueValue)
            .WithMessage($"Stalled only accepts '{WorkItemSearchFilters.TrueValue}'")
            .When(x => x.Stalled is not null);

        RuleFor(x => x.Priority)
            .Must(value => WorkItemSearchFilters.IsPriorityList(value!))
            .WithMessage("Priority must be a comma-separated list of Low, Medium, High or Critical")
            .When(x => x.Priority is not null);

        RuleFor(x => x.Sort)
            .Must(value => value == WorkItemSearchFilters.DefaultSort)
            .WithMessage($"Sort only accepts '{WorkItemSearchFilters.DefaultSort}'")
            .When(x => x.Sort is not null);

        Include(new PagedQueryValidator());
    }
}
```

- [x] **Verlo pasar.** Repite los comandos (15 en verde).
- [x] `dotnet build "Aurora Flowboard.slnx"` y `./test/Aurora.Flowboard.ArchitectureTests/bin/Debug/net10.0/Aurora.Flowboard.ArchitectureTests.exe`. La query aún no tiene handler; ningún test de arquitectura lo exige.
- [x] Commit: `Validate the filters of the cross-project work item search` con los cuatro ficheros del slice, `GlobalUsings.cs`, `GetWorkItemsValidatorTests.cs` y este plan.

  > Desviación: IDE0200 (warning como error) en `WorkItemSearchFilters.ParsePriorities`; la lambda `name => Enum.Parse<Priority>(name)` pasa a ser el grupo de métodos `Enum.Parse<Priority>`. Mismo comportamiento.

### Tarea 7: handler de `GET work-items`

**Skill:** `create-feature`

**Ficheros:**
- Crear: `src/Aurora.Flowboard.Application/Projects/Shared/FlowStateStageResolver.cs`
- Crear: `src/Aurora.Flowboard.Application/WorkItems/GetWorkItems/GetWorkItemsHandler.cs`
- Crear: `test/Aurora.Flowboard.Application.UnitTests/WorkItems/GetWorkItemsQueryData.cs`
- Test: `test/Aurora.Flowboard.Application.UnitTests/WorkItems/GetWorkItemsHandlerTests.cs`

- [x] **Escribir las fixtures.** Crea `test/Aurora.Flowboard.Application.UnitTests/WorkItems/GetWorkItemsQueryData.cs`:

```csharp
using System.Reflection;

namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

// Fixtures for the cross-project reads, GET work-items and GET dashboard. Times are relative to Now,
// so the time an item has spent in its state reads straight from the call.
internal static class GetWorkItemsQueryData
{
    public const string BacklogState = "Backlog";
    public const string InProgressState = "In Progress";
    public const string ReviewState = "Review";
    public const string DoneState = "Done";
    public const string CancelledState = "Cancelled";

    public static readonly DateTime Now = new(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);
    public static readonly Color MilestoneColor = Color.Create("#F97316").Value;
    private static readonly DateTime CreatedOnUtc = Now.AddDays(-90);
    private static readonly Color ProjectColor = Color.Create("#3B82F6").Value;

    public static User GetUser(string firstName, string lastName)
    {
        Email email = Email.Create($"{firstName.ToLowerInvariant()}.{lastName.ToLowerInvariant()}@search.test").Value;
        User user = User.Create(firstName, lastName, email, Password.Create("hashed_password_123").Value, CreatedOnUtc).Value;
        user.AssignRole(Role.Member);
        return user;
    }

    // Backlog, In Progress and Review are Active, and Backlog is the backlog stage.
    // Client kind, so the project can reach Completed.
    public static Project GetThreeStageProject(string name, string code, User admin, params User[] developers) =>
        CreateProject(name, code, ProjectKind.Client, GetThreeStageFlow(), admin, developers);

    // The same flow with Product kind, so the project can reach Maintenance.
    public static Project GetContinuousThreeStageProject(string name, string code, User admin, params User[] developers) =>
        CreateProject(name, code, ProjectKind.Product, GetThreeStageFlow(), admin, developers);

    // Backlog is the only Active state: it is in flight, and the project has no backlog stage.
    public static Project GetSingleStateProject(string name, string code, User admin, params User[] developers) =>
        CreateProject(name, code, ProjectKind.Client, FlowStateDefinitionData.GetMinimalFlow(), admin, developers);

    public static void ChangeStatus(Project project, ProjectStatus status, User admin) =>
        EnsureSuccess(project.ChangeStatus(status, admin, Now), $"{project.Name} to {status}");

    // Created in the project's first Active state, timeInState before Now.
    public static WorkItem CreateInBacklog(
        Project project,
        User creator,
        TimeSpan timeInState,
        Priority priority = Priority.Medium,
        User? assignee = null,
        Milestone? milestone = null) =>
        WorkItem.Create("Search item", null, WorkItemType.Story, priority, project, creator, null, null, Now - timeInState, assignee, milestone).Value;

    // Created long ago, then moved timeInState before Now to stateName along the flow's transitions.
    public static WorkItem CreateInState(
        Project project,
        string stateName,
        User creator,
        TimeSpan timeInState,
        Priority priority = Priority.Medium,
        User? assignee = null,
        Milestone? milestone = null)
    {
        WorkItem workItem = WorkItem.Create("Search item", null, WorkItemType.Story, priority, project, creator, null, null, CreatedOnUtc, assignee, milestone).Value;
        MoveTo(workItem, project, stateName, creator, Now - timeInState);
        return workItem;
    }

    public static Milestone CreateActiveMilestone(Project project, User admin, string name, DateOnly? targetEndDate, DateTime? createdOnUtc = null)
    {
        Milestone milestone = Milestone.Create(name, null, MilestoneColor, null, targetEndDate, project, admin, createdOnUtc ?? CreatedOnUtc).Value;
        EnsureSuccess(milestone.ChangeStatus(MilestoneStatus.Active, admin, 0, Now), $"{name} to Active");
        return milestone;
    }

    // Move cannot reopen an item (no transition leaves a closing state), but a row reopened by hand
    // keeps its CompletedOnUtc, which closedLast7Days must ignore.
    public static void SetCompletedOnUtc(WorkItem workItem, DateTime completedOnUtc) =>
        (typeof(WorkItem).GetField("<CompletedOnUtc>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("WorkItem has no CompletedOnUtc backing field."))
            .SetValue(workItem, completedOnUtc);

    // GetMySummaryHandler counts through Project.WorkItems, which EF fills and WorkItem.Create does not.
    public static void AttachWorkItems(Project project, params WorkItem[] workItems)
    {
        var list = (List<WorkItem>?)typeof(Project)
            .GetField("_workItems", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(project)
            ?? throw new InvalidOperationException("Project has no _workItems field.");
        list.AddRange(workItems);
    }

    private static void MoveTo(WorkItem workItem, Project project, string stateName, User changedBy, DateTime movedOnUtc)
    {
        FlowState target = project.FlowStates.Single(s => s.Name == stateName);

        if (target.Id == workItem.FlowStateId)
        {
            throw new InvalidOperationException($"The item is already in {stateName}: use {nameof(CreateInBacklog)}.");
        }

        // Active states only link to their neighbours, and Done only to the last Active state.
        int walkTo = target.Category switch
        {
            FlowStateCategory.Active => target.SortOrder,
            FlowStateCategory.Completed => project.FlowStates.Where(s => s.Category == FlowStateCategory.Active).Max(s => s.SortOrder),
            _ => workItem.FlowState.SortOrder
        };

        List<FlowState> steps = [.. project.FlowStates
            .Where(s => s.Category == FlowStateCategory.Active && s.SortOrder > workItem.FlowState.SortOrder && s.SortOrder <= walkTo)
            .OrderBy(s => s.SortOrder)];

        foreach (FlowState step in steps)
        {
            EnsureSuccess(workItem.Move(step, changedBy, null, movedOnUtc), $"move to {step.Name}");
        }

        if (target.Category != FlowStateCategory.Active)
        {
            EnsureSuccess(workItem.Move(target, changedBy, null, movedOnUtc), $"move to {target.Name}");
        }
    }

    private static Project CreateProject(string name, string code, ProjectKind kind, FlowStateDefinition[] flow, User admin, User[] developers)
    {
        Project project = Project.Create(name, null, ProjectCode.Create(code).Value, kind, ProjectColor, flow, admin, CreatedOnUtc).Value;

        foreach (User developer in developers)
        {
            EnsureSuccess(project.AddMember(developer, ProjectRole.Developer, admin, CreatedOnUtc), $"add {developer.FullName}");
        }

        return project;
    }

    private static FlowStateDefinition[] GetThreeStageFlow() =>
        FlowStateDefinitionData.GetFlow(
            (BacklogState, FlowStateCategory.Active),
            (InProgressState, FlowStateCategory.Active),
            (ReviewState, FlowStateCategory.Active),
            (DoneState, FlowStateCategory.Completed),
            (CancelledState, FlowStateCategory.Cancelled));

    private static void EnsureSuccess(Result result, string step)
    {
        if (!result.IsSuccessful)
        {
            throw new InvalidOperationException($"Fixture step '{step}' failed: {result.Error.Code}");
        }
    }
}
```

- [x] **Escribir el test que falla.** Crea `test/Aurora.Flowboard.Application.UnitTests/WorkItems/GetWorkItemsHandlerTests.cs`:

```csharp
using Aurora.Flowboard.Application.Abstractions.Pagination;
using static Aurora.Flowboard.Application.UnitTests.WorkItems.GetWorkItemsQueryData;

namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class GetWorkItemsHandlerTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly User _caller = GetUser("Casey", "Caller");
    private readonly User _teammate = GetUser("Taylor", "Teammate");
    private readonly GetWorkItemsHandler _handler;

    public GetWorkItemsHandlerTests()
    {
        _userContext.UserId.Returns(_caller.Id);
        _dateTimeProvider.UtcNow.Returns(Now);
        _handler = new GetWorkItemsHandler(_dbContext, _userContext, _dateTimeProvider);
    }

    [Fact]
    public async Task Should_ReturnEmptyPage_When_UserHasNoActiveProjects()
    {
        // Arrange
        Project foreign = GetThreeStageProject("Foreign", "FRG", _teammate);
        Arrange([foreign], [CreateInState(foreign, InProgressState, _teammate, Days(1))]);

        // Act
        Result<PagedResponse<WorkItemListItemResponse>> result = await _handler.Handle(Search(), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
        _ = _dbContext.DidNotReceive().WorkItems;
    }

    [Fact]
    public async Task Should_ReturnOnlyOpenItems_OfMyActiveAndMaintenanceProjects()
    {
        // Arrange
        Project active = GetThreeStageProject("Active", "ACT", _caller);
        Project maintenance = GetContinuousThreeStageProject("Maintenance", "MNT", _caller);
        Project completed = GetThreeStageProject("Completed", "CMP", _caller);
        Project archived = GetThreeStageProject("Archived", "ARC", _caller);
        Project foreign = GetThreeStageProject("Foreign", "FRG", _teammate);
        WorkItem activeInFlight = CreateInState(active, InProgressState, _caller, Days(1));
        WorkItem activeBacklog = CreateInBacklog(active, _caller, Days(1));
        WorkItem activeDone = CreateInState(active, DoneState, _caller, Days(1));
        WorkItem activeCancelled = CreateInState(active, CancelledState, _caller, Days(1));
        WorkItem maintenanceInFlight = CreateInState(maintenance, InProgressState, _caller, Days(1));
        WorkItem completedInFlight = CreateInState(completed, InProgressState, _caller, Days(1));
        WorkItem archivedInFlight = CreateInState(archived, InProgressState, _caller, Days(1));
        WorkItem foreignInFlight = CreateInState(foreign, InProgressState, _teammate, Days(1));
        ChangeStatus(maintenance, ProjectStatus.Maintenance, _caller);
        ChangeStatus(completed, ProjectStatus.Completed, _caller);
        ChangeStatus(archived, ProjectStatus.Archived, _caller);
        Arrange(
            [active, maintenance, completed, archived, foreign],
            [activeInFlight, activeBacklog, activeDone, activeCancelled, maintenanceInFlight, completedInFlight, archivedInFlight, foreignInFlight]);

        // Act
        Result<PagedResponse<WorkItemListItemResponse>> result = await _handler.Handle(Search(), CancellationToken.None);

        // Assert
        result.Value.Items.Select(i => i.WorkItemId).Should().BeEquivalentTo([activeInFlight.Id, activeBacklog.Id, maintenanceInFlight.Id]);
    }

    [Fact]
    public async Task Should_ReturnOnlyItemsAssignedToMe_When_AssigneeIsMe()
    {
        // Arrange
        Project project = GetThreeStageProject("Work", "WRK", _caller, _teammate);
        WorkItem mine = CreateInState(project, InProgressState, _caller, Days(1), assignee: _caller);
        WorkItem theirs = CreateInState(project, InProgressState, _caller, Days(1), assignee: _teammate);
        WorkItem unassigned = CreateInState(project, InProgressState, _caller, Days(1));
        Arrange([project], [mine, theirs, unassigned]);

        // Act
        Result<PagedResponse<WorkItemListItemResponse>> result = await _handler.Handle(Search(assignee: "me"), CancellationToken.None);

        // Assert
        result.Value.Items.Select(i => i.WorkItemId).Should().Equal(mine.Id);
    }

    [Fact]
    public async Task Should_ReturnOpenItemsICreated_ForAnyAssignee_When_CreatedByIsMe()
    {
        // Arrange
        Project project = GetThreeStageProject("Work", "WRK", _caller, _teammate);
        WorkItem forTeammate = CreateInState(project, InProgressState, _caller, Days(2), assignee: _teammate);
        WorkItem unassigned = CreateInBacklog(project, _caller, Days(1));
        WorkItem byTeammate = CreateInState(project, InProgressState, _teammate, Days(1), assignee: _caller);
        WorkItem closed = CreateInState(project, DoneState, _caller, Days(1));
        Arrange([project], [forTeammate, unassigned, byTeammate, closed]);

        // Act
        Result<PagedResponse<WorkItemListItemResponse>> result = await _handler.Handle(Search(createdBy: "me"), CancellationToken.None);

        // Assert
        result.Value.Items.Select(i => i.WorkItemId).Should().BeEquivalentTo([forTeammate.Id, unassigned.Id]);
    }

    [Fact]
    public async Task Should_ReturnOnlyBacklogItems_When_StageIsBacklog()
    {
        // Arrange
        Project threeStage = GetThreeStageProject("Three", "THR", _caller);
        Project singleState = GetSingleStateProject("Single", "SGL", _caller);
        WorkItem backlog = CreateInBacklog(threeStage, _caller, Days(1));
        WorkItem inProgress = CreateInState(threeStage, InProgressState, _caller, Days(1));
        WorkItem single = CreateInBacklog(singleState, _caller, Days(1));
        Arrange([threeStage, singleState], [backlog, inProgress, single]);

        // Act
        Result<PagedResponse<WorkItemListItemResponse>> result = await _handler.Handle(Search(stage: "Backlog"), CancellationToken.None);

        // Assert
        result.Value.Items.Select(i => i.WorkItemId).Should().Equal(backlog.Id);
    }

    [Fact]
    public async Task Should_IncludeSingleStateProjects_When_StageIsInFlight()
    {
        // Arrange
        Project threeStage = GetThreeStageProject("Three", "THR", _caller);
        Project singleState = GetSingleStateProject("Single", "SGL", _caller);
        WorkItem backlog = CreateInBacklog(threeStage, _caller, Days(1));
        WorkItem inProgress = CreateInState(threeStage, InProgressState, _caller, Days(1));
        WorkItem single = CreateInBacklog(singleState, _caller, Days(1));
        Arrange([threeStage, singleState], [backlog, inProgress, single]);

        // Act
        Result<PagedResponse<WorkItemListItemResponse>> result = await _handler.Handle(Search(stage: "InFlight"), CancellationToken.None);

        // Assert
        result.Value.Items.Select(i => i.WorkItemId).Should().BeEquivalentTo([inProgress.Id, single.Id]);
        result.Value.Items.Should().OnlyContain(i => i.Stage == FlowStateStage.InFlight);
    }

    [Fact]
    public async Task Should_ReturnInFlightItemsWithSevenDaysOrMore_When_StalledIsTrue()
    {
        // Arrange
        Project project = GetThreeStageProject("Work", "WRK", _caller);
        Project singleState = GetSingleStateProject("Single", "SGL", _caller);
        WorkItem stalled = CreateInState(project, InProgressState, _caller, Days(7));
        WorkItem almostStalled = CreateInState(project, ReviewState, _caller, Days(7) - TimeSpan.FromMinutes(1));
        WorkItem oldBacklog = CreateInBacklog(project, _caller, Days(30));
        WorkItem singleStateStalled = CreateInBacklog(singleState, _caller, Days(8));
        Arrange([project, singleState], [stalled, almostStalled, oldBacklog, singleStateStalled]);

        // Act
        Result<PagedResponse<WorkItemListItemResponse>> result = await _handler.Handle(Search(stalled: "true"), CancellationToken.None);

        // Assert
        result.Value.Items.Select(i => i.WorkItemId).Should().BeEquivalentTo([stalled.Id, singleStateStalled.Id]);
    }

    [Fact]
    public async Task Should_ReturnEmptyPage_When_StageIsBacklogAndStalledIsTrue()
    {
        // Arrange
        Project project = GetThreeStageProject("Work", "WRK", _caller);
        WorkItem oldBacklog = CreateInBacklog(project, _caller, Days(30));
        WorkItem stalled = CreateInState(project, InProgressState, _caller, Days(9));
        Arrange([project], [oldBacklog, stalled]);

        // Act
        Result<PagedResponse<WorkItemListItemResponse>> result =
            await _handler.Handle(Search(stage: "Backlog", stalled: "true"), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_FilterByEveryListedPriority()
    {
        // Arrange
        Project project = GetThreeStageProject("Work", "WRK", _caller);
        WorkItem low = CreateInState(project, InProgressState, _caller, Days(1), Priority.Low);
        WorkItem high = CreateInState(project, InProgressState, _caller, Days(1), Priority.High);
        WorkItem critical = CreateInState(project, InProgressState, _caller, Days(1), Priority.Critical);
        Arrange([project], [low, high, critical]);

        // Act
        Result<PagedResponse<WorkItemListItemResponse>> result =
            await _handler.Handle(Search(priority: "High,Critical"), CancellationToken.None);

        // Assert
        result.Value.Items.Select(i => i.WorkItemId).Should().BeEquivalentTo([high.Id, critical.Id]);
    }

    [Fact]
    public async Task Should_OrderInFlightFirst_ThenByLongestTimeInState()
    {
        // Arrange
        Project project = GetThreeStageProject("Work", "WRK", _caller);
        WorkItem backlogOldest = CreateInBacklog(project, _caller, Days(40));
        WorkItem inFlightNewer = CreateInState(project, InProgressState, _caller, Days(2));
        WorkItem inFlightOlder = CreateInState(project, ReviewState, _caller, Days(9));
        WorkItem backlogNewer = CreateInBacklog(project, _caller, Days(1));
        Arrange([project], [backlogOldest, inFlightNewer, inFlightOlder, backlogNewer]);

        // Act
        Result<PagedResponse<WorkItemListItemResponse>> result = await _handler.Handle(Search(), CancellationToken.None);

        // Assert
        result.Value.Items.Select(i => i.WorkItemId).Should().Equal(inFlightOlder.Id, inFlightNewer.Id, backlogOldest.Id, backlogNewer.Id);
    }

    [Fact]
    public async Task Should_ReturnDifferentItems_OnConsecutivePages()
    {
        // Arrange
        Project project = GetThreeStageProject("Work", "WRK", _caller);
        WorkItem oldest = CreateInState(project, InProgressState, _caller, Days(3));
        WorkItem middle = CreateInState(project, InProgressState, _caller, Days(2));
        WorkItem newest = CreateInState(project, InProgressState, _caller, Days(1));
        Arrange([project], [newest, oldest, middle]);

        // Act
        Result<PagedResponse<WorkItemListItemResponse>> firstPage = await _handler.Handle(Search(page: 1, pageSize: 2), CancellationToken.None);
        Result<PagedResponse<WorkItemListItemResponse>> secondPage = await _handler.Handle(Search(page: 2, pageSize: 2), CancellationToken.None);

        // Assert
        firstPage.Value.Items.Select(i => i.WorkItemId).Should().Equal(oldest.Id, middle.Id);
        secondPage.Value.Items.Select(i => i.WorkItemId).Should().Equal(newest.Id);
        secondPage.Value.TotalCount.Should().Be(3);
        secondPage.Value.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task Should_MapEveryListItemField()
    {
        // Arrange
        Project project = GetThreeStageProject("Aurora Web", "AWB", _caller, _teammate);
        Milestone milestone = CreateActiveMilestone(project, _caller, "Sprint 1", null);
        WorkItem workItem = CreateInState(project, InProgressState, _caller, Days(3), Priority.High, _teammate, milestone);
        Arrange([project], [workItem]);

        // Act
        Result<PagedResponse<WorkItemListItemResponse>> result = await _handler.Handle(Search(), CancellationToken.None);

        // Assert
        result.Value.Items.Should().ContainSingle().Which.Should().Be(new WorkItemListItemResponse(
            workItem.Id,
            workItem.Code,
            workItem.Title,
            workItem.Type,
            Priority.High,
            project.Id,
            "Aurora Web",
            project.Color.Value,
            workItem.FlowStateId,
            InProgressState,
            FlowStateStage.InFlight,
            Now - Days(3),
            _teammate.Id,
            "TT",
            "Taylor Teammate",
            "Sprint 1",
            MilestoneColor.Value));
    }

    [Fact]
    public async Task Should_ReturnNullAssigneeAndMilestone_When_ItemHasNone()
    {
        // Arrange
        Project project = GetThreeStageProject("Work", "WRK", _caller);
        WorkItem workItem = CreateInBacklog(project, _caller, Days(1));
        Arrange([project], [workItem]);

        // Act
        Result<PagedResponse<WorkItemListItemResponse>> result = await _handler.Handle(Search(), CancellationToken.None);

        // Assert
        WorkItemListItemResponse item = result.Value.Items.Single();
        item.Stage.Should().Be(FlowStateStage.Backlog);
        item.AssigneeId.Should().BeNull();
        item.AssigneeInitials.Should().BeNull();
        item.AssigneeFullName.Should().BeNull();
        item.MilestoneName.Should().BeNull();
        item.MilestoneColor.Should().BeNull();
    }

    private static TimeSpan Days(int days) => TimeSpan.FromDays(days);

    private static GetWorkItemsQuery Search(
        string? assignee = null,
        string? createdBy = null,
        string? stage = null,
        string? stalled = null,
        string? priority = null,
        int page = PaginationDefaults.DefaultPage,
        int pageSize = PaginationDefaults.DefaultPageSize) =>
        new(assignee, createdBy, stage, stalled, priority, null, page, pageSize);

    private void Arrange(IReadOnlyCollection<Project> projects, IReadOnlyCollection<WorkItem> workItems)
    {
        _dbContext.Seed(c => c.Projects, projects);
        _dbContext.Seed(c => c.WorkItems, workItems);
        _dbContext.Seed(c => c.Users, _caller, _teammate);
    }
}
```

- [x] **Verlo fallar.** Para que compile, crea `FlowStateStageResolver.cs` con su código final y un handler stub:

```csharp
namespace Aurora.Flowboard.Application.WorkItems.GetWorkItems;

internal sealed class GetWorkItemsHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider) : IQueryHandler<GetWorkItemsQuery, PagedResponse<WorkItemListItemResponse>>
{
    public Task<Result<PagedResponse<WorkItemListItemResponse>>> Handle(
        GetWorkItemsQuery query,
        CancellationToken cancellationToken)
    {
        // Stub: reads the dependencies only so that CS9113 (unread parameter) does not stop the build.
        _ = (dbContext, userContext, dateTimeProvider);

        return Task.FromResult<Result<PagedResponse<WorkItemListItemResponse>>>(
            new PagedResponse<WorkItemListItemResponse>([], query.Page, query.PageSize, 0));
    }
}
```

```bash
dotnet build test/Aurora.Flowboard.Application.UnitTests/Aurora.Flowboard.Application.UnitTests.csproj
./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests.exe -class "Aurora.Flowboard.Application.UnitTests.WorkItems.GetWorkItemsHandlerTests"
```

  Fallo esperado: fallan todos salvo `Should_ReturnEmptyPage_When_StageIsBacklogAndStalledIsTrue` y, quizá, `Should_ReturnEmptyPage_When_UserHasNoActiveProjects` (el stub no lee `WorkItems`). Si algún fallo es una excepción de las fixtures (`Fixture step '...' failed`), corrige la fixture antes de seguir: el fallo tiene que ser de assertion.

- [x] **Implementar.** Crea `src/Aurora.Flowboard.Application/Projects/Shared/FlowStateStageResolver.cs`:

```csharp
namespace Aurora.Flowboard.Application.Projects.Shared;

internal static class FlowStateStageResolver
{
    // The backlog state of every project in the list, from its Active states (FlowState.GetBacklogStateId).
    public static HashSet<Guid> GetBacklogStateIds(IEnumerable<(Guid Id, Guid ProjectId, int SortOrder)> activeStates) =>
        [.. activeStates
            .GroupBy(s => s.ProjectId)
            .Select(project => FlowState.GetBacklogStateId([.. project.Select(s => (s.Id, s.SortOrder))]))
            .OfType<Guid>()];
}
```

  Sustituye el stub de `GetWorkItemsHandler.cs` por:

```csharp
namespace Aurora.Flowboard.Application.WorkItems.GetWorkItems;

internal sealed class GetWorkItemsHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider) : IQueryHandler<GetWorkItemsQuery, PagedResponse<WorkItemListItemResponse>>
{
    public async Task<Result<PagedResponse<WorkItemListItemResponse>>> Handle(
        GetWorkItemsQuery query,
        CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        // The Active states of my active projects fix the scope on their own: open items only,
        // and never from a Completed, Archived or foreign project.
        List<ActiveStateRow> activeStates = await dbContext
            .Projects
            .AsNoTracking()
            .WhereMyActiveProjects(userId)
            .SelectMany(p => p.FlowStates)
            .Where(s => s.Category == FlowStateCategory.Active)
            .Select(s => new ActiveStateRow(s.Id, s.ProjectId, s.SortOrder))
            .ToListAsync(cancellationToken);

        if (activeStates.Count == 0)
        {
            return new PagedResponse<WorkItemListItemResponse>([], query.Page, query.PageSize, 0);
        }

        HashSet<Guid> backlogStateIds = FlowStateStageResolver.GetBacklogStateIds(
            activeStates.Select(s => (s.Id, s.ProjectId, s.SortOrder)));
        List<Guid> openStateIds = [.. activeStates.Select(s => s.Id)];
        List<Guid> backlogIds = [.. backlogStateIds];
        List<Guid> inFlightStateIds = [.. openStateIds.Where(id => !backlogStateIds.Contains(id))];

        IQueryable<WorkItem> workItems = ApplyFilters(
            dbContext.WorkItems.AsNoTracking().Where(w => openStateIds.Contains(w.FlowStateId)),
            query,
            userId,
            backlogIds,
            inFlightStateIds);

        // In flight first (false sorts before true), then the longest time in state, then Id for stable pages.
        var rows = await workItems
            .OrderBy(w => backlogIds.Contains(w.FlowStateId))
            .ThenBy(w => w.EnteredCurrentStateOnUtc)
            .ThenBy(w => w.Id)
            .ToPagedResponseAsync(
                query,
                w => new
                {
                    w.Id,
                    w.Code,
                    w.Title,
                    w.Type,
                    w.Priority,
                    w.ProjectId,
                    ProjectName = w.Project.Name,
                    ProjectColor = w.Project.Color.Value,
                    w.FlowStateId,
                    FlowStateName = w.FlowState.Name,
                    w.EnteredCurrentStateOnUtc,
                    w.AssigneeId,
                    AssigneeFirstName = dbContext.Users
                        .Where(u => u.Id == w.AssigneeId)
                        .Select(u => u.FirstName)
                        .FirstOrDefault(),
                    AssigneeLastName = dbContext.Users
                        .Where(u => u.Id == w.AssigneeId)
                        .Select(u => u.LastName)
                        .FirstOrDefault(),
                    MilestoneName = w.Milestone != null ? w.Milestone.Name : null,
                    MilestoneColor = w.Milestone != null ? w.Milestone.Color.Value : null
                },
                cancellationToken);

        return rows.Map(w => new WorkItemListItemResponse(
            w.Id,
            w.Code,
            w.Title,
            w.Type,
            w.Priority,
            w.ProjectId,
            w.ProjectName,
            w.ProjectColor,
            w.FlowStateId,
            w.FlowStateName,
            backlogStateIds.Contains(w.FlowStateId) ? FlowStateStage.Backlog : FlowStateStage.InFlight,
            w.EnteredCurrentStateOnUtc,
            w.AssigneeId,
            w.AssigneeFirstName is null || w.AssigneeLastName is null
                ? null
                : User.FormatInitials(w.AssigneeFirstName, w.AssigneeLastName),
            w.AssigneeFirstName is null || w.AssigneeLastName is null
                ? null
                : User.FormatFullName(w.AssigneeFirstName, w.AssigneeLastName),
            w.MilestoneName,
            w.MilestoneColor));
    }

    // Filters combine with AND. GetWorkItemsValidator only lets "me" and "true" through, so a present
    // value means the filter is on. Sort is validated but has a single value, applied by Handle.
    private IQueryable<WorkItem> ApplyFilters(
        IQueryable<WorkItem> source,
        GetWorkItemsQuery query,
        Guid userId,
        List<Guid> backlogIds,
        List<Guid> inFlightStateIds)
    {
        IQueryable<WorkItem> filtered = source;

        if (query.Assignee is not null)
        {
            filtered = filtered.Where(w => w.AssigneeId == userId);
        }

        if (query.CreatedBy is not null)
        {
            filtered = filtered.Where(w => w.CreatedById == userId);
        }

        if (query.Stage is not null)
        {
            List<Guid> stageStateIds = WorkItemSearchFilters.ParseStage(query.Stage) == FlowStateStage.Backlog
                ? backlogIds
                : inFlightStateIds;
            filtered = filtered.Where(w => stageStateIds.Contains(w.FlowStateId));
        }

        if (query.Stalled is not null)
        {
            // Whole 24-hour blocks: entered at or before now - 7 days is 7 days or more in the state.
            DateTime stalledSince = dateTimeProvider.UtcNow.AddDays(-WorkItem.StalledThresholdDays);
            filtered = filtered.Where(w => inFlightStateIds.Contains(w.FlowStateId) && w.EnteredCurrentStateOnUtc <= stalledSince);
        }

        if (query.Priority is not null)
        {
            List<Priority> priorities = WorkItemSearchFilters.ParsePriorities(query.Priority);
            filtered = filtered.Where(w => priorities.Contains(w.Priority));
        }

        return filtered;
    }

    private sealed record ActiveStateRow(Guid Id, Guid ProjectId, int SortOrder);
}
```

- [x] **Verlo pasar.** Repite los comandos (13 en verde). Ejecuta también la suite de Application completa sin filtro.
- [x] `dotnet build "Aurora Flowboard.slnx"` y `./test/Aurora.Flowboard.ArchitectureTests/bin/Debug/net10.0/Aurora.Flowboard.ArchitectureTests.exe`.
- [x] Commit: `Search open work items across my active projects` con `FlowStateStageResolver.cs`, `GetWorkItemsHandler.cs`, `GetWorkItemsQueryData.cs`, `GetWorkItemsHandlerTests.cs` y este plan.

### Tarea 8: contrato del dashboard y `DashboardCalculator`

**Skill:** `create-feature`

**Ficheros:**
- Crear: `src/Aurora.Flowboard.Application/Dashboard/GetDashboard/DashboardResponse.cs`, `DashboardSnapshot.cs`, `DashboardCalculator.cs`
- Modificar: `test/Aurora.Flowboard.Application.UnitTests/GlobalUsings.cs`
- Crear: `test/Aurora.Flowboard.Application.UnitTests/Dashboard/DashboardQueryData.cs`
- Test: `test/Aurora.Flowboard.Application.UnitTests/Dashboard/DashboardCalculatorTests.cs`

Los tramos de antigüedad se calculan en SQL (tarea 9), así que sus límites exactos se prueban en `GetDashboardHandlerTests`, que ejecuta esa misma expresión. Aquí se prueban las reglas que no son filtros de SQL.

- [x] **Crear el contrato.** Es el código final; los tests lo necesitan para compilar. `DashboardResponse.cs`:

```csharp
namespace Aurora.Flowboard.Application.Dashboard.GetDashboard;

public sealed record DashboardResponse(
    DashboardMeResponse Me,
    DashboardTeamResponse Team,
    DashboardAgingResponse Aging,
    IReadOnlyCollection<DashboardMilestoneResponse> Milestones,
    IReadOnlyCollection<DashboardProjectResponse> Portfolio);

public sealed record DashboardMeResponse(
    int Open,
    int Stalled,
    int InFlight,
    int CriticalHigh);

public sealed record DashboardTeamResponse(
    int Stalled,
    int Unassigned,
    int ClosedLast7Days);

public sealed record DashboardAgingResponse(
    IReadOnlyCollection<DashboardAgingBucketResponse> Buckets,
    DashboardOldestInFlightResponse? OldestInFlight);

public sealed record DashboardAgingBucketResponse(
    int From,
    int? To,
    int Count);

public sealed record DashboardOldestInFlightResponse(
    string Code,
    string Title,
    Guid ProjectId,
    string ProjectName,
    DateTime EnteredCurrentStateOnUtc);

public sealed record DashboardMilestoneResponse(
    Guid Id,
    Guid ProjectId,
    string ProjectName,
    string Name,
    string Color,
    DateOnly? TargetEndDate,
    int LinkedTotal,
    int LinkedClosed,
    int LinkedStalled);

public sealed record DashboardProjectResponse(
    Guid ProjectId,
    string Name,
    string Color,
    ProjectKind Kind,
    ProjectStatus Status,
    int Open,
    int Stalled,
    IReadOnlyCollection<DashboardFlowStateCountResponse> Distribution,
    DashboardActiveMilestoneResponse? ActiveMilestone,
    IReadOnlyCollection<DashboardMemberResponse> Members);

public sealed record DashboardFlowStateCountResponse(
    Guid FlowStateId,
    string Name,
    string Color,
    int Count);

public sealed record DashboardActiveMilestoneResponse(
    Guid Id,
    string Name,
    string Color,
    DateOnly? TargetEndDate,
    int LinkedTotal,
    int LinkedClosed);

public sealed record DashboardMemberResponse(
    Guid UserId,
    string FullName,
    string Initials);
```

  `DashboardSnapshot.cs`:

```csharp
namespace Aurora.Flowboard.Application.Dashboard.GetDashboard;

// Everything GetDashboardHandler reads, in the shape DashboardCalculator needs.
internal sealed record DashboardSnapshot(
    IReadOnlyCollection<DashboardProjectRow> Projects,
    IReadOnlyCollection<DashboardStateRow> States,
    IReadOnlyCollection<DashboardOpenGroupRow> OpenGroups,
    IReadOnlyCollection<DashboardMilestoneRow> Milestones,
    IReadOnlyDictionary<Guid, int> ClosedByMilestone,
    int ClosedLast7Days,
    DashboardOldestInFlightResponse? OldestInFlight,
    IReadOnlyCollection<DashboardMemberRow> Members)
{
    public static DashboardSnapshot Empty { get; } = new(
        Array.Empty<DashboardProjectRow>(),
        Array.Empty<DashboardStateRow>(),
        Array.Empty<DashboardOpenGroupRow>(),
        Array.Empty<DashboardMilestoneRow>(),
        new Dictionary<Guid, int>(),
        0,
        null,
        Array.Empty<DashboardMemberRow>());
}

// One of my projects (WhereMyProjects).
internal sealed record DashboardProjectRow(Guid Id, string Name, string Color, ProjectKind Kind, ProjectStatus Status);

// An Active flow state of one of my projects.
internal sealed record DashboardStateRow(Guid Id, Guid ProjectId, string Name, string Color, int SortOrder);

// Open work items of my active projects that share every key; Count is how many there are.
internal sealed record DashboardOpenGroupRow(
    Guid FlowStateId,
    Guid? MilestoneId,
    bool IsMine,
    bool IsUnassigned,
    bool IsCriticalHigh,
    int AgeBand,
    int Count);

// An Active milestone of one of my active projects.
internal sealed record DashboardMilestoneRow(Guid Id, Guid ProjectId, string Name, string Color, DateOnly? TargetEndDate, DateTime CreatedOnUtc);

// How many Completed work items a milestone has.
internal sealed record DashboardMilestoneCountRow(Guid MilestoneId, int Count);

// A member of one of my projects; the names are null when the user row is missing.
internal sealed record DashboardMemberRow(Guid ProjectId, Guid UserId, string? FirstName, string? LastName);
```

- [x] **Escribir el test que falla.** En `GlobalUsings.cs` de Application.UnitTests, en orden alfabético:

```csharp
global using Aurora.Flowboard.Application.Dashboard.GetDashboard;
```

  Crea `test/Aurora.Flowboard.Application.UnitTests/Dashboard/DashboardQueryData.cs`:

```csharp
namespace Aurora.Flowboard.Application.UnitTests.Dashboard;

// Rows for DashboardCalculator, which is tested without the handler: each test states its own rows.
internal static class DashboardQueryData
{
    public const string ProjectColor = "#3B82F6";
    public const string StateColor = "#94A3B8";
    public const string MilestoneColor = "#F97316";
    public static readonly DateTime CreatedOnUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static DashboardProjectRow ProjectRow(string name, ProjectStatus status = ProjectStatus.Active) =>
        new(Guid.NewGuid(), name, ProjectColor, ProjectKind.Client, status);

    public static DashboardStateRow StateRow(DashboardProjectRow project, string name, int sortOrder) =>
        new(Guid.NewGuid(), project.Id, name, StateColor, sortOrder);

    public static DashboardOpenGroupRow GroupRow(
        DashboardStateRow state,
        int count,
        int ageBand = DashboardCalculator.FreshBand,
        bool isMine = false,
        bool isUnassigned = false,
        bool isCriticalHigh = false,
        Guid? milestoneId = null) =>
        new(state.Id, milestoneId, isMine, isUnassigned, isCriticalHigh, ageBand, count);

    public static DashboardMilestoneRow MilestoneRow(DashboardProjectRow project, string name, DateOnly? targetEndDate, DateTime? createdOnUtc = null) =>
        new(Guid.NewGuid(), project.Id, name, MilestoneColor, targetEndDate, createdOnUtc ?? CreatedOnUtc);

    public static DashboardMemberRow MemberRow(DashboardProjectRow project, string firstName, string lastName) =>
        new(project.Id, Guid.NewGuid(), firstName, lastName);

    public static DashboardSnapshot Snapshot(
        IReadOnlyCollection<DashboardProjectRow> projects,
        IReadOnlyCollection<DashboardStateRow> states,
        IReadOnlyCollection<DashboardOpenGroupRow>? openGroups = null,
        IReadOnlyCollection<DashboardMilestoneRow>? milestones = null,
        IReadOnlyDictionary<Guid, int>? closedByMilestone = null,
        int closedLast7Days = 0,
        IReadOnlyCollection<DashboardMemberRow>? members = null) =>
        new(
            projects,
            states,
            openGroups ?? Array.Empty<DashboardOpenGroupRow>(),
            milestones ?? Array.Empty<DashboardMilestoneRow>(),
            closedByMilestone ?? new Dictionary<Guid, int>(),
            closedLast7Days,
            null,
            members ?? Array.Empty<DashboardMemberRow>());
}
```

  Crea `test/Aurora.Flowboard.Application.UnitTests/Dashboard/DashboardCalculatorTests.cs`:

```csharp
using static Aurora.Flowboard.Application.UnitTests.Dashboard.DashboardQueryData;

namespace Aurora.Flowboard.Application.UnitTests.Dashboard;

public sealed class DashboardCalculatorTests
{
    [Fact]
    public void Should_ReturnZerosAndFourEmptyBuckets_When_SnapshotIsEmpty()
    {
        // Act
        DashboardResponse response = DashboardCalculator.Build(DashboardSnapshot.Empty);

        // Assert
        response.Me.Should().Be(new DashboardMeResponse(0, 0, 0, 0));
        response.Team.Should().Be(new DashboardTeamResponse(0, 0, 0));
        response.Aging.Buckets.Should().Equal(
            new DashboardAgingBucketResponse(0, 3, 0),
            new DashboardAgingBucketResponse(4, 6, 0),
            new DashboardAgingBucketResponse(7, 13, 0),
            new DashboardAgingBucketResponse(14, null, 0));
        response.Aging.OldestInFlight.Should().BeNull();
        response.Milestones.Should().BeEmpty();
        response.Portfolio.Should().BeEmpty();
    }

    [Fact]
    public void Should_NeverCountBacklogAsStalled()
    {
        // Arrange
        DashboardProjectRow project = ProjectRow("Aurora");
        DashboardStateRow backlog = StateRow(project, "Backlog", 1);
        DashboardStateRow inProgress = StateRow(project, "In Progress", 2);
        DashboardSnapshot snapshot = Snapshot(
            [project],
            [backlog, inProgress],
            [GroupRow(backlog, 5, DashboardCalculator.OldBand, isMine: true)]);

        // Act
        DashboardResponse response = DashboardCalculator.Build(snapshot);

        // Assert
        response.Me.Should().Be(new DashboardMeResponse(5, 0, 0, 0));
        response.Team.Stalled.Should().Be(0);
        response.Aging.Buckets.Sum(b => b.Count).Should().Be(0);
        response.Portfolio.Single().Stalled.Should().Be(0);
    }

    [Fact]
    public void Should_TreatASingleActiveState_AsInFlight()
    {
        // Arrange
        DashboardProjectRow project = ProjectRow("Aurora");
        DashboardStateRow only = StateRow(project, "Backlog", 1);
        DashboardSnapshot snapshot = Snapshot(
            [project],
            [only],
            [GroupRow(only, 2, DashboardCalculator.StalledBand, isMine: true)]);

        // Act
        DashboardResponse response = DashboardCalculator.Build(snapshot);

        // Assert
        response.Me.Should().Be(new DashboardMeResponse(2, 2, 2, 0));
        response.Team.Stalled.Should().Be(2);
    }

    [Fact]
    public void Should_MakeTheStalledBucketsAddUpToTeamStalled()
    {
        // Arrange
        DashboardProjectRow project = ProjectRow("Aurora");
        DashboardStateRow backlog = StateRow(project, "Backlog", 1);
        DashboardStateRow inProgress = StateRow(project, "In Progress", 2);
        DashboardStateRow review = StateRow(project, "Review", 3);
        DashboardSnapshot snapshot = Snapshot(
            [project],
            [backlog, inProgress, review],
            [
                GroupRow(inProgress, 1, DashboardCalculator.FreshBand),
                GroupRow(inProgress, 2, DashboardCalculator.AgingBand),
                GroupRow(inProgress, 3, DashboardCalculator.StalledBand),
                GroupRow(review, 4, DashboardCalculator.OldBand),
                GroupRow(backlog, 10, DashboardCalculator.OldBand)
            ]);

        // Act
        DashboardResponse response = DashboardCalculator.Build(snapshot);

        // Assert
        List<DashboardAgingBucketResponse> buckets = [.. response.Aging.Buckets];
        buckets.Select(b => b.Count).Should().Equal(1, 2, 3, 4);
        (buckets[2].Count + buckets[3].Count).Should().Be(response.Team.Stalled);
        response.Team.Stalled.Should().Be(7);
    }

    [Fact]
    public void Should_CountMyWork_AndTheTeamUnassignedWork()
    {
        // Arrange
        DashboardProjectRow project = ProjectRow("Aurora");
        DashboardStateRow backlog = StateRow(project, "Backlog", 1);
        DashboardStateRow inProgress = StateRow(project, "In Progress", 2);
        DashboardSnapshot snapshot = Snapshot(
            [project],
            [backlog, inProgress],
            [
                GroupRow(backlog, 1, isMine: true, isCriticalHigh: true),
                GroupRow(inProgress, 2, isMine: true),
                GroupRow(inProgress, 3, DashboardCalculator.StalledBand, isMine: true, isCriticalHigh: true),
                GroupRow(inProgress, 4),
                GroupRow(backlog, 5, isUnassigned: true),
                GroupRow(inProgress, 6, DashboardCalculator.OldBand, isUnassigned: true)
            ]);

        // Act
        DashboardResponse response = DashboardCalculator.Build(snapshot);

        // Assert
        response.Me.Should().Be(new DashboardMeResponse(Open: 6, Stalled: 3, InFlight: 5, CriticalHigh: 4));
        response.Team.Stalled.Should().Be(9);
        response.Team.Unassigned.Should().Be(11);
    }

    [Fact]
    public void Should_KeepACompletedProjectRow_WithZeros()
    {
        // Arrange
        DashboardProjectRow completed = ProjectRow("Closed", ProjectStatus.Completed);
        DashboardStateRow backlog = StateRow(completed, "Backlog", 1);
        DashboardStateRow inProgress = StateRow(completed, "In Progress", 2);
        DashboardMilestoneRow milestone = MilestoneRow(completed, "Release", new DateOnly(2026, 2, 1));
        DashboardSnapshot snapshot = Snapshot(
            [completed],
            [backlog, inProgress],
            [GroupRow(inProgress, 3, DashboardCalculator.StalledBand, isMine: true, milestoneId: milestone.Id)],
            [milestone],
            closedLast7Days: 2,
            members: [MemberRow(completed, "Ana", "Lopez")]);

        // Act
        DashboardResponse response = DashboardCalculator.Build(snapshot);

        // Assert
        response.Me.Should().Be(new DashboardMeResponse(0, 0, 0, 0));
        response.Team.Should().Be(new DashboardTeamResponse(0, 0, 2));
        response.Aging.Buckets.Sum(b => b.Count).Should().Be(0);
        response.Milestones.Should().BeEmpty();
        DashboardProjectResponse row = response.Portfolio.Single();
        row.Status.Should().Be(ProjectStatus.Completed);
        row.Open.Should().Be(0);
        row.Stalled.Should().Be(0);
        row.Distribution.Should().HaveCount(2).And.OnlyContain(d => d.Count == 0);
        row.ActiveMilestone.Should().BeNull();
        row.Members.Should().ContainSingle();
    }

    [Fact]
    public void Should_ListTheDistribution_InSortOrder_IncludingEmptyStates()
    {
        // Arrange
        DashboardProjectRow project = ProjectRow("Aurora");
        DashboardStateRow review = StateRow(project, "Review", 3);
        DashboardStateRow backlog = StateRow(project, "Backlog", 1);
        DashboardStateRow inProgress = StateRow(project, "In Progress", 2);
        DashboardSnapshot snapshot = Snapshot([project], [review, backlog, inProgress], [GroupRow(inProgress, 2)]);

        // Act
        DashboardResponse response = DashboardCalculator.Build(snapshot);

        // Assert
        response.Portfolio.Single().Distribution.Select(d => (d.Name, d.Count)).Should().Equal(
            ("Backlog", 0),
            ("In Progress", 2),
            ("Review", 0));
    }

    [Fact]
    public void Should_OrderThePortfolio_LikeTheSidebar()
    {
        // Arrange
        DashboardSnapshot snapshot = Snapshot(
            [
                ProjectRow("zeta"),
                ProjectRow("Alpha", ProjectStatus.Maintenance),
                ProjectRow("beta"),
                ProjectRow("Omega", ProjectStatus.Completed)
            ],
            []);

        // Act
        DashboardResponse response = DashboardCalculator.Build(snapshot);

        // Assert
        response.Portfolio.Select(p => p.Name).Should().Equal("beta", "zeta", "Alpha", "Omega");
    }

    [Fact]
    public void Should_OrderMilestones_DatedFirst_ThenUndatedByMostStalled()
    {
        // Arrange
        DashboardProjectRow project = ProjectRow("Aurora");
        DashboardStateRow backlog = StateRow(project, "Backlog", 1);
        DashboardStateRow inProgress = StateRow(project, "In Progress", 2);
        DashboardMilestoneRow late = MilestoneRow(project, "Late", new DateOnly(2026, 4, 1));
        DashboardMilestoneRow overdue = MilestoneRow(project, "Overdue", new DateOnly(2026, 1, 15));
        DashboardMilestoneRow quiet = MilestoneRow(project, "Quiet", null);
        DashboardMilestoneRow busy = MilestoneRow(project, "Busy", null);
        DashboardSnapshot snapshot = Snapshot(
            [project],
            [backlog, inProgress],
            [
                GroupRow(inProgress, 1, DashboardCalculator.StalledBand, milestoneId: quiet.Id),
                GroupRow(inProgress, 4, DashboardCalculator.StalledBand, milestoneId: busy.Id)
            ],
            [late, quiet, overdue, busy]);

        // Act
        DashboardResponse response = DashboardCalculator.Build(snapshot);

        // Assert
        response.Milestones.Select(m => m.Name).Should().Equal("Overdue", "Late", "Busy", "Quiet");
    }

    [Fact]
    public void Should_BreakMilestoneTies_ByName()
    {
        // Arrange
        DashboardProjectRow project = ProjectRow("Aurora");
        DateOnly targetEndDate = new(2026, 3, 1);
        DashboardSnapshot snapshot = Snapshot(
            [project],
            [],
            milestones: [MilestoneRow(project, "beta", targetEndDate), MilestoneRow(project, "Alpha", targetEndDate)]);

        // Act
        DashboardResponse response = DashboardCalculator.Build(snapshot);

        // Assert
        response.Milestones.Select(m => m.Name).Should().Equal("Alpha", "beta");
    }

    [Fact]
    public void Should_ComputeTheLinkedCounts_OfAMilestone()
    {
        // Arrange
        DashboardProjectRow project = ProjectRow("Aurora");
        DashboardStateRow backlog = StateRow(project, "Backlog", 1);
        DashboardStateRow inProgress = StateRow(project, "In Progress", 2);
        DashboardMilestoneRow milestone = MilestoneRow(project, "Sprint 1", new DateOnly(2026, 3, 15));
        DashboardSnapshot snapshot = Snapshot(
            [project],
            [backlog, inProgress],
            [
                GroupRow(backlog, 2, milestoneId: milestone.Id),
                GroupRow(inProgress, 3, DashboardCalculator.StalledBand, milestoneId: milestone.Id),
                GroupRow(inProgress, 7)
            ],
            [milestone],
            new Dictionary<Guid, int> { [milestone.Id] = 4 });

        // Act
        DashboardResponse response = DashboardCalculator.Build(snapshot);

        // Assert
        DashboardMilestoneResponse linked = response.Milestones.Single();
        linked.LinkedTotal.Should().Be(9);
        linked.LinkedClosed.Should().Be(4);
        linked.LinkedStalled.Should().Be(3);
        response.Portfolio.Single().ActiveMilestone.Should().Be(new DashboardActiveMilestoneResponse(
            milestone.Id, "Sprint 1", MilestoneColor, new DateOnly(2026, 3, 15), 9, 4));
    }

    [Fact]
    public void Should_PickTheActiveMilestone_WithTheEarliestTargetEndDate_OverdueIncluded()
    {
        // Arrange
        DashboardProjectRow project = ProjectRow("Aurora");
        DashboardMilestoneRow upcoming = MilestoneRow(project, "Upcoming", new DateOnly(2026, 6, 1));
        DashboardMilestoneRow overdue = MilestoneRow(project, "Overdue", new DateOnly(2026, 1, 10));
        DashboardMilestoneRow undated = MilestoneRow(project, "Undated", null, CreatedOnUtc.AddDays(30));
        DashboardSnapshot snapshot = Snapshot([project], [], milestones: [upcoming, undated, overdue]);

        // Act
        DashboardResponse response = DashboardCalculator.Build(snapshot);

        // Assert
        response.Portfolio.Single().ActiveMilestone!.Id.Should().Be(overdue.Id);
    }

    [Fact]
    public void Should_PickTheMostRecentlyCreatedMilestone_When_NoneHasATargetDate()
    {
        // Arrange
        DashboardProjectRow project = ProjectRow("Aurora");
        DashboardMilestoneRow older = MilestoneRow(project, "Older", null, CreatedOnUtc);
        DashboardMilestoneRow newer = MilestoneRow(project, "Newer", null, CreatedOnUtc.AddDays(5));
        DashboardSnapshot snapshot = Snapshot([project], [], milestones: [older, newer]);

        // Act
        DashboardResponse response = DashboardCalculator.Build(snapshot);

        // Assert
        response.Portfolio.Single().ActiveMilestone!.Id.Should().Be(newer.Id);
    }

    [Fact]
    public void Should_OrderMembers_ByFullName()
    {
        // Arrange
        DashboardProjectRow project = ProjectRow("Aurora");
        DashboardSnapshot snapshot = Snapshot(
            [project],
            [],
            members: [MemberRow(project, "Zoe", "Young"), MemberRow(project, "adam", "Smith")]);

        // Act
        DashboardResponse response = DashboardCalculator.Build(snapshot);

        // Assert
        response.Portfolio.Single().Members.Select(m => (m.FullName, m.Initials)).Should().Equal(
            ("adam Smith", "AS"),
            ("Zoe Young", "ZY"));
    }
}
```

- [x] **Verlo fallar.** Para que compile, crea `DashboardCalculator.cs` con las constantes del código final y un `Build` stub:

```csharp
    internal static DashboardResponse Build(DashboardSnapshot snapshot) =>
        new(
            new DashboardMeResponse(0, 0, 0, 0),
            new DashboardTeamResponse(0, 0, snapshot.ClosedLast7Days),
            new DashboardAgingResponse([], snapshot.OldestInFlight),
            [],
            []);
```

```bash
dotnet build test/Aurora.Flowboard.Application.UnitTests/Aurora.Flowboard.Application.UnitTests.csproj
./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests.exe -class "Aurora.Flowboard.Application.UnitTests.Dashboard.DashboardCalculatorTests"
```

  Fallo esperado: los 14 fallan (buckets vacíos, portafolio vacío o contadores en cero).

- [x] **Implementar.** `DashboardCalculator.cs`:

```csharp
namespace Aurora.Flowboard.Application.Dashboard.GetDashboard;

// Builds the dashboard from the rows GetDashboardHandler reads. A pure function: every rule of the
// spec's definitions that is not a SQL filter lives here, so it is tested without a database.
internal static class DashboardCalculator
{
    // Age band of an open item, by whole 24-hour blocks in its state. GetDashboardHandler computes it
    // in SQL. StalledBand and OldBand are stalled when the item is in flight.
    internal const int FreshBand = 0;
    internal const int AgingBand = 1;
    internal const int StalledBand = 2;
    internal const int OldBand = 3;

    internal const int AgingThresholdDays = 4;
    internal const int OldThresholdDays = 14;
    internal const int ClosedWindowDays = 7;

    // 0-3, 4-6, 7-13 and 14+ days. The 4 is only a bucket limit: it is not exposed to the frontend.
    private static readonly (int Band, int From, int? To)[] Buckets =
    [
        (FreshBand, 0, AgingThresholdDays - 1),
        (AgingBand, AgingThresholdDays, WorkItem.StalledThresholdDays - 1),
        (StalledBand, WorkItem.StalledThresholdDays, OldThresholdDays - 1),
        (OldBand, OldThresholdDays, null)
    ];

    internal static DashboardResponse Build(DashboardSnapshot snapshot)
    {
        HashSet<Guid> activeProjectIds = [.. snapshot.Projects
            .Where(p => MyProjectsQueryExtensions.AcceptsOpenWork(p.Status))
            .Select(p => p.Id)];
        HashSet<Guid> activeStateIds = [.. snapshot.States
            .Where(s => activeProjectIds.Contains(s.ProjectId))
            .Select(s => s.Id)];
        HashSet<Guid> backlogStateIds = FlowStateStageResolver.GetBacklogStateIds(
            snapshot.States.Select(s => (s.Id, s.ProjectId, s.SortOrder)));

        // The handler already reads only active projects; filtering again keeps a Completed project
        // at zero whatever rows it is given.
        List<DashboardOpenGroupRow> open = [.. snapshot.OpenGroups.Where(g => activeStateIds.Contains(g.FlowStateId))];
        List<DashboardOpenGroupRow> inFlight = [.. open.Where(g => !backlogStateIds.Contains(g.FlowStateId))];
        List<DashboardOpenGroupRow> stalled = [.. inFlight.Where(g => g.AgeBand >= StalledBand)];
        List<DashboardMilestoneRow> milestoneRows = [.. snapshot.Milestones.Where(m => activeProjectIds.Contains(m.ProjectId))];

        List<DashboardMilestoneResponse> milestones = BuildMilestones(snapshot, milestoneRows, open, stalled);

        return new DashboardResponse(
            new DashboardMeResponse(
                Count(open.Where(g => g.IsMine)),
                Count(stalled.Where(g => g.IsMine)),
                Count(inFlight.Where(g => g.IsMine)),
                Count(open.Where(g => g.IsMine && g.IsCriticalHigh))),
            new DashboardTeamResponse(
                Count(stalled),
                Count(open.Where(g => g.IsUnassigned)),
                snapshot.ClosedLast7Days),
            new DashboardAgingResponse(
                [.. Buckets.Select(b => new DashboardAgingBucketResponse(b.From, b.To, Count(inFlight.Where(g => g.AgeBand == b.Band))))],
                snapshot.OldestInFlight),
            milestones,
            BuildPortfolio(snapshot, milestoneRows, milestones.ToDictionary(m => m.Id), open, stalled));
    }

    private static List<DashboardMilestoneResponse> BuildMilestones(
        DashboardSnapshot snapshot,
        List<DashboardMilestoneRow> milestoneRows,
        List<DashboardOpenGroupRow> open,
        List<DashboardOpenGroupRow> stalled)
    {
        Dictionary<Guid, string> projectNames = snapshot.Projects.ToDictionary(p => p.Id, p => p.Name);

        return [.. milestoneRows
            .Select(m =>
            {
                int closed = snapshot.ClosedByMilestone.GetValueOrDefault(m.Id);

                return new DashboardMilestoneResponse(
                    m.Id,
                    m.ProjectId,
                    projectNames[m.ProjectId],
                    m.Name,
                    m.Color,
                    m.TargetEndDate,
                    Count(open.Where(g => g.MilestoneId == m.Id)) + closed,
                    closed,
                    Count(stalled.Where(g => g.MilestoneId == m.Id)));
            })
            // Dated first, the soonest (most overdue) first; then undated, the most stalled first.
            .OrderBy(m => m.TargetEndDate is null)
            .ThenBy(m => m.TargetEndDate)
            .ThenByDescending(m => m.TargetEndDate is null ? m.LinkedStalled : 0)
            .ThenBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(m => m.Id)];
    }

    private static List<DashboardProjectResponse> BuildPortfolio(
        DashboardSnapshot snapshot,
        List<DashboardMilestoneRow> milestoneRows,
        Dictionary<Guid, DashboardMilestoneResponse> milestonesById,
        List<DashboardOpenGroupRow> open,
        List<DashboardOpenGroupRow> stalled) =>
        [.. snapshot.Projects
            .OrderLikeSidebar(p => p.Status, p => p.Name)
            .Select(p =>
            {
                List<DashboardStateRow> states = [.. snapshot.States.Where(s => s.ProjectId == p.Id).OrderBy(s => s.SortOrder)];
                HashSet<Guid> stateIds = [.. states.Select(s => s.Id)];

                return new DashboardProjectResponse(
                    p.Id,
                    p.Name,
                    p.Color,
                    p.Kind,
                    p.Status,
                    Count(open.Where(g => stateIds.Contains(g.FlowStateId))),
                    Count(stalled.Where(g => stateIds.Contains(g.FlowStateId))),
                    [.. states.Select(s => new DashboardFlowStateCountResponse(s.Id, s.Name, s.Color, Count(open.Where(g => g.FlowStateId == s.Id))))],
                    PickActiveMilestone([.. milestoneRows.Where(m => m.ProjectId == p.Id)], milestonesById),
                    [.. snapshot.Members
                        .Where(m => m.ProjectId == p.Id)
                        .Select(ToMember)
                        .OrderBy(m => m.FullName, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(m => m.UserId)]);
            })];

    // The Active milestone with the earliest target end date, overdue ones included. When none has a
    // date, the most recently created one.
    private static DashboardActiveMilestoneResponse? PickActiveMilestone(
        List<DashboardMilestoneRow> projectMilestones,
        Dictionary<Guid, DashboardMilestoneResponse> milestonesById)
    {
        DashboardMilestoneRow? picked = projectMilestones
            .Where(m => m.TargetEndDate is not null)
            .OrderBy(m => m.TargetEndDate)
            .ThenBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(m => m.Id)
            .FirstOrDefault()
            ?? projectMilestones
                .OrderByDescending(m => m.CreatedOnUtc)
                .ThenBy(m => m.Id)
                .FirstOrDefault();

        if (picked is null)
        {
            return null;
        }

        DashboardMilestoneResponse counts = milestonesById[picked.Id];

        return new DashboardActiveMilestoneResponse(
            picked.Id,
            picked.Name,
            picked.Color,
            picked.TargetEndDate,
            counts.LinkedTotal,
            counts.LinkedClosed);
    }

    private static DashboardMemberResponse ToMember(DashboardMemberRow member) =>
        new(
            member.UserId,
            member.FirstName is null || member.LastName is null
                ? string.Empty
                : User.FormatFullName(member.FirstName, member.LastName),
            User.FormatInitials(member.FirstName, member.LastName));

    private static int Count(IEnumerable<DashboardOpenGroupRow> groups) => groups.Sum(g => g.Count);
}
```

- [x] **Verlo pasar.** Repite los comandos (14 en verde).
- [x] `dotnet build "Aurora Flowboard.slnx"` y `./test/Aurora.Flowboard.ArchitectureTests/bin/Debug/net10.0/Aurora.Flowboard.ArchitectureTests.exe` (`Responses_Should_BeSealedRecords` cubre los `Dashboard*Response`).
- [x] Commit: `Compute the dashboard from aggregated open work` con los tres ficheros de `Dashboard/GetDashboard/`, `GlobalUsings.cs`, `DashboardQueryData.cs`, `DashboardCalculatorTests.cs` y este plan.

### Tarea 9: `GetDashboardHandler`

**Skill:** `create-feature`

**Ficheros:**
- Crear: `src/Aurora.Flowboard.Application/Dashboard/GetDashboard/GetDashboardQuery.cs`, `GetDashboardHandler.cs`
- Test: `test/Aurora.Flowboard.Application.UnitTests/Dashboard/GetDashboardHandlerTests.cs`

- [ ] **Escribir el test que falla.** Crea `test/Aurora.Flowboard.Application.UnitTests/Dashboard/GetDashboardHandlerTests.cs`:

```csharp
using Aurora.Flowboard.Application.Users.GetMySummary;
using static Aurora.Flowboard.Application.UnitTests.WorkItems.GetWorkItemsQueryData;

namespace Aurora.Flowboard.Application.UnitTests.Dashboard;

public sealed class GetDashboardHandlerTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly User _caller = GetUser("Casey", "Caller");
    private readonly User _teammate = GetUser("Taylor", "Teammate");
    private readonly GetDashboardHandler _handler;

    public GetDashboardHandlerTests()
    {
        _userContext.UserId.Returns(_caller.Id);
        _dateTimeProvider.UtcNow.Returns(Now);
        _handler = new GetDashboardHandler(_dbContext, _userContext, _dateTimeProvider);
    }

    [Fact]
    public async Task Should_ReturnTheEmptyDashboard_When_UserHasNoProjects()
    {
        // Arrange
        Project foreign = GetThreeStageProject("Foreign", "FRG", _teammate);
        Arrange([foreign], [CreateInState(foreign, InProgressState, _teammate, Days(9))]);

        // Act
        Result<DashboardResponse> result = await _handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Me.Should().Be(new DashboardMeResponse(0, 0, 0, 0));
        result.Value.Aging.Buckets.Should().HaveCount(4).And.OnlyContain(b => b.Count == 0);
        result.Value.Portfolio.Should().BeEmpty();
        _ = _dbContext.DidNotReceive().WorkItems;
    }

    [Fact]
    public async Task Should_PlaceInFlightItems_InAgeBuckets_AtTheExactLimits()
    {
        // Arrange
        Project project = GetThreeStageProject("Aurora", "AUR", _caller);
        TimeSpan[] timesInState =
        [
            Days(3) + Hours(23),
            Days(4),
            Days(6) + Hours(23),
            Days(7),
            Days(13) + Hours(23),
            Days(14)
        ];
        Arrange([project], [.. timesInState.Select(age => CreateInState(project, InProgressState, _caller, age))]);

        // Act
        Result<DashboardResponse> result = await _handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        // Assert
        result.Value.Aging.Buckets.Select(b => b.Count).Should().Equal(1, 2, 2, 1);
        result.Value.Team.Stalled.Should().Be(3);
    }

    [Fact]
    public async Task Should_NotCountBacklogAsStalled_WhateverItsAge()
    {
        // Arrange
        Project project = GetThreeStageProject("Aurora", "AUR", _caller);
        Arrange([project], [CreateInBacklog(project, _caller, Days(30), assignee: _caller)]);

        // Act
        Result<DashboardResponse> result = await _handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        // Assert
        result.Value.Me.Should().Be(new DashboardMeResponse(1, 0, 0, 0));
        result.Value.Team.Stalled.Should().Be(0);
        result.Value.Aging.OldestInFlight.Should().BeNull();
    }

    [Fact]
    public async Task Should_CountASingleActiveState_AsInFlight()
    {
        // Arrange
        Project project = GetSingleStateProject("Single", "SGL", _caller);
        Arrange([project], [CreateInBacklog(project, _caller, Days(7))]);

        // Act
        Result<DashboardResponse> result = await _handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        // Assert
        result.Value.Team.Stalled.Should().Be(1);
        result.Value.Portfolio.Single().Stalled.Should().Be(1);
    }

    [Fact]
    public async Task Should_MatchMyOpenIssues_OfMySummary()
    {
        // Arrange
        Project active = GetThreeStageProject("Active", "ACT", _caller, _teammate);
        Project completed = GetThreeStageProject("Completed", "CMP", _caller);
        Project archived = GetThreeStageProject("Archived", "ARC", _caller);
        WorkItem mineInFlight = CreateInState(active, InProgressState, _caller, Days(2), assignee: _caller);
        WorkItem mineInBacklog = CreateInBacklog(active, _caller, Days(1), assignee: _caller);
        WorkItem mineDone = CreateInState(active, DoneState, _caller, Days(1), assignee: _caller);
        WorkItem theirs = CreateInState(active, InProgressState, _caller, Days(1), assignee: _teammate);
        WorkItem mineInCompleted = CreateInState(completed, InProgressState, _caller, Days(1), assignee: _caller);
        WorkItem mineInArchived = CreateInState(archived, InProgressState, _caller, Days(1), assignee: _caller);
        ChangeStatus(completed, ProjectStatus.Completed, _caller);
        ChangeStatus(archived, ProjectStatus.Archived, _caller);
        AttachWorkItems(active, mineInFlight, mineInBacklog, mineDone, theirs);
        AttachWorkItems(completed, mineInCompleted);
        AttachWorkItems(archived, mineInArchived);
        Arrange(
            [active, completed, archived],
            [mineInFlight, mineInBacklog, mineDone, theirs, mineInCompleted, mineInArchived]);

        // Act
        Result<DashboardResponse> dashboard = await _handler.Handle(new GetDashboardQuery(), CancellationToken.None);
        Result<MySummaryResponse> summary = await new GetMySummaryHandler(_dbContext)
            .Handle(new GetMySummaryQuery(_caller.Id), CancellationToken.None);

        // Assert
        dashboard.Value.Me.Open.Should().Be(2);
        summary.Value.Counts.MyOpenIssues.Should().Be(dashboard.Value.Me.Open);
    }

    [Fact]
    public async Task Should_ExcludeACompletedProject_FromOpenWork_ButKeepItsRow()
    {
        // Arrange
        Project completed = GetThreeStageProject("Closed", "CLS", _caller);
        Milestone milestone = CreateActiveMilestone(completed, _caller, "Release", new DateOnly(2026, 2, 1));
        WorkItem stalled = CreateInState(completed, InProgressState, _caller, Days(20), assignee: _caller, milestone: milestone);
        ChangeStatus(completed, ProjectStatus.Completed, _caller);
        Arrange([completed], [stalled]);

        // Act
        Result<DashboardResponse> result = await _handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        // Assert
        result.Value.Me.Should().Be(new DashboardMeResponse(0, 0, 0, 0));
        result.Value.Team.Stalled.Should().Be(0);
        result.Value.Aging.OldestInFlight.Should().BeNull();
        result.Value.Milestones.Should().BeEmpty();
        DashboardProjectResponse row = result.Value.Portfolio.Single();
        row.Status.Should().Be(ProjectStatus.Completed);
        row.Open.Should().Be(0);
        row.ActiveMilestone.Should().BeNull();
        row.Distribution.Should().HaveCount(3).And.OnlyContain(d => d.Count == 0);
    }

    [Fact]
    public async Task Should_CountClosedLast7Days_ByCategory_IncludingCompletedProjects()
    {
        // Arrange
        Project active = GetThreeStageProject("Active", "ACT", _caller);
        Project completed = GetThreeStageProject("Completed", "CMP", _caller);
        WorkItem closedRecently = CreateInState(active, DoneState, _caller, Days(2));
        WorkItem closedAtTheWindowEdge = CreateInState(active, DoneState, _caller, Days(7));
        WorkItem closedLongAgo = CreateInState(active, DoneState, _caller, Days(8));
        WorkItem cancelledRecently = CreateInState(active, CancelledState, _caller, Days(1));
        WorkItem reopened = CreateInState(active, InProgressState, _caller, Days(1));
        SetCompletedOnUtc(reopened, Now - Days(2));
        WorkItem closedInCompletedProject = CreateInState(completed, DoneState, _caller, Days(3));
        ChangeStatus(completed, ProjectStatus.Completed, _caller);
        Arrange(
            [active, completed],
            [closedRecently, closedAtTheWindowEdge, closedLongAgo, cancelledRecently, reopened, closedInCompletedProject]);

        // Act
        Result<DashboardResponse> result = await _handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        // Assert
        result.Value.Team.ClosedLast7Days.Should().Be(3);
    }

    [Fact]
    public async Task Should_ReturnTheOldestInFlightItem_IgnoringTheBacklog()
    {
        // Arrange
        Project project = GetThreeStageProject("Aurora", "AUR", _caller);
        WorkItem oldBacklog = CreateInBacklog(project, _caller, Days(40));
        WorkItem oldest = CreateInState(project, ReviewState, _caller, Days(12));
        WorkItem newer = CreateInState(project, InProgressState, _caller, Days(3));
        Arrange([project], [oldBacklog, oldest, newer]);

        // Act
        Result<DashboardResponse> result = await _handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        // Assert
        result.Value.Aging.OldestInFlight.Should().Be(new DashboardOldestInFlightResponse(
            oldest.Code, oldest.Title, project.Id, "Aurora", Now - Days(12)));
    }

    [Fact]
    public async Task Should_ComputeMilestoneCounts_FromOpenAndCompletedItems()
    {
        // Arrange
        Project project = GetThreeStageProject("Aurora", "AUR", _caller);
        Milestone milestone = CreateActiveMilestone(project, _caller, "Sprint 1", new DateOnly(2026, 3, 15));
        Milestone draft = Milestone.Create("Draft", null, MilestoneColor, null, null, project, _caller, Now).Value;
        WorkItem stalledLinked = CreateInState(project, InProgressState, _caller, Days(9), milestone: milestone);
        WorkItem backlogLinked = CreateInBacklog(project, _caller, Days(1), milestone: milestone);
        WorkItem doneLinked = CreateInState(project, DoneState, _caller, Days(30), milestone: milestone);
        WorkItem cancelledLinked = CreateInState(project, CancelledState, _caller, Days(1), milestone: milestone);
        WorkItem draftLinked = CreateInBacklog(project, _caller, Days(1), milestone: draft);
        WorkItem unlinked = CreateInState(project, InProgressState, _caller, Days(1));
        Arrange([project], [stalledLinked, backlogLinked, doneLinked, cancelledLinked, draftLinked, unlinked]);

        // Act
        Result<DashboardResponse> result = await _handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        // Assert
        DashboardMilestoneResponse linked = result.Value.Milestones.Should().ContainSingle().Subject;
        linked.Id.Should().Be(milestone.Id);
        linked.ProjectName.Should().Be("Aurora");
        linked.LinkedTotal.Should().Be(3);
        linked.LinkedClosed.Should().Be(1);
        linked.LinkedStalled.Should().Be(1);
        result.Value.Portfolio.Single().ActiveMilestone!.LinkedTotal.Should().Be(3);
    }

    [Fact]
    public async Task Should_MapThePortfolioRow_WithItsMembers()
    {
        // Arrange
        Project project = GetThreeStageProject("Aurora", "AUR", _caller, _teammate);
        Arrange([project], []);

        // Act
        Result<DashboardResponse> result = await _handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        // Assert
        DashboardProjectResponse row = result.Value.Portfolio.Single();
        row.ProjectId.Should().Be(project.Id);
        row.Name.Should().Be("Aurora");
        row.Color.Should().Be(project.Color.Value);
        row.Kind.Should().Be(ProjectKind.Client);
        row.Status.Should().Be(ProjectStatus.Active);
        row.Distribution.Select(d => d.Name).Should().Equal(BacklogState, InProgressState, ReviewState);
        row.Members.Should().Equal(
            new DashboardMemberResponse(_caller.Id, "Casey Caller", "CC"),
            new DashboardMemberResponse(_teammate.Id, "Taylor Teammate", "TT"));
    }

    private static TimeSpan Days(int days) => TimeSpan.FromDays(days);

    private static TimeSpan Hours(int hours) => TimeSpan.FromHours(hours);

    private void Arrange(IReadOnlyCollection<Project> projects, IReadOnlyCollection<WorkItem> workItems)
    {
        _dbContext.Seed(c => c.Projects, projects);
        _dbContext.Seed(c => c.WorkItems, workItems);
        _dbContext.Seed(c => c.Users, _caller, _teammate);
    }
}
```

- [ ] **Verlo fallar.** Para que compile, crea `GetDashboardQuery.cs` con su código final y un handler stub:

```csharp
namespace Aurora.Flowboard.Application.Dashboard.GetDashboard;

internal sealed class GetDashboardHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider) : IQueryHandler<GetDashboardQuery, DashboardResponse>
{
    public Task<Result<DashboardResponse>> Handle(GetDashboardQuery query, CancellationToken cancellationToken)
    {
        // Stub: reads the dependencies only so that CS9113 (unread parameter) does not stop the build.
        _ = (dbContext, userContext, dateTimeProvider);

        return Task.FromResult<Result<DashboardResponse>>(DashboardCalculator.Build(DashboardSnapshot.Empty));
    }
}
```

```bash
dotnet build test/Aurora.Flowboard.Application.UnitTests/Aurora.Flowboard.Application.UnitTests.csproj
./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests.exe -class "Aurora.Flowboard.Application.UnitTests.Dashboard.GetDashboardHandlerTests"
```

  Fallo esperado: fallan todos salvo `Should_ReturnTheEmptyDashboard_When_UserHasNoProjects`. Si alguno falla con `Fixture step '...' failed`, corrige la fixture antes de seguir.

- [ ] **Implementar.** `GetDashboardQuery.cs`:

```csharp
namespace Aurora.Flowboard.Application.Dashboard.GetDashboard;

// No parameters: the scope is the current user (IUserContext), so there is nothing to validate.
public sealed record GetDashboardQuery : IQuery<DashboardResponse>;
```

  `GetDashboardHandler.cs`:

```csharp
namespace Aurora.Flowboard.Application.Dashboard.GetDashboard;

// Reads the rows DashboardCalculator turns into the response. Every open-work counter comes from one
// GROUP BY, so they always add up. The milestone closed counts, closedLast7Days and oldestInFlight are
// separate reads: a concurrent move can make them disagree with it for one load (spec, section 6).
internal sealed class GetDashboardHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider) : IQueryHandler<GetDashboardQuery, DashboardResponse>
{
    public async Task<Result<DashboardResponse>> Handle(
        GetDashboardQuery query,
        CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;
        DateTime now = dateTimeProvider.UtcNow;

        List<DashboardProjectRow> projects = await dbContext
            .Projects
            .AsNoTracking()
            .WhereMyProjects(userId)
            .Select(p => new DashboardProjectRow(p.Id, p.Name, p.Color.Value, p.Kind, p.Status))
            .ToListAsync(cancellationToken);

        if (projects.Count == 0)
        {
            return DashboardCalculator.Build(DashboardSnapshot.Empty);
        }

        HashSet<Guid> activeProjectIds = [.. projects
            .Where(p => MyProjectsQueryExtensions.AcceptsOpenWork(p.Status))
            .Select(p => p.Id)];

        List<DashboardStateRow> states = await dbContext
            .Projects
            .AsNoTracking()
            .WhereMyProjects(userId)
            .SelectMany(p => p.FlowStates)
            .Where(s => s.Category == FlowStateCategory.Active)
            .Select(s => new DashboardStateRow(s.Id, s.ProjectId, s.Name, s.Color.Value, s.SortOrder))
            .ToListAsync(cancellationToken);

        HashSet<Guid> backlogStateIds = FlowStateStageResolver.GetBacklogStateIds(
            states.Select(s => (s.Id, s.ProjectId, s.SortOrder)));
        List<Guid> openStateIds = [.. states.Where(s => activeProjectIds.Contains(s.ProjectId)).Select(s => s.Id)];
        List<Guid> inFlightStateIds = [.. openStateIds.Where(id => !backlogStateIds.Contains(id))];

        List<DashboardOpenGroupRow> openGroups = await GetOpenGroupsAsync(openStateIds, userId, now, cancellationToken);
        List<DashboardMilestoneRow> milestones = await GetActiveMilestonesAsync(userId, cancellationToken);
        Dictionary<Guid, int> closedByMilestone = await GetClosedByMilestoneAsync([.. milestones.Select(m => m.Id)], cancellationToken);
        int closedLast7Days = await CountClosedLast7DaysAsync([.. projects.Select(p => p.Id)], now, cancellationToken);
        DashboardOldestInFlightResponse? oldestInFlight = await GetOldestInFlightAsync(inFlightStateIds, cancellationToken);
        List<DashboardMemberRow> members = await GetMembersAsync(userId, cancellationToken);

        return DashboardCalculator.Build(new DashboardSnapshot(
            projects,
            states,
            openGroups,
            milestones,
            closedByMilestone,
            closedLast7Days,
            oldestInFlight,
            members));
    }

    private async Task<List<DashboardOpenGroupRow>> GetOpenGroupsAsync(
        List<Guid> openStateIds,
        Guid userId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (openStateIds.Count == 0)
        {
            return [];
        }

        // Whole 24-hour blocks: entered at or before now - N days is N days or more in the state.
        // A ternary chain, because expression trees cannot hold a switch (CS8514).
        DateTime agingSince = now.AddDays(-DashboardCalculator.AgingThresholdDays);
        DateTime stalledSince = now.AddDays(-WorkItem.StalledThresholdDays);
        DateTime oldSince = now.AddDays(-DashboardCalculator.OldThresholdDays);

        return await dbContext
            .WorkItems
            .AsNoTracking()
            .Where(w => openStateIds.Contains(w.FlowStateId))
            .GroupBy(w => new
            {
                w.FlowStateId,
                w.MilestoneId,
                IsMine = w.AssigneeId == userId,
                IsUnassigned = w.AssigneeId == null,
                IsCriticalHigh = w.Priority == Priority.High || w.Priority == Priority.Critical,
                AgeBand = w.EnteredCurrentStateOnUtc > agingSince
                    ? DashboardCalculator.FreshBand
                    : w.EnteredCurrentStateOnUtc > stalledSince
                        ? DashboardCalculator.AgingBand
                        : w.EnteredCurrentStateOnUtc > oldSince
                            ? DashboardCalculator.StalledBand
                            : DashboardCalculator.OldBand
            })
            .Select(g => new DashboardOpenGroupRow(
                g.Key.FlowStateId,
                g.Key.MilestoneId,
                g.Key.IsMine,
                g.Key.IsUnassigned,
                g.Key.IsCriticalHigh,
                g.Key.AgeBand,
                g.Count()))
            .ToListAsync(cancellationToken);
    }

    private Task<List<DashboardMilestoneRow>> GetActiveMilestonesAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext
            .Projects
            .AsNoTracking()
            .WhereMyActiveProjects(userId)
            .SelectMany(p => p.Milestones)
            .Where(m => m.Status == MilestoneStatus.Active)
            .Select(m => new DashboardMilestoneRow(m.Id, m.ProjectId, m.Name, m.Color.Value, m.TargetEndDate, m.CreatedOnUtc))
            .ToListAsync(cancellationToken);

    // Completed with any date. A Cancelled item no longer counts towards its milestone.
    private async Task<Dictionary<Guid, int>> GetClosedByMilestoneAsync(List<Guid> milestoneIds, CancellationToken cancellationToken)
    {
        if (milestoneIds.Count == 0)
        {
            return [];
        }

        List<DashboardMilestoneCountRow> rows = await dbContext
            .WorkItems
            .AsNoTracking()
            .Where(w => w.MilestoneId != null
                && milestoneIds.Contains(w.MilestoneId.Value)
                && w.FlowState.Category == FlowStateCategory.Completed)
            .GroupBy(w => w.MilestoneId ?? Guid.Empty)
            .Select(g => new DashboardMilestoneCountRow(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.MilestoneId, r => r.Count);
    }

    // By category and not by CompletedOnUtc alone: Move also sets it when cancelling, and nothing clears
    // it when an item is reopened. Completed projects count, because the closing did happen.
    private Task<int> CountClosedLast7DaysAsync(List<Guid> projectIds, DateTime now, CancellationToken cancellationToken)
    {
        DateTime closedSince = now.AddDays(-DashboardCalculator.ClosedWindowDays);

        return dbContext
            .WorkItems
            .AsNoTracking()
            .CountAsync(
                w => projectIds.Contains(w.ProjectId)
                    && w.FlowState.Category == FlowStateCategory.Completed
                    && w.CompletedOnUtc >= closedSince,
                cancellationToken);
    }

    private async Task<DashboardOldestInFlightResponse?> GetOldestInFlightAsync(List<Guid> inFlightStateIds, CancellationToken cancellationToken)
    {
        if (inFlightStateIds.Count == 0)
        {
            return null;
        }

        return await dbContext
            .WorkItems
            .AsNoTracking()
            .Where(w => inFlightStateIds.Contains(w.FlowStateId))
            .OrderBy(w => w.EnteredCurrentStateOnUtc)
            .ThenBy(w => w.Id)
            .Select(w => new DashboardOldestInFlightResponse(w.Code, w.Title, w.ProjectId, w.Project.Name, w.EnteredCurrentStateOnUtc))
            .FirstOrDefaultAsync(cancellationToken);
    }

    // Names through correlated subqueries: projecting the User would also load its roles (ef-core-queries.md).
    private Task<List<DashboardMemberRow>> GetMembersAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext
            .Projects
            .AsNoTracking()
            .WhereMyProjects(userId)
            .SelectMany(p => p.Members)
            .Select(m => new DashboardMemberRow(
                m.ProjectId,
                m.UserId,
                dbContext.Users.Where(u => u.Id == m.UserId).Select(u => u.FirstName).FirstOrDefault(),
                dbContext.Users.Where(u => u.Id == m.UserId).Select(u => u.LastName).FirstOrDefault()))
            .ToListAsync(cancellationToken);
}
```

- [ ] **Verlo pasar.** Repite los comandos (10 en verde) y ejecuta la suite de Application completa sin filtro.
- [ ] `dotnet build "Aurora Flowboard.slnx"` y `./test/Aurora.Flowboard.ArchitectureTests/bin/Debug/net10.0/Aurora.Flowboard.ArchitectureTests.exe` (`Queries_Should_HaveValidator` excluye a `GetDashboardQuery` porque no tiene propiedades públicas).
- [ ] Commit: `Read the dashboard rows with one aggregate query over open work` con `GetDashboardQuery.cs`, `GetDashboardHandler.cs`, `GetDashboardHandlerTests.cs` y este plan.

### Tarea 10: endpoints `GET work-items` y `GET dashboard`

**Skill:** `create-feature`

**Ficheros:**
- Modificar: `src/Aurora.Flowboard.Api/Endpoints/EndpointTags.cs`
- Crear: `src/Aurora.Flowboard.Api/Endpoints/WorkItems/GetWorkItems.cs`, `src/Aurora.Flowboard.Api/Endpoints/Dashboard/GetDashboard.cs`
- Modificar: `src/Aurora.Flowboard.Api/Aurora.Flowboard.Api.http`

Api no tiene tests unitarios: la tarea se cubre con los tests de arquitectura y con verificación manual.

- [ ] **Implementar.** En `EndpointTags`, en orden alfabético:

```csharp
    internal const string Dashboard = "Dashboard";
```

  `src/Aurora.Flowboard.Api/Endpoints/WorkItems/GetWorkItems.cs`. Los parámetros van en un request con `[AsParameters]` para no pasar de siete en la lambda:

```csharp
using Aurora.Flowboard.Application.Abstractions.Pagination;
using Aurora.Flowboard.Application.WorkItems.GetWorkItems;

namespace Aurora.Flowboard.Api.Endpoints.WorkItems;

public sealed class GetWorkItems : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "work-items",
            async (
                [AsParameters] GetWorkItemsRequest request,
                IQueryHandler<GetWorkItemsQuery, PagedResponse<WorkItemListItemResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetWorkItemsQuery(
                    request.Assignee,
                    request.CreatedBy,
                    request.Stage,
                    request.Stalled,
                    request.Priority,
                    request.Sort,
                    request.Page,
                    request.PageSize);

                Result<PagedResponse<WorkItemListItemResponse>> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("GetWorkItems")
            .WithTags(EndpointTags.WorkItems)
            .Produces<PagedResponse<WorkItemListItemResponse>>(StatusCodes.Status200OK)
            .ProducesStandardProblems(
                StatusCodes.Status400BadRequest);
    }

    // Every filter is optional; GetWorkItemsValidator answers 400 for any value it does not accept.
    internal sealed record GetWorkItemsRequest(
        string? Assignee = null,
        string? CreatedBy = null,
        string? Stage = null,
        string? Stalled = null,
        string? Priority = null,
        string? Sort = null,
        int Page = PaginationDefaults.DefaultPage,
        int PageSize = PaginationDefaults.DefaultPageSize);
}
```

  `src/Aurora.Flowboard.Api/Endpoints/Dashboard/GetDashboard.cs`:

```csharp
using Aurora.Flowboard.Application.Dashboard.GetDashboard;

namespace Aurora.Flowboard.Api.Endpoints.Dashboard;

public sealed class GetDashboard : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "dashboard",
            async (
                IQueryHandler<GetDashboardQuery, DashboardResponse> handler,
                CancellationToken cancellationToken) =>
            {
                Result<DashboardResponse> result = await handler.Handle(new GetDashboardQuery(), cancellationToken);

                return result.Match(Results.Ok, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("GetDashboard")
            .WithTags(EndpointTags.Dashboard)
            .Produces<DashboardResponse>(StatusCodes.Status200OK)
            .ProducesStandardProblems();
    }
}
```

  Ambos usan `.RequireAuthorization()` sin política de rol: solo la membresía da visibilidad. La cadena fluida queda dentro de `MapEndpoint`, porque `ApiLayerTests` inspecciona ahí su IL.

  En `Aurora.Flowboard.Api.http`, añade al final:

```http
### Login: copy accessToken from the response into @token
POST {{Aurora.Flowboard.Api_HostAddress}}/api/v1/flowboard/auth/login
Content-Type: application/json

{
  "email": "<email of a seeded user>",
  "password": "<password>"
}

###
@token = <accessToken>

### Dashboard of the current user
GET {{Aurora.Flowboard.Api_HostAddress}}/api/v1/flowboard/dashboard
Authorization: Bearer {{token}}
Accept: application/json

### My Issues: assigned to me
GET {{Aurora.Flowboard.Api_HostAddress}}/api/v1/flowboard/work-items?assignee=me&page=1&pageSize=25
Authorization: Bearer {{token}}
Accept: application/json

### My Issues: reported by me
GET {{Aurora.Flowboard.Api_HostAddress}}/api/v1/flowboard/work-items?createdBy=me
Authorization: Bearer {{token}}
Accept: application/json

### Stalled critical and high work in flight
GET {{Aurora.Flowboard.Api_HostAddress}}/api/v1/flowboard/work-items?stage=InFlight&stalled=true&priority=High,Critical
Authorization: Bearer {{token}}
Accept: application/json

### Invalid filter: 400
GET {{Aurora.Flowboard.Api_HostAddress}}/api/v1/flowboard/work-items?stage=Done
Authorization: Bearer {{token}}
Accept: application/json
```

  No pongas credenciales reales en el `.http`: el fichero se versiona.

- [ ] **Compilar y pasar los tests de arquitectura.**

```bash
dotnet build src/Aurora.Flowboard.Api/Aurora.Flowboard.Api.csproj
dotnet build test/Aurora.Flowboard.ArchitectureTests/Aurora.Flowboard.ArchitectureTests.csproj
./test/Aurora.Flowboard.ArchitectureTests/bin/Debug/net10.0/Aurora.Flowboard.ArchitectureTests.exe
```

  Esperado: todo en verde. Este fichero lo cubren en especial `Endpoints_Should_ResideInNamespaceMatchingAnEndpointTag` (`Dashboard`), `EndpointRequests_Should_BeNestedInternalSealedRecords`, `Endpoints_Should_RequireAuthorizationUnlessExplicitlyAnonymous` y `Endpoints_Should_DeclareOpenApiMetadata`.

- [ ] **Verificación manual.** Arranca con `dotnet run --project src/Aurora.Flowboard.AppHost`, haz login con el `.http` y comprueba:
  1. `GET dashboard` responde 200 con `me`, `team`, `aging.buckets` (siempre 4, el último con `"to": null`), `milestones` y `portfolio`. `stage` sale como `"Backlog"`/`"InFlight"` y `targetEndDate` como `"YYYY-MM-DD"` o `null`.
  2. `me.open` coincide con `counts.myOpenIssues` de `GET users/my-summary`.
  3. El portafolio lista los mismos proyectos que `my-summary` y en el mismo orden.
  4. `GET work-items?assignee=me` devuelve `items`, `page`, `pageSize`, `totalCount` y `totalPages`; `?stage=Done` responde 400; sin `Authorization`, los dos endpoints responden 401.
  5. `GET projects/{id}/board` tiene `stage` en cada columna y `enteredCurrentStateOnUtc` en cada card; `GET work-items/{code}` tiene `enteredCurrentStateOnUtc`. Mueve un item y comprueba que la fecha cambia a la del movimiento.
  6. **SQL en los logs de EF** (dashboard de Aspire, logs de la Api): el dashboard ejecuta un único `GROUP BY` sobre `work_items`, con un `CASE WHEN ... entered_current_state_on_utc > @...` para el tramo y `flow_state_id = ANY (@...)`; la prioridad se compara con `'High'`/`'Critical'` como texto. La búsqueda filtra `priority = ANY (@...)` con un array de texto y ordena por `flow_state_id = ANY (@...)`, `entered_current_state_on_utc`, `id`. Si alguna consulta falla con `could not be translated` o evalúa en cliente, para y avisa.
- [ ] Commit: `Expose the dashboard and the cross-project work item search` con `EndpointTags.cs`, `GetWorkItems.cs`, `GetDashboard.cs`, `Aurora.Flowboard.Api.http` y este plan.

### Tarea 11: documentación de las reglas

**Skill:** —

**Ficheros:**
- Modificar: `.claude/rules/work-items.md`
- Crear: `.claude/rules/dashboard.md`
- Modificar: `CLAUDE.md`

Las rules están en inglés, como el resto de `.claude/rules/`.

- [ ] **`work-items.md`.** En el frontmatter, añade a `paths:` la línea `  - "src/**/Dashboard/**/*.cs"`. En la sección del board, sustituye el último punto (`- /board is the only project board endpoint...`) por:

```markdown
- `/board` is the only project board endpoint. A near-identical `GET projects/{id}/work-items` was removed as an unused duplicate — don't reintroduce a per-project work item list under `work-items/`; extend `GetProjectBoardQuery` instead. `GET work-items` (no project in the route) is the cross-project search below, not a per-project list.
- Each column carries `stage` (`Backlog` / `InFlight`, see below). Only `Active` states have a stage, so every column has one.
```

  Después de esa sección, añade:

```markdown
## Cross-project search — `GET work-items`

`Application/WorkItems/GetWorkItems/` lists the **open** work items of my **active** projects (`WhereMyActiveProjects`, see `dashboard.md`), paginated, for the Home and My Issues.

- The scope comes from the Active states of those projects (`stateIds.Contains(w.FlowStateId)`): open only, never from a `Completed`, `Archived` or foreign project. There is no opt-in for closed items.
- Filters arrive as `string?` and combine with AND. `GetWorkItemsValidator` answers 400 for any value outside `WorkItemSearchFilters`, case-sensitive: `assignee=me`, `createdBy=me`, `stage=Backlog|InFlight`, `stalled=true`, `priority=` a comma-separated list of `Priority` names (numbers are rejected), `sort=stageThenTimeInState` (the only order). A valid combination with no results (`stage=Backlog&stalled=true`) is a 200 with an empty page.
- Order: in flight first, then `EnteredCurrentStateOnUtc` ascending, then `Id`.

## Time in state, stage and stalled

- `WorkItem.EnteredCurrentStateOnUtc` is written by `Create` (its `createdOnUtc`) and `Move` (its `changedOnUtc`) only. No other mutation touches it. For a closed item it is when it entered the terminal state.
- **Stage is not persisted.** `FlowState.GetBacklogStateId` takes a project's Active states: the one with the lowest `SortOrder` is the backlog, except when there is a single Active state — then that state is in flight and the project has no backlog. `FlowStateStageResolver` (`Projects/Shared/`) does it for several projects at once. Never duplicate the rule in a handler.
- **Stalled** = in flight and `EnteredCurrentStateOnUtc <= now - WorkItem.StalledThresholdDays` (7). Compare against cutoff dates computed once from `IDateTimeProvider.UtcNow`, never with day arithmetic in SQL: "7 days" means 7 whole 24-hour blocks, not calendar days. A backlog item is never stalled.
- `stage` appears on board columns and search items; the detail (`GET work-items/{code}`) has `enteredCurrentStateOnUtc` but no `stage`.
```

- [ ] **Crear `.claude/rules/dashboard.md`:**

```markdown
---
paths:
  - "src/**/Dashboard/**/*.cs"
  - "src/Aurora.Flowboard.Application/Projects/Shared/MyProjectsQueryExtensions.cs"
  - "src/Aurora.Flowboard.Application/Users/GetMySummary/**/*.cs"
  - "test/**/Dashboard/**/*.cs"
---

# Dashboard

Spec: `docs/specs/2026-11-07-dashboard-design.md` (definitions in section 2).

## Two project sets

`Projects/Shared/MyProjectsQueryExtensions.cs` defines both; don't write the filters again in a handler.

| Set | Rule | Feeds |
|---|---|---|
| My projects (`WhereMyProjects`) | Member and `Status != Archived` | Sidebar (`my-summary` list), portfolio rows, `counts.projects`, `counts.members`, `closedLast7Days` |
| My active projects (`WhereMyActiveProjects`, `AcceptsOpenWork`) | Member and `Active` or `Maintenance` | Every open-work number: `my-summary` `myOpenIssues`, dashboard `me`, `team.stalled`, `team.unassigned`, `aging`, `milestones`, portfolio `open`/`stalled`/`distribution`/`activeMilestone`, and `GET work-items` |

- Why: a `Completed` project's items cannot be moved (`IsModifiable` is false), and the PRD §5.1 rule that forbids completing a project with open items is not enforced yet, so they would stay stalled forever. A `Completed` project keeps its portfolio row with zeros and `activeMilestone = null`.
- A new open-work metric uses the active set. A new list of projects uses my projects and `OrderLikeSidebar` (status, then name, `OrdinalIgnoreCase`), so it matches the Sidebar.
- `me.open` must always equal `myOpenIssues`; `GetDashboardHandlerTests` asserts it over the same data.

## One aggregate query

- `GetDashboardHandler` makes 8 reads. Every open-work counter comes from one `GROUP BY` over the open items of my active projects, keyed by state, milestone, mine, unassigned, critical/high and age band. Don't split it into one count per metric (the numbers must add up: buckets 7-13 + 14+ == `team.stalled`) and don't load work items into memory (the cost must depend on the combinations, not on the number of items).
- The age band is a ternary chain over cutoffs (`now - 4/7/14` days) inside the `GroupBy`: no `switch` in expression trees, no day arithmetic in SQL, one `now` per request.
- `DashboardCalculator` is the pure part: every rule that is not a SQL filter lives there and is tested without a database. The handler only reads. It filters `Completed` projects out again, so the rule holds whatever rows it is given.
- `closedLast7Days` filters by `Completed` category, because `Move` also sets `CompletedOnUtc` when cancelling and nothing clears it on reopening.
- **Consistency limit:** the milestone closed counts, `closedLast7Days` and `oldestInFlight` are separate reads, so a concurrent move can make them disagree with the `GROUP BY` for one load; the next load fixes it. Accepted: no read transaction and no cache. A future cache must respect the time budgets and stampede protection in `CLAUDE.md`.
- Unit tests run on LINQ-to-Objects (`MockDbSetHelper`): the `GROUP BY` translation, the ternary and `Contains` over `Priority` (a string column) are checked against Postgres by reading the SQL logs.
```

- [ ] **`CLAUDE.md`.** En la tabla "Area-specific rules", cambia la fila de `work-items.md` por:

```markdown
| `work-items.md` | Board endpoint, cross-project search, time in state, stage and stalled, detail vs. paginated activity endpoints, change log semantics, `Viewer` permissions |
```

  y añade, a continuación:

```markdown
| `dashboard.md` | The two project sets for open work, the dashboard aggregate query and its consistency limit |
```

- [ ] **Verificar.** Comprueba que las rutas y los nombres que citan las rules existen: `grep -rn "WhereMyActiveProjects\|FlowStateStageResolver\|GetBacklogStateId\|StalledThresholdDays\|WorkItemSearchFilters" src --include=*.cs | grep -v "/obj/"` debe encontrar cada uno.
- [ ] Commit: `Document the dashboard rules and the cross-project search` con `.claude/rules/work-items.md`, `.claude/rules/dashboard.md`, `CLAUDE.md` y este plan.

## Verificación final

- [ ] `dotnet-test-runner` limpio: build y los tres ejecutables de tests.
- [ ] `dotnet ef migrations has-pending-model-changes --project src/Aurora.Flowboard.Infrastructure --startup-project src/Aurora.Flowboard.Api`: sin cambios pendientes.
- [ ] `arch-guard` y `code-reviewer` sobre el diff de la rama contra `main`.
- [ ] Verificación manual:
  - Migración contra Postgres: backfill con y sin historial, `Up` y `Down` (tarea 3).
  - Las peticiones del `.http` y el SQL de los logs (tarea 10, puntos 1 a 6).
  - Avisar a la sesión `fb-web-dashboard` de que los endpoints están disponibles. El contrato no cambió respecto a su spec v1.1.
- [ ] Al terminar, recuerda al usuario que la regla del PRD §5.1 sigue pendiente y necesita su propio ticket (spec, sección 10).
