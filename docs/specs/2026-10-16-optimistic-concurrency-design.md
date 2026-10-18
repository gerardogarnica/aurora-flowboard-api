# Concurrencia optimista y conflictos de persistencia — Diseño

**Fecha:** 2026-10-16
**Estado:** Implementado
**Tickets:** I9 (ver `docs/reviews/review-remediation-plan.md` y la página de seguimiento)
**Rama:** `fix/optimistic-concurrency` · **PR:** #41
**Plan:** `docs/plans/2026-10-16-optimistic-concurrency-plan.md`
**ADR:** `docs/architecture/adr/0001-optimistic-concurrency.md`

## 1. Contexto y problema

- **Sin concurrency token en los aggregate roots.** Solo `UserToken` mapea `xmin` (`Infrastructure/Configurations/UserTokenConfiguration.cs:42`, adelantado en I4). `Project`, `Milestone`, `Component`, `WorkItem` y `User` no tienen `IsRowVersion`/`IsConcurrencyToken`, así que dos peticiones que leen el mismo agregado y escriben después se pisan (*lost update*). Ejemplo: dos admins de un proyecto se quitan mutuamente a la vez; cada uno ve dos admins en `Project.RemoveMember` (`Domain/Projects/Project.cs:312`) y el proyecto queda sin admin.
- **Carreras entre agregados (*write skew*).** `ChangeMilestoneStatusHandler` cuenta los work items abiertos y cierra el milestone; `ChangeWorkItemMilestoneHandler` comprueba el estado del milestone (`Domain/WorkItems/WorkItem.cs:450`) y escribe en `work_items`. Ninguno escribe la fila que lee el otro, así que un concurrency token no lo detecta: el milestone puede cerrarse con un work item abierto. Igual con `RetireComponentHandler` frente a `ChangeWorkItemComponentHandler` (`WorkItem.cs:414`) y con `CreateWorkItemHandler` cuando recibe milestone o componente (`WorkItem.cs:160`, `:173`). Y con `ChangeUserRoleHandler`: dos degradaciones simultáneas de dos Administrators distintos ven cada una "otro admin activo" y el sistema queda sin Administrator.
- **Unicidad con consultar-y-luego-insertar.** `CreateProjectHandler.cs:30` (prefijo) y `CreateUserHandler.cs:28` (email) consultan con `AnyAsync` y luego insertan; los índices únicos (`ProjectConfiguration.cs:30`, `UserConfiguration.cs:36`, `ComponentConfiguration.cs:41`, `MilestoneConfiguration.cs:57`, `WorkItemTagConfiguration.cs:17`) frenan el duplicado, pero con un `23505` que sale como 500.
- **Todo sale como 500.** `Api/Middlewares/GlobalExceptionHandler.cs` convierte cualquier excepción en 500, incluidas `DbUpdateConcurrencyException` y `23505`.
- **Ya resuelto, no se toca:** el contador de work items (`Project.IncrementWorkItemCounter`) está serializado con `SELECT ... FOR UPDATE` en `CreateWorkItemHandler.cs:22`.

## 2. Objetivo y criterios de éxito

- Una escritura sobre un dato que otra petición cambió entre la lectura y la escritura devuelve **409** (`Persistence.ConcurrencyConflict`), nunca 500 ni una actualización perdida.
- Una violación de índice único por carrera devuelve **409** (`Persistence.UniqueViolation`).
- Las invariantes "un proyecto tiene al menos un admin", "un milestone cerrado / componente retirado no tiene work items abiertos" y "el sistema tiene al menos un Administrator activo" se mantienen bajo concurrencia.
- Los tests unitarios existentes de los handlers implicados siguen existiendo (sin SQL crudo nuevo en handlers).
- Los tres escenarios de la verificación manual (sección 9) dan 409.

## 3. Alcance

**Dentro:**
- `xmin` como concurrency token en `Project`, `Milestone`, `Component`, `WorkItem` y `User`.
- `Project.AddMember` toca el root (`UpdatedOnUtc`).
- Transacciones `Serializable` en los handlers con write skew entre agregados o filas.
- `PersistenceConflictBehavior`: traduce `DbUpdateConcurrencyException`, `40001` y `23505` a `Result.Fail` con `Conflict`.
- Migrar el `catch` de `ChangePasswordHandler` al behavior.
- `Produces 409` en los endpoints de comandos.
- Rule nueva, actualización de `authentication.md`, `CLAUDE.md` y las skills `create-feature` / `domain-entity`.

**Fuera de alcance:**
- ETag / `If-Match` en la API (concurrencia entre el GET del cliente y su escritura, mencionada en el PRD): no se hará en I9; necesitaría su propio spec.
- Reintento automático en el servidor: no se hará; el cliente recarga y reintenta.
- Errores de unicidad específicos por índice (`ProjectErrors.PrefixAlreadyExists` en la carrera): no se hará; las comprobaciones previas siguen dando el error específico en el caso normal.
- `xmin` en `TemplateFlow`: no se hará; solo lo escribe el seeder.
- Reabrir (`Move` a un estado abierto) un work item de un milestone ya cerrado: es una regla de negocio que hoy no existe, no una carrera; backlog.
- Tests de integración contra Postgres: no hay proyecto; verificación manual (sección 9).
- Refactors R* de la revisión; en particular R9 (behaviors) debe partir de esta rama si va después.

## 4. Supuestos y decisiones

| # | Supuesto o decisión | Motivo |
|---|---|---|
| 1 | El 409 de concurrencia es genérico ("reload and try again"), sin reintento en el servidor | El cliente es quien sabe si reintentar; un reintento ciego en el servidor puede reaplicar una decisión tomada sobre datos viejos. |
| 2 | El 409 por `23505` es genérico (`Persistence.UniqueViolation`) | `DbException` no expone el nombre de la constraint sin depender de Npgsql; la comprobación previa ya da el error específico en el caso normal. |
| 3 | No se exponen ETag ni `If-Match` | `xmin` protege la ventana leer-escribir dentro de una petición; la concurrencia entre peticiones del cliente es otra funcionalidad. |
| 4 | `TemplateFlow` no lleva `xmin` | No se edita por HTTP. |
| 5 | `Project.AddMember` fija `UpdatedOnUtc` | Sin escribir la fila del root, `xmin` no detecta un alta concurrente con una baja. `RemoveMember` ya lo hace. |
| 6 | `AddComment`, `UpdateComment`, `RemoveComment` y `LogTime` no tocan el root | Son altas conmutativas o last-write-wins sobre un comentario; ninguna invariante depende de ellas. |
| 7 | Se acepta que `CreateWorkItem` (que cambia `WorkItemCounter`) provoque un 409 en un `UpdateProject`/`AddMember` simultáneo | La ventana es de milisegundos y el cliente reintenta. |
| 8 | Las carreras entre agregados se resuelven con transacciones `Serializable` (SSI de Postgres), no con `FOR UPDATE`/`FOR SHARE` | `FromSqlRaw` no funciona sobre el `DbSet` simulado: con SQL crudo, `ChangeMilestoneStatus`, `RetireComponent`, `ChangeWorkItemMilestone` y `ChangeWorkItemComponent` perderían sus tests de handler. SSI detecta el write skew y aborta un lado con `40001`. |
| 9 | `RefreshTokenHandler` (→ 401) y `LogoutHandler` (→ `Ok`) conservan su `catch (DbUpdateConcurrencyException)`; `ChangePasswordHandler` se migra al behavior | Refresh y logout tienen una semántica distinta de 409; change password ya devolvía 409 con la misma acción del cliente (reintentar). |
| 10 | Una sola migración `AddConcurrencyTokens`; sin SQL efectivo | Npgsql no emite SQL para `xmin`; solo cambia el snapshot. |
| 11 | `ChangeUserRoleHandler` abre una transacción `Serializable` cuando degrada a un Administrator activo | Cierra el write skew entre dos usuarios distintos con el mismo mecanismo que el supuesto 8. |
| 12 | Sin tests de integración; los unitarios cubren dominio, behavior y handlers, y el 409 real se verifica a mano | No hay proyecto de integración y crearlo queda fuera de I9. |
| 13 | El behavior se llama `PersistenceConflictBehavior` y solo decora comandos | Traduce también `23505`, que no es concurrencia optimista; las queries no escriben. |
| 14 | `ChangePasswordHandler` deja de devolver `Auth.SessionChangedConcurrently`; se elimina `AuthenticationErrors.SessionChangedConcurrently` | Mismo HTTP (409) y misma acción del cliente; un solo mecanismo. |
| 15 | `CreateWorkItemHandler` solo abre `Serializable` cuando recibe milestone o componente; sin ellos sigue en Read Committed | Bajo `Serializable`, su `FOR UPDATE` sobre `projects` falla con `40001` cuando otra alta en el mismo proyecto confirma antes (en Read Committed espera y relee el contador). Se acepta el 409 entre altas concurrentes solo cuando una lleva milestone o componente. Detectado en la revisión final. |
| 16 | `40P01` (deadlock) se traduce como `40001` | Postgres revierte la transacción en ambos casos y el cliente puede reintentar; detectado en la revisión final. |

## 5. Enfoques considerados

Todos comparten `xmin` en los roots, tocar el root (supuesto 5) y SSI (supuesto 8). Difieren en dónde se traduce la excepción.

| Enfoque | Ventajas | Inconvenientes |
|---|---|---|
| **A (elegido):** behavior `PersistenceConflictBehavior` en Application | Encaja con los decoradores existentes (tipado, sin reflexión); sigue en el railway de `Result` y `ApiResponses` ya mapea `Conflict` → 409; se prueba con tests unitarios; `LoggingBehavior` registra un warning con el código y no un error con stack trace | Cambia la línea del pipeline en `CLAUDE.md` y la skill `create-feature`; R9 debe partir de esta rama |
| B: `IExceptionHandler` en la API | El cambio más pequeño; no toca el pipeline | La API no tiene proyecto de tests; la excepción atraviesa `LoggingBehavior` como error; sale del railway de `Result` |
| C: `catch` en cada handler | Errores específicos por caso de uso | ~25 `try/catch` duplicados y fáciles de olvidar en handlers nuevos |

Se eligió A porque centraliza la traducción en el sitio donde ya viven las responsabilidades transversales de los comandos, se puede probar y conserva el modelo de errores.

## 6. Diseño

### Domain

- `Project.AddMember` fija `UpdatedOnUtc = joinedOnUtc`.
- Sin errores nuevos en Domain: los de persistencia viven en Application.
- El resto de mutadores relevantes (`WorkItem.*`, `Milestone.ChangeStatus/Update`, `Component.Rename/Retire`, `User.ChangeRole/ChangePassword/Deactivate/Reactivate`, `Project.RemoveMember/Update*`) ya fijan `UpdatedOnUtc`.

### Application

- **`Abstractions/Data/PersistenceErrors.cs`** (`public static class`):
  - `ConcurrencyConflict` = `BaseError.Conflict("Persistence.ConcurrencyConflict", "The resource was modified by another request. Reload it and try again.")`.
  - `UniqueViolation` = `BaseError.Conflict("Persistence.UniqueViolation", "A resource with the same unique value already exists.")`.
  - Constantes `UniqueViolationSqlState = "23505"`, `SerializationFailureSqlState = "40001"` y `DeadlockDetectedSqlState = "40P01"` (supuesto 16).
- **`Abstractions/Behaviors/PersistenceConflictBehavior.cs`** (`internal static class`, como `ValidationBehavior`), con `CommandHandler<TCommand, TResponse>` y `CommandBaseHandler<TCommand>` (`internal sealed`). Envuelve `innerHandler.Handle` en `try/catch`:
  - `DbUpdateConcurrencyException` → `PersistenceErrors.ConcurrencyConflict`;
  - `DbException { SqlState: "40001" o "40P01" }`, directa o como `InnerException` de `DbUpdateException` → `ConcurrencyConflict`;
  - `DbUpdateException` con `InnerException` `DbException { SqlState: "23505" }` → `UniqueViolation`;
  - cualquier otra excepción se propaga (500 en `GlobalExceptionHandler`).
  - Registra `LogWarning` con la excepción (para conservar el nombre de la constraint y el SQL afectado). `DbException` es `System.Data.Common`: Application no depende de Npgsql.
- **Registro** (`Application/DependencyInjection.cs`, `AddBehaviors`): los dos `Decorate` del behavior van **antes** que los de `ValidationBehavior`, para que sea el más interno. Pipeline de comandos: `Logging → Performance → Validation → PersistenceConflict → Handler`.
- **`IApplicationDbContext`**: nueva sobrecarga `Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)`.
- **Handlers con transacción `Serializable`** (abren la transacción antes de la primera lectura, guardan y llaman a `CommitAsync`; un error de dominio sale sin commit y la transacción se descarta con `await using`):
  - `ChangeMilestoneStatusHandler` y `RetireComponentHandler`.
  - `WorkItemFieldUpdateHandler<TCommand>`: nuevo `protected virtual IsolationLevel? TransactionIsolationLevel => null`. Si no es `null`, `Handle` envuelve la carga, `ApplyAsync`, `SaveChangesAsync` y el commit en una transacción con ese nivel. `ChangeWorkItemMilestoneHandler` y `ChangeWorkItemComponentHandler` lo sobrescriben con `IsolationLevel.Serializable`; el resto de actualizaciones de campo no cambia.
  - `CreateWorkItemHandler`: su transacción existente pasa a `Serializable` solo cuando recibe milestone o componente (supuesto 15); conserva el `FOR UPDATE` sobre `projects`, que ahora selecciona `xmin` explícitamente (`SELECT *, xmin`: `SELECT *` no devuelve columnas de sistema y `Project` mapea `xmin`).
  - `ChangeUserRoleHandler`: abre la transacción `Serializable` solo cuando `isDemotingActiveAdministrator`; en el resto de casos sigue sin transacción.
- **`ChangePasswordHandler`**: se quita el `try/catch`; se elimina `AuthenticationErrors.SessionChangedConcurrently`.
- `RefreshTokenHandler` y `LogoutHandler` no cambian.

### Infrastructure

- **`Database/ConcurrencyTokenExtensions.cs`** (`internal static`): `HasXminConcurrencyToken<TEntity>(this EntityTypeBuilder<TEntity> builder)` mapea la shadow property `uint Version` con `IsRowVersion()` y `HasColumnName("xmin")` (explícito por `EFCore.NamingConventions`).
- `ProjectConfiguration`, `MilestoneConfiguration`, `ComponentConfiguration`, `WorkItemConfiguration` y `UserConfiguration` llaman a `builder.HasXminConcurrencyToken()`. `UserTokenConfiguration` sustituye su mapping por la extensión (mismo modelo).
- `ApplicationDbContext` implementa la sobrecarga con `Database.BeginTransactionAsync(isolationLevel, cancellationToken)`.

### API

- Sin middleware nuevo; `GlobalExceptionHandler` y `ApiResponses` no cambian.
- Los endpoints de comandos que aún no lo declaran añaden `.Produces<ProblemDetails>(StatusCodes.Status409Conflict)`.

## 7. Errores y códigos HTTP

| Caso | Error | HTTP |
|---|---|---|
| Otra petición modificó la fila entre la lectura y la escritura (`xmin`) | `Persistence.ConcurrencyConflict` | 409 |
| Postgres aborta una transacción serializable (`40001`) o detecta un deadlock (`40P01`) | `Persistence.ConcurrencyConflict` | 409 |
| Índice único violado por una carrera (`23505`) | `Persistence.UniqueViolation` | 409 |
| Cambio de contraseña concurrente con un refresh (antes `Auth.SessionChangedConcurrently`) | `Persistence.ConcurrencyConflict` | 409 |
| Refresh concurrente con el mismo token (sin cambios) | `Auth.InvalidRefreshToken` | 401 |
| Logout concurrente (sin cambios) | — | 200 |
| Cualquier otra excepción de base de datos | — | 500 |

## 8. Datos y migraciones

- Migración `AddConcurrencyTokens`: EF genera un `AddColumn<uint>("xmin")` por `projects`, `milestones`, `components`, `work_items` y `users`, y actualiza el snapshot. Npgsql no emite SQL para `xmin` (columna de sistema), así que `dotnet ef migrations script` no debe mostrar ningún `ALTER TABLE`; se comprueba antes de commitear.
- Sin cambios en datos existentes. `Down` elimina las propiedades del snapshot, tampoco emite SQL.
- Tras la migración, `dotnet ef migrations has-pending-model-changes` debe salir limpio.

## 9. Tests

- **Domain:** `ProjectTests` — `AddMember` fija `UpdatedOnUtc`.
- **Application:**
  - `PersistenceConflictBehaviorTests` (ambas variantes): `DbUpdateConcurrencyException` → `ConcurrencyConflict`; `40001` y `40P01` directas y envueltas en `DbUpdateException` → `ConcurrencyConflict`; `23505` envuelta → `UniqueViolation`; otra excepción (incluidas una `DbUpdateException` con otro SqlState y una `OperationCanceledException`) → se propaga; resultado del handler (éxito o fallo) → se devuelve sin tocar.
  - `ChangeMilestoneStatusHandlerTests`, `RetireComponentHandlerTests`, `ChangeWorkItemMilestoneHandlerTests`, `ChangeWorkItemComponentHandlerTests`: con éxito se pide `BeginTransactionAsync(IsolationLevel.Serializable, …)` y se llama a `CommitAsync`; con un error de dominio no hay commit.
  - `ChangeUserRoleHandlerTests`: al degradar a un Administrator activo se abre la transacción `Serializable` y se hace commit; en los demás casos no se abre transacción.
  - `ChangePasswordHandlerTests`: el test de `SessionChangedConcurrently` pasa a comprobar que la `DbUpdateConcurrencyException` se propaga.
- **ArchitectureTests:** se ejecutan tras añadir el behavior y la extensión (sellado, visibilidad, namespaces).
- **Verificación manual** (Postgres vía Aspire, `psql` como segunda sesión):
  1. `xmin` → 409: en `psql`, `BEGIN; SELECT * FROM flowboard.projects WHERE id = '<id>' FOR UPDATE;`. Lanzar `DELETE /api/v1/flowboard/projects/<id>/members/<userId>` (queda bloqueado en el `UPDATE`). En `psql`, `UPDATE flowboard.projects SET updated_on_utc = now() WHERE id = '<id>'; COMMIT;`. Esperado: 409 `Persistence.ConcurrencyConflict`.
  2. `23505` → 409: en `psql`, `BEGIN;` + `INSERT` de un usuario con email X, sin commit. Lanzar `POST /api/v1/flowboard/users` con el email X (la comprobación previa no ve la fila; el `INSERT` se bloquea en el índice). `COMMIT;` en `psql`. Esperado: 409 `Persistence.UniqueViolation`.
  3. `40001` → 409: en `psql`, `BEGIN ISOLATION LEVEL SERIALIZABLE;`, leer el milestone (`SELECT status FROM flowboard.milestones WHERE id = '<m>'`) y asignarle un work item abierto (`UPDATE flowboard.work_items SET milestone_id = '<m>' WHERE id = '<w>'`), sin commit. Cerrar el milestone por la API y, mientras, `COMMIT;` en `psql`. Esperado: una de las dos transacciones aborta con `40001`; si es la de la API, responde 409 `Persistence.ConcurrencyConflict`; si es la de `psql`, el milestone queda cerrado y el work item sin asignar.
  4. Alta de work items (regresión de `SELECT *, xmin` y del nivel de aislamiento): `POST /api/v1/flowboard/work-items` sin milestone ni componente y otro con milestone responden 201 con códigos consecutivos.

## 10. Riesgos y preguntas abiertas

- **Falsos positivos de SSI:** Postgres puede abortar transacciones serializables sin conflicto real (bloqueos de predicado sobre rangos de índice). Solo afecta a los cinco handlers con `Serializable` y se traduce a 409; aceptable para el volumen de una API interna.
- **SSI solo protege entre transacciones serializables:** cualquier handler nuevo que lea el estado de un milestone o componente para decidir, o que degrade a un Administrator, debe abrir también una transacción `Serializable`. Se documenta en la rule nueva.
- **R9 (refactor de behaviors):** si se hace después, debe incluir `PersistenceConflictBehavior`.
- **Contrato de error de change password:** los clientes que miraran `Auth.SessionChangedConcurrently` verán `Persistence.ConcurrencyConflict` (mismo 409).

## 11. Documentación

- Rule nueva `.claude/rules/concurrency.md` (`paths`: `src/Aurora.Flowboard.Application/**/*.cs`, `src/Aurora.Flowboard.Infrastructure/Configurations/**/*.cs`, `src/Aurora.Flowboard.Infrastructure/Database/**/*.cs`): qué roots llevan `xmin` y cómo añadirlo a uno nuevo; tocar el root cuando un cambio en hijos afecta a una invariante; cuándo abrir `Serializable`; qué traduce el behavior; por qué refresh y logout conservan su `catch`.
- `.claude/rules/authentication.md`: la viñeta de change password pasa a remitir al behavior.
- `CLAUDE.md`: línea del pipeline y fila de `concurrency.md` en la tabla de rules.
- `.claude/skills/create-feature/SKILL.md`: sección del pipeline.
- `.claude/skills/domain-entity/references/ef-configuration.md`: el ejemplo de concurrencia (`byte[] row_version`, no válido en Postgres) pasa a `HasXminConcurrencyToken()`.
