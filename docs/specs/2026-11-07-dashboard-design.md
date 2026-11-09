# Dashboard y búsqueda de work items — Diseño

**Fecha:** 2026-11-07
**Estado:** Aprobado
**Tickets:** —
**Rama:** `feature/dashboard` · **PR:** —
**Plan:** `docs/plans/2026-11-07-dashboard-plan.md`

## 1. Contexto y problema

El frontend necesita construir el Home y My Issues, cuya spec está aprobada en `aurora-flowboard-web/docs/specs/dashboard.spec.md` (v1.1; contrato en la sección 9). Hoy la API no puede alimentarlos:

- **No se sabe cuándo entró un item a su estado.** `WorkItem` (`src/Aurora.Flowboard.Domain/WorkItems/WorkItem.cs`) no guarda ese dato. Solo se puede deducir del historial, y `Create` no escribe historial.
- **No existe una lista de work items entre proyectos.** Solo está el board por proyecto (`GET projects/{projectId:guid}/board`).
- **No existe el concepto de etapa.** `Project.GetInitialFlowState()` dice dónde nace un item, pero nada distingue backlog de trabajo en vuelo.
- **Un proyecto puede completarse con items abiertos.** El PRD (§5.1) lo prohíbe, pero `Project.ChangeStatus` (`src/Aurora.Flowboard.Domain/Projects/Project.cs:233`) no lo comprueba. En `Completed`, `IsModifiable` es falso y los items no se pueden mover, y desde ahí solo se puede pasar a `Archived` (`Project.cs:25`). Sin una decisión, esos items saldrían como estancados para siempre.

## 2. Objetivo y criterios de éxito

Que la API calcule, con una sola definición, los datos del Home y de My Issues.

- `me.open` coincide siempre con `counts.myOpenIssues` de `my-summary`.
- El portafolio lista los mismos proyectos que el Sidebar y en el mismo orden.
- La suma de los buckets 7-13 y 14+ es igual a `team.stalled`.
- Un item en backlog nunca cuenta como estancado.
- Un item de un proyecto `Completed` no cuenta en ninguna métrica de items abiertos ni aparece en la búsqueda.
- El coste de `/dashboard` no crece con una fila en memoria por cada item abierto.

### Definiciones

| Concepto | Definición |
|---|---|
| **Mis proyectos** | Soy miembro y `Status != Archived`. Orden del Sidebar: estado y luego nombre |
| **Mis proyectos activos** | Soy miembro y `Status` es `Active` o `Maintenance` |
| **Abierto** | `FlowState.Category == Active` |
| **Backlog** | El estado `Active` de menor `SortOrder`. Excepción: si el proyecto tiene un solo estado `Active`, ese estado es en vuelo y el proyecto no tiene backlog |
| **En vuelo** | Abierto y no backlog |
| **Días en el estado** | Bloques de 24 horas completos desde `EnteredCurrentStateOnUtc`, no días de calendario |
| **Estancado** | En vuelo y 7 días o más en el estado |
| **`closedLast7Days`** | `Category == Completed` y `CompletedOnUtc >= now - 7d`. Se filtra por categoría porque `Move` llena `CompletedOnUtc` también al cancelar y no lo limpia al reabrir (`WorkItem.cs:512`) |
| **Unassigned** | Abierto, backlog incluido, y sin asignado |
| **Critical & high** | Prioridad `High` o `Critical` y abierto, backlog incluido |
| **`linkedTotal`** | Vinculados al milestone y no cancelados |
| **`linkedClosed`** | Vinculados en `Completed`, con cualquier fecha |
| **`linkedStalled`** | Vinculados y estancados |

## 3. Alcance

**Dentro:**
- `WorkItem.EnteredCurrentStateOnUtc`, con migración y backfill, expuesto en detalle, board y búsqueda.
- `stage` en las columnas del board y en los items de la búsqueda.
- `GET work-items`: búsqueda entre proyectos, paginada.
- `GET dashboard`.
- `my-summary`: `counts.myOpenIssues` pasa a usar mis proyectos activos. La forma de la respuesta no cambia.

**Fuera de alcance:**
- Regla del PRD §5.1 que impide completar o archivar un proyecto con items abiertos, y limpieza de los datos ya afectados: ticket aparte (ver sección 10).
- Limpiar `CompletedOnUtc` al reabrir un item: no se hará aquí, porque `closedLast7Days` lo evita filtrando por categoría.
- Caché (Redis u otra): posible mejora futura si el coste del `GROUP BY` lo justifica.
- Zonas horarias por usuario y umbrales configurables: no se harán en esta feature.
- `reportedByMe` en `/dashboard` y exponer los umbrales 4 y 7: decidido que no, según la spec del frontend.
- Cambios en `OpenWorkItems` de `GET projects` y `GET projects/{id}`: no se harán, porque ahí el dato describe al proyecto.

## 4. Supuestos y decisiones

| # | Supuesto o decisión | Motivo |
|---|---|---|
| 1 | Hay dos conjuntos: **mis proyectos** para Sidebar, filas del portafolio, `counts.projects` y `counts.members`; **mis proyectos activos** para métricas de items abiertos, búsqueda y milestones. `counts.myOpenIssues` usa los activos. Un proyecto `Completed` conserva su fila con ceros y `activeMilestone = null`. `closedLast7Days` sigue incluyendo los proyectos `Completed` | Los items de esos proyectos no se pueden mover. El dashboard solo muestra trabajo sobre el que se puede actuar, y el cierre sí ocurrió (decisión del usuario, opción C) |
| 2 | El `activeMilestone` "más próximo" es el de menor `TargetEndDate`: gana el vencido más antiguo. Si ninguno tiene fecha, el de `CreatedOnUtc` más reciente | Coincide con el orden de la lista de milestones |
| 3 | Desempates: por `Name` y luego `Id` en milestones y `activeMilestone` con fecha; por `Id` en la búsqueda y en `activeMilestone` sin fecha | Orden determinista y paginación estable |
| 4 | `members` incluye a todos los miembros del proyecto, también `Viewer` e inactivos, ordenados por nombre completo | Refleja la membresía real |
| 5 | Un usuario sin proyectos recibe 200 con ceros, listas vacías y los 4 buckets | El frontend decide su estado vacío a partir de la respuesta |
| 6 | Los filtros de la búsqueda se combinan con AND. `stage=Backlog&stalled=true` devuelve 200 con una página vacía | Es una combinación válida sin resultados |
| 7 | `createdBy=me` incluye items de cualquier asignado y sin asignar, pero solo abiertos y de mis proyectos activos | Pestaña Reported by me |
| 8 | `linkedTotal`, `linkedClosed` y `linkedStalled` cuentan items de cualquier asignado | Miden el milestone, no al usuario |
| 9 | En el detalle de un item cerrado, `enteredCurrentStateOnUtc` es la fecha en que entró al estado terminal. El detalle no lleva `stage` | El campo mantiene un solo significado, y el contrato solo pide `stage` en board y búsqueda |
| 10 | `/dashboard` no devuelve `reportedByMe` | Confirmado por el usuario |
| 11 | Los umbrales 4 y 7 no se exponen. El 4 solo existe como límite de bucket en el backend | Confirmado por el usuario |
| 12 | Aging y `oldestInFlight` cubren todo el trabajo en vuelo de mis proyectos activos, no solo el mío | Confirmado por el usuario; el conjunto sigue el supuesto 1 |
| 13 | La etapa no se persiste. Los handlers cargan los estados `Active`, resuelven cuáles son backlog con una regla pura del Domain y filtran por esos ids en SQL | Pocos estados por proyecto, y la regla vive en un solo sitio |
| 14 | Estancado se calcula en SQL como `EnteredCurrentStateOnUtc <= now - 7d`, con un solo `now` de `IDateTimeProvider` por petición. Los buckets usan el mismo método con 4, 7 y 14 días | Equivale a bloques completos de 24 horas y no necesita aritmética de días en SQL |
| 15 | `entered_current_state_on_utc` es `NOT NULL`. La migración la añade nullable, hace el backfill y luego aplica `NOT NULL`. `Down` la elimina | El dato se puede volver a calcular desde el historial |
| 16 | Se añade solo el índice `(flow_state_id, entered_current_state_on_utc)`. No se indexan `assignee_id` ni `created_by_id` | El filtro por estado ya acota al alcance del usuario |
| 17 | Los filtros se reciben como `string` y los valida `GetWorkItemsValidator`. `assignee` y `createdBy` solo admiten `me`; `stalled` solo `true`; `sort` solo `stageThenTimeInState`, que es el valor por defecto | Todo valor inválido responde con el mismo 400 |
| 18 | `priority` y `stage` distinguen mayúsculas (`High`, `InFlight`). Una prioridad desconocida en la lista da 400 | Son los valores que serializa la API |
| 19 | `pageSize` usa 20 por defecto y máximo 100. La respuesta es `PagedResponse<T>`, con `items`, `page`, `pageSize`, `totalCount` y `totalPages` | Es el envoltorio común de `ef-core-queries.md` |
| 20 | `GetDashboardQuery` no lleva parámetros y usa `IUserContext` | Los tests de arquitectura ya excluyen a estas consultas de tener validador |
| 21 | No hay errores de dominio nuevos: `dashboard` responde 200 y 401; `work-items`, además, 400 | Son solo lecturas sobre el alcance del usuario |
| 22 | No hay caché ni transacción de lectura | El frontend lleva la frescura con `staleTime` de 60 segundos |
| 23 | Los tests unitarios no prueban la traducción a SQL. Se verifica a mano contra Postgres | `MockDbSetHelper` corre sobre LINQ-to-Objects |
| 24 | No se limpia `CompletedOnUtc` al reabrir un item | El filtro por categoría lo evita y el defecto queda fuera de esta feature |
| 25 | Zonas horarias por usuario y umbrales configurables quedan fuera | No los pide el objetivo |

## 5. Enfoques considerados

| Enfoque | Ventajas | Inconvenientes |
|---|---|---|
| **A (elegido): consulta agregada** con un `GROUP BY` sobre los items abiertos, más consultas de referencia | Todos los contadores salen de la misma foto y cuadran entre sí. El coste depende de las combinaciones, no del número de items | La consulta es más compleja, y su traducción a SQL solo se comprueba contra Postgres |
| B: una consulta por métrica | Es el código más simple de leer y encaja con `GetMySummaryHandler` | Más de 15 consultas por carga, y los números pueden no cuadrar si algo cambia entre ellas |
| C: cargar los items en memoria | Es la lógica más fácil de probar, también con una sola foto | Contradice `ef-core-queries.md` ("counts are projected"): el coste crece con los items |

Se eligió A porque es la única que garantiza que los números cuadren sin cargar los items en memoria.

## 6. Diseño

### Domain

**`WorkItem`**
- Propiedad `DateTime EnteredCurrentStateOnUtc { get; private set; }`.
- `Create` la fija a `createdOnUtc`. `Move` la fija a `changedOnUtc`, junto a `FlowStateId`. Ningún otro flujo la escribe.
- Constante `public const int StalledThresholdDays = 7`.
- Sin eventos ni errores nuevos.

**Etapa, en `Domain/Projects/`**
- Enum `FlowStateStage { Backlog, InFlight }`, serializado como string por el `JsonStringEnumConverter` global.
- Método estático `FlowState.GetBacklogStateId(activeStates)`: recibe `(Id, SortOrder)` de los estados `Active` de un proyecto y devuelve el de menor `SortOrder`, o `null` si hay uno solo o ninguno. Es un método de `FlowState` y no una clase estática aparte, porque `Errors_Should_BeStaticAndHaveErrorsSuffix` exige que toda clase estática del Domain termine en `Errors`. En Application, `Projects/Shared/FlowStateStageResolver` lo aplica a varios proyectos a la vez.

`Project.GetInitialFlowState()` no cambia: responde dónde nace un item. En un proyecto con un solo estado `Active`, el item nace en ese estado y su etapa es `InFlight`. `Project` y `Milestone` tampoco cambian: el filtro de proyectos activos va en Application, porque `IsModifiable` no se puede traducir a SQL.

### Application

**Compartido: `Projects/Shared/MyProjectsQueryExtensions.cs`**
- `WhereMyProjects(userId)` y `WhereMyActiveProjects(userId)`.
- El orden del Sidebar, aplicado en memoria: estado y luego nombre con `StringComparer.OrdinalIgnoreCase`.
- `GetMySummaryHandler` los adopta. `myOpenIssues` usa los activos, y lo demás no cambia.

**Respuestas existentes**
- `BoardColumnResponse` añade `FlowStateStage Stage`, calculado con `FlowStateStages` en `GetProjectBoardHandler`.
- `BoardWorkItemResponse` y `WorkItemResponse` añaden `DateTime EnteredCurrentStateOnUtc`.

**`WorkItems/GetWorkItems/`**: `GetWorkItemsQuery` (`IPagedQuery`), `GetWorkItemsHandler`, `GetWorkItemsValidator`, `WorkItemListItemResponse`.

1. Carga los estados `Active` de mis proyectos activos y resuelve cuáles son backlog y cuáles en vuelo, proyecto por proyecto.
2. Filtra con `stateIds.Contains(w.FlowStateId)`, que por sí solo fija el alcance: mis proyectos activos y solo abiertos.
3. Aplica los filtros con AND:
   - `assignee=me`: `AssigneeId == userId`.
   - `createdBy=me`: `CreatedById == userId`.
   - `stage`: estados de esa etapa.
   - `stalled=true`: estados en vuelo y `EnteredCurrentStateOnUtc <= now - 7d`.
   - `priority`: `priorities.Contains(w.Priority)`.
4. Ordena: en vuelo primero, luego `EnteredCurrentStateOnUtc` ascendente, luego `Id`. Pagina con `ToPagedResponseAsync`.
5. Resuelve nombres con subconsultas correlacionadas y termina con `PagedResponse.Map` para `stage`, `AssigneeInitials` y `AssigneeFullName`, ambos `null` en un item sin asignar.

**`Dashboard/GetDashboard/`**: `GetDashboardQuery`, `GetDashboardHandler`, `DashboardResponse` y sus registros `Dashboard*Response`.

| # | Consulta | Para qué |
|---|---|---|
| 1 | Mis proyectos | Filas del portafolio. Sin proyectos, devuelve aquí la respuesta vacía |
| 2 | Estados `Active` de esos proyectos | `distribution` y etapas |
| 3 | `GROUP BY` de abiertos en proyectos activos, por `FlowStateId`, `MilestoneId`, mío, sin asignar, prioridad alta y tramo de antigüedad | KPIs, Aging, `open`, `stalled`, `distribution`, `linkedStalled` |
| 4 | Milestones `Active` de proyectos activos | `milestones` y `activeMilestone` |
| 5 | Items `Completed` por milestone | `linkedClosed`. `linkedTotal` es esto más los abiertos de la consulta 3 |
| 6 | Conteo de `closedLast7Days` en mis proyectos | `team.closedLast7Days` |
| 7 | Item en vuelo más antiguo, por `EnteredCurrentStateOnUtc` y luego `Id` | `oldestInFlight` |
| 8 | Miembros, proyectando nombres y no `User` | `members` |

- `DashboardCalculator` (interna) arma la respuesta a partir de estas filas, como función pura. El handler solo orquesta.
- Los tramos se calculan en SQL con un ternario anidado, porque los árboles de expresión no admiten `switch` (CS8514). Los límites 4 y 14 son constantes de `DashboardCalculator`, y el 7 es `WorkItem.StalledThresholdDays`.
- Reglas de salida:
  - `me.*`: asignados a mí en mis proyectos activos.
  - `team.stalled`, `team.unassigned` y `aging`: mis proyectos activos, de cualquier asignado.
  - `aging`: solo items en vuelo, siempre con los 4 buckets (0-3, 4-6, 7-13 y 14+, con `to = null` en el último).
  - `milestones`: primero con fecha, ascendente; luego sin fecha, por `linkedStalled` descendente; después los desempates del supuesto 3.
  - `portfolio`: mismo conjunto y orden que el Sidebar. `distribution` solo con estados `Active`, en `SortOrder`, incluidos los de `count = 0`.

**Límite de consistencia:** los contadores de la consulta 3 siempre cuadran entre sí, pero las consultas 5, 6 y 7 son lecturas separadas. Un movimiento concurrente puede desalinearlas durante una carga, y se corrige en la siguiente.

### Infrastructure

`WorkItemConfiguration`:
- `builder.Property(x => x.EnteredCurrentStateOnUtc).IsRequired()`.
- Índice sobre `(FlowStateId, EnteredCurrentStateOnUtc)`, que sustituye a `ix_work_items_flow_state_id` porque empieza por la misma columna. La migración generada debe confirmar que EF lo elimina.

No hay `DbSet` nuevos ni cambios en `xmin`: `WorkItem` ya lo tiene y `Move` ya escribe esa fila.

### API

**`Endpoints/WorkItems/GetWorkItems.cs`: `GET work-items`**
- Parámetros opcionales `string?`: `assignee`, `createdBy`, `stage`, `stalled`, `priority`, `sort`. Además, `page` y `pageSize`, con `PaginationDefaults`.
- `.Produces<PagedResponse<WorkItemListItemResponse>>(200)` y `.ProducesStandardProblems(400)`.

Forma de cada item:

```ts
{
  workItemId: string; code: string; title: string; type: WorkItemType; priority: Priority
  projectId: string; projectName: string; projectColor: string
  flowStateId: string; flowStateName: string; stage: 'Backlog' | 'InFlight'
  enteredCurrentStateOnUtc: string
  assigneeId: string | null; assigneeInitials: string | null; assigneeFullName: string | null
  milestoneName: string | null; milestoneColor: string | null
}
```

**`Endpoints/Dashboard/GetDashboard.cs`: `GET dashboard`**
- Sin parámetros. `.Produces<DashboardResponse>(200)` y `.ProducesStandardProblems()`. Nueva constante `EndpointTags.Dashboard`.

```ts
{
  me: { open: number; stalled: number; inFlight: number; criticalHigh: number }
  team: { stalled: number; unassigned: number; closedLast7Days: number }
  aging: {
    buckets: { from: number; to: number | null; count: number }[]
    oldestInFlight: {
      code: string; title: string; projectId: string; projectName: string
      enteredCurrentStateOnUtc: string
    } | null
  }
  milestones: {
    id: string; projectId: string; projectName: string; name: string; color: string
    targetEndDate: string | null; linkedTotal: number; linkedClosed: number; linkedStalled: number
  }[]
  portfolio: {
    projectId: string; name: string; color: string; kind: ProjectKind; status: ProjectStatus
    open: number; stalled: number
    distribution: { flowStateId: string; name: string; color: string; count: number }[]
    activeMilestone: {
      id: string; name: string; color: string; targetEndDate: string | null
      linkedTotal: number; linkedClosed: number
    } | null
    members: { userId: string; fullName: string; initials: string }[]
  }[]
}
```

**Ambos endpoints**
- Usan `.RequireAuthorization()` sin política de rol: el rol `Administrator` no da visibilidad, solo la membresía.
- La cadena fluida va dentro de `MapEndpoint`, porque `ApiLayerTests` inspecciona ahí su IL.
- Se añaden las peticiones a `Aurora.Flowboard.Api.http`.

**Documentación al implementar**
- `.claude/rules/work-items.md`: `GET work-items` es la búsqueda entre proyectos, y sigue prohibida una lista por proyecto bajo esa ruta. También añade las definiciones de etapa y estancado.
- `.claude/rules/dashboard.md` (nueva): los dos conjuntos de proyectos, la consulta agregada y su límite de consistencia.

## 7. Errores y códigos HTTP

| Caso | Error | HTTP |
|---|---|---|
| Valor inválido en un filtro, `sort`, `page` o `pageSize` de `GET work-items` | Validación de `GetWorkItemsValidator` | 400 |
| Sin autenticar, en ambos endpoints | — | 401 |
| Usuario sin proyectos | Ninguno: respuesta vacía | 200 |
| Página más allá del final | Ninguno: `items` vacío y `totalCount` real | 200 |

## 8. Datos y migraciones

**Migración `AddWorkItemEnteredCurrentState`**, editada a mano porque EF generaría la columna `NOT NULL` con un valor por defecto artificial:

1. Añade `entered_current_state_on_utc` como nullable.
2. Hace el backfill:
   ```sql
   UPDATE flowboard.work_items w
   SET entered_current_state_on_utc = COALESCE(
       (SELECT MAX(h.changed_on_utc)
        FROM flowboard.work_item_state_transition_histories h
        WHERE h.work_item_id = w.id),
       w.created_on_utc);
   ```
3. La pasa a `NOT NULL`, sin valor por defecto.
4. Sustituye el índice de `flow_state_id` por el compuesto.

**`Down`:** restaura el índice anterior y elimina la columna. No se pierde información, porque el dato se puede volver a calcular desde el historial.

**Despliegue:** las migraciones se aplican al arrancar. El cambio de esquema es aditivo y el frontend tolera que falte el campo (AC-18 de su spec). El `UPDATE` reescribe todas las filas y cambia su `xmin`, así que una edición en curso durante el arranque podría recibir un 409.

## 9. Tests

- **Domain:**
  - `Create` fija la fecha a `createdOnUtc`, y `Move` la actualiza, también al volver a un estado `Active` anterior. El flujo no genera transiciones que salgan de un estado de cierre, así que la reapertura solo puede ocurrir por SQL.
  - Un `Move` rechazado no la cambia.
  - `GetBacklogStateId`: sin estados, con uno solo, con varios y con la entrada desordenada.
- **Application:**
  - `GetWorkItemsHandler`: alcance (excluye proyectos `Completed`, `Archived` y ajenos, además de items cerrados), cada filtro y sus combinaciones, orden por etapa y antigüedad, y paginación a través de un límite de página.
  - `GetWorkItemsValidator`: cada valor inválido.
  - `DashboardCalculator`: backlog nunca estancado, excepción de estado único, buckets 7-13 y 14+ iguales a `team.stalled`, orden de milestones, elección de `activeMilestone` y proyecto `Completed` con ceros.
  - `GetDashboardHandler`: límites exactos de los tramos (3d23h, 4d, 6d23h, 7d, 13d23h y 14d), que se prueban aquí porque el tramo se calcula en la expresión de la consulta; `me.open` coincide con `myOpenIssues` de `GetMySummaryHandler` sobre los mismos datos; `closedLast7Days` excluye cancelados y reabiertos con `CompletedOnUtc` reciente, e incluye proyectos `Completed`; un usuario sin proyectos recibe la respuesta vacía.
  - `GetMySummaryHandler`: no cuenta items de proyectos `Completed`, pero sigue listando el proyecto.
  - `GetProjectBoardHandler` y `GetWorkItemByCodeHandler`: `stage` y `EnteredCurrentStateOnUtc`.
- **Arquitectura:** sin tests nuevos, pero se ejecutan porque hay tipos nuevos en varias capas.
- **Verificación manual (contra Postgres):**
  - Backfill con y sin historial, y `Up` y `Down` de la migración.
  - SQL real del `GROUP BY` con el ternario y de `Contains` sobre `Priority`, que se guarda como string.
  - Las peticiones del `.http`.

## 10. Riesgos y preguntas abiertas

- **La regla del PRD §5.1 sigue sin implementarse.** Se pueden seguir completando proyectos con items abiertos, que dejarán de verse en el dashboard y en My Issues. Hay que abrir un ticket aparte. Requiere `Serializable` en `ChangeProjectStatusHandler`, revisar los handlers que crean o reabren items (`concurrency.md`) y decidir qué hacer con los datos afectados. Con esa regla y sin datos antiguos, el segundo conjunto de proyectos dejaría de ser necesario.
- **Cifras distintas entre pantallas.** `GET projects` puede mostrar items abiertos en un proyecto `Completed` cuya fila del portafolio muestra 0.
- **Board de un proyecto `Completed`.** Sus cards siguen visibles y pueden salir en rojo por antigüedad, aunque no se puedan mover. Se acepta.
- **Item asignado en un proyecto `Completed`.** Deja de aparecer en My Issues, y solo se llega a él por el board o por su código.
- **Traducción a SQL.** La consulta agregada solo se valida contra Postgres, no en los tests unitarios.
- **Rendimiento.** Si el coste del `GROUP BY` crece, se evaluará una caché, que debe respetar las reglas de presupuesto de tiempo y protección contra estampidas de `CLAUDE.md`.
