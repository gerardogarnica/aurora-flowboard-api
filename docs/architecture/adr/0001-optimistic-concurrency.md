# 0001 — Concurrencia optimista con `xmin`, SSI y traducción en el pipeline

**Fecha:** 2026-10-16
**Estado:** Aprobado
**Tickets:** I9
**Rama:** `fix/optimistic-concurrency` · **PR:** —
**Relacionado:** `docs/specs/2026-10-16-optimistic-concurrency-design.md`

## Contexto

Los handlers de comandos cargan un agregado, deciden con su estado y guardan con `SaveChangesAsync`, sin repositorio y sin bloqueos (salvo el `FOR UPDATE` del contador de work items). Hay tres clases de carrera:

- **Lost update sobre el mismo agregado:** dos peticiones leen la misma fila y la segunda escritura pisa la primera (dos admins que se quitan mutuamente dejan el proyecto sin admin).
- **Write skew entre agregados o filas:** cada petición lee una fila y escribe otra (cerrar un milestone frente a asignarle un work item; degradar a dos Administrators distintos a la vez). Un concurrency token sobre la fila escrita no lo detecta.
- **Unicidad con consultar-y-luego-insertar:** el índice único frena el duplicado, pero con un `23505`.

Todas acababan en un 500 de `GlobalExceptionHandler`. Restricciones: los handlers se prueban con NSubstitute sobre `IApplicationDbContext` (`FromSqlRaw` no funciona sobre el `DbSet` simulado), Application no depende de Npgsql y los errores esperados viajan como `Result`.

## Decisión

1. Cada aggregate root editable por HTTP mapea `xmin` como concurrency token (shadow `uint Version`, `IsRowVersion()`, `HasColumnName("xmin")`) con `HasXminConcurrencyToken()`. Un método de dominio que cambia hijos de los que depende una invariante del root también escribe el root (`UpdatedOnUtc`).
2. El write skew se resuelve con transacciones `Serializable` (SSI de Postgres) en los handlers que leen una fila para decidir y escriben otra, mediante `IApplicationDbContext.BeginTransactionAsync(IsolationLevel, …)`.
3. `PersistenceConflictBehavior`, el decorador más interno de los comandos, traduce `DbUpdateConcurrencyException`, `40001` y `40P01` (deadlock) a `Persistence.ConcurrencyConflict`, y `23505` a `Persistence.UniqueViolation`, ambos `Conflict` → 409. Un handler solo captura la excepción él mismo cuando la semántica no es un 409 (refresh → 401, logout → 200).

## Alternativas descartadas

| Alternativa | Por qué se descartó |
|---|---|
| Bloqueo pesimista `FOR UPDATE` / `FOR SHARE` con `FromSqlRaw` en los handlers | Los handlers afectados perderían sus tests unitarios (`FromSqlRaw` no se puede simular), y obliga a que cada lado de la carrera recuerde el bloqueo correcto. |
| Método de bloqueo en `IApplicationDbContext` (`LockRowAsync`) | Simulable, pero añade una abstracción nueva con nombres de tabla en Application; SSI resuelve lo mismo con una sobrecarga estándar. |
| Columna `row_version` propia (`byte[]` / contador) | Postgres ya mantiene `xmin`; una columna propia necesita migración con datos y mantenimiento en cada escritura. |
| `IExceptionHandler` en la API para el 409 | Sin proyecto de tests en la API, la excepción atraviesa `LoggingBehavior` como error y sale del railway de `Result`. |
| `catch` en cada handler | ~25 bloques duplicados y fáciles de olvidar en handlers nuevos. |
| Reintento automático en el servidor | Puede reaplicar una decisión tomada sobre datos viejos; el cliente decide si reintentar. |
| ETag / `If-Match` | Protege otra ventana (entre peticiones del cliente); queda para un spec propio. |

## Consecuencias

- **Positivas:** las carreras devuelven 409 en vez de 500 o de corromper invariantes; un solo punto de traducción, probado; los handlers no cambian salvo los que necesitan `Serializable`; sin dependencia de Npgsql en Application; la migración no emite SQL.
- **Negativas / costes:** SSI puede abortar transacciones sin conflicto real (falsos positivos → 409); solo protege si **todas** las transacciones implicadas son `Serializable`; un `CreateWorkItem` concurrente provoca un 409 en una edición del proyecto que coincida en el tiempo; un `CreateWorkItem` con milestone o componente (el único `Serializable`) puede dar 409 si coincide con otro alta en el mismo proyecto, porque su `FOR UPDATE` sobre `projects` falla con `40001` en vez de esperar; R9 (refactor de behaviors) debe incluir el behavior nuevo.
- **Qué hay que hacer a partir de ahora:**
  - Un aggregate root nuevo editable por HTTP llama a `HasXminConcurrencyToken()` en su configuración (skill `domain-entity`, `references/ef-configuration.md`).
  - Un handler que lee el estado de otra fila u otro agregado para decidir (estado de milestone o componente, "otro Administrator activo") abre una transacción `Serializable`, igual que el handler del otro lado de la carrera.
  - No se añade `catch (DbUpdateConcurrencyException)` en handlers salvo que la respuesta no sea un 409.
  - Las reglas detalladas viven en `.claude/rules/concurrency.md`; el pipeline está en `CLAUDE.md` y en la skill `create-feature`.
