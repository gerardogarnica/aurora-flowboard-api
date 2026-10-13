# Flowboard Backend — Gap Analysis vs. PRD

**Fecha:** 2026-08-31
**Revisado:** 2026-09-05 — #2 y #7 marcados resueltos, #8 y #10 marcados parciales.
**Revisado:** 2026-09-22 — re-verificados los 17 puntos contra el código (`staging` @ `2214619`). #12 resuelto; #11 parcial; #3, #4, #5, #9 y #14 con avances relacionados pero el gap sigue abierto; nuevo #18 (el rol `Viewer` no es read-only).
**Revisado:** 2026-09-30 — #18 resuelto (`faa42d1`); #14 resuelto por decisión de producto (`Viewer` puede comentar).
**Comparado contra:** `docs/product/flowboard-prd.md` (v1.0)

> Este documento compara el estado actual del código (`src/`) contra los requisitos del PRD, y lista funcionalidades pendientes o deseables clasificadas por prioridad.

## Estado actual (resumen)

Implementado sólidamente: Projects (CRUD, kind/status, flow fijado en la creación, membresía), Milestones (con color opcional desde #21), Components, WorkItems (CRUD, move, assign, comments, tags, time logging, colecciones de actividad paginadas), Users/Auth (login, refresh, logout, change password, create user, cambio de rol global, `users/me`, `users/my-summary`). Seeding al arranque del `Administrator` inicial y de los `TemplateFlow`. `TemplateFlow` tiene dominio, EF, migración, seeding y query de lectura; le faltan los comandos de escritura (ver #10).

| # | Punto | Prioridad | Estado |
|---|-------|-----------|--------|
| 1 | Desactivar usuarios | 🔴 | Pendiente |
| 2 | Scoping por membresía en lecturas | 🔴 | ✅ Resuelto |
| 3 | Auditoría de cambios de rol global | 🔴 | Pendiente (agravado: el cambio de rol ya está expuesto) |
| 4 | `GET /me/board` | 🟠 | Pendiente |
| 5 | `GET /milestones/{id}/board` | 🟠 | Pendiente |
| 6 | Filtros del project board | 🟠 | Pendiente |
| 7 | Detalle de work item por código | 🟠 | ✅ Resuelto |
| 8 | Paginación de colecciones | 🟠 | 🟡 Parcial |
| 9 | Control de concurrencia en `move` | 🟠 | Pendiente |
| 18 | `Viewer` no es read-only | 🟠 | ✅ Resuelto |
| 10 | `TemplateFlow` — comandos de escritura | 🟡 | 🟡 Parcial |
| 11 | RFC 7807 con `type` estable | 🟡 | 🟡 Parcial |
| 12 | Bootstrap del `Administrator` | 🟡 | ✅ Resuelto |
| 13 | Backlog view | 🟡 | Pendiente (depende de §16) |
| 14 | `Viewer` y comentarios | 🟢 | ✅ Resuelto (decisión: `Viewer` puede comentar) |
| 15 | Componente obligatorio en `Bug` | 🟢 | Pendiente (pregunta de negocio abierta) |
| 16 | Rate limiting / vencimiento de membresía | 🟢 | Pendiente |
| 17 | Export de facturación `Client` | 🟢 | Pendiente |

---

## 🔴 Crítica

### 1. No hay endpoint para desactivar usuarios
`User.Deactivate()` existe en el dominio (`Domain/Users/User.cs:92`) con su `UserDeactivatedDomainEvent`, pero sigue sin haber `Command`/`Endpoint` que lo invoque. §5.4 lo pide explícitamente ("User management (create, deactivate)") y hoy no hay forma de revocar acceso a un usuario vía API — sólo eliminando membresías de proyecto. Para un equipo que trae colaboradores temporales, esto es la mitigación central del riesgo "colaborador retiene acceso" (§14).

**Lo que ya está en sitio (y hará efectivo el endpoint en cuanto exista):** `IsActive` ya se respeta en `LoginHandler`, `RefreshTokenHandler` y `ChangePasswordHandler`, y en los guards de dominio (`Project.AddMember`, `WorkItem.Create`/`Assign`/`EnsureCanBeModifiedBy`).

**A tener en cuenta al implementarlo:** `Deactivate()` sólo cambia el flag; no revoca los `UserToken` emitidos. El refresh queda bloqueado, pero el access token JWT vigente sigue siendo válido hasta su expiración. El handler debería revocar los tokens activos (`User.RevokeToken` ya existe) o asumir explícitamente esa ventana.

### 2. ✅ RESUELTO — Aislamiento de datos entre proyectos — scoping por membresía en las queries de lectura
~~`GetAllProjectsQuery`, `GetWorkItemById`, etc. no muestran evidencia de filtrar por membresía del `IUserContext` (más allá de `RequireAuthorization()` genérico). §8.3 exige "no cross-project leakage: every query scoped to caller memberships" y §11 exige que un no-miembro reciba `404`, no `403`.~~

**Resuelto — auditoría de los query handlers hecha (2026-09-05), re-confirmada 2026-09-22.** Todos los que tocan datos de proyecto filtran por membresía del `IUserContext`, con dos helpers compartidos:

- `IsProjectMemberAsync` (`Application/Projects/Shared/ProjectMembershipExtensions.cs`) — usado por `GetProjectBoard`, `GetComponentsByProject`, `GetMilestonesByProject`.
- `CanAccessWorkItemAsync` (`Application/WorkItems/Shared/WorkItemAccessExtensions.cs`) — usado por las cuatro colecciones de actividad (`comments`, `change-logs`, `state-history`, `time-entries`).
- Filtro inline en la propia query: `GetAllProjects` (`p.Members.Any(m => m.UserId == userContext.UserId)`), `GetProjectById` y `GetWorkItemByCode` (`w.Project.Members.Any(...)`).

El requisito §11 de `404` sobre `403` también se cumple: los guards devuelven `ProjectErrors.NotFound` / `WorkItemErrors.NotFound`, que `ResultExtensions.Match` mapea a 404 — un no-miembro no puede distinguir "no existe" de "no soy miembro". `GetMySummary` y `GetMyProfile` (`users/me`) toman el id de `userContext.UserId` en el endpoint, no de la ruta, así que no son enumerables.

**Salvedad (no es cross-project leakage, pero §8.3 se lee literal):** `GET users` (`GetAllUsers`) no tiene ningún scoping — cualquier usuario autenticado lista a todos los usuarios, incluido su email y rol global. Es un directorio global, no datos de proyecto, y hoy el front lo necesita para los selectores de assignee; si se quiere cumplir §8.3 al pie de la letra habría que acotarlo a los usuarios que comparten proyecto con el caller. (Corrección respecto a la revisión anterior: `GetUserById` ya **no** es un problema — sólo se expone vía `users/me` con el id del caller; no hay `GET users/{id}`.) `GetTemplateFlowByKind` tampoco filtra, pero `TemplateFlow` es un agregado global sin `project_id` por diseño.

### 3. Sin auditoría de cambios de rol global (`Administrator`/`Member`)
§5.11 lo marca como el requisito de auditoría de mayor prioridad ("most privilege-escalating action in the system") y el propio PRD aclara que es trabajo nuevo: `Role` es un value type cerrado sin change log. Sigue sin existir `UserChangeLog`/`GlobalRoleChangeLog` en el dominio (los únicos change logs son `ProjectChangeLog` y `WorkItemChangeLog`).

**Avance relacionado — que agrava el gap:** ahora existe `PATCH users/{id:guid}/role` (`Application/Users/ChangeRole/`, `Api/Endpoints/Users/ChangeUserRole.cs`), restringido a `Administrator`. `User.ChangeRole` emite `UserRoleRemovedDomainEvent` + `UserRoleAssignedDomainEvent`, pero **no hay ninguna implementación de `IDomainEventHandler<>`** en la solución, así que esos eventos no se persisten en ningún sitio. Además los eventos no llevan el actor ni el rol previo de forma explícita. Resultado: otorgar `Administrator` vía API ya es posible y no deja rastro — exactamente el escenario que §5.11 prohíbe ("it must never be inferable only from current state").

**Pendiente:** entidad de change log en el agregado `User` (actor, timestamp, rol anterior, rol nuevo), escrita dentro de `ChangeRole` como hace `ProjectChangeLog` con la membresía; retenida aunque el usuario se desactive (§5.11).

**Salvedad lateral:** `ChangeRole` no impide degradar al último `Administrator` (incluido uno mismo). `SeedAdministratorAsync` sólo recrea el admin por defecto en el siguiente arranque si no queda ninguno, así que el sistema es recuperable, pero no de forma inmediata.

---

## 🟠 Alta

### 4. No hay "My work" board (`GET /me/board`)
§5.8 lo define como uno de los tres tipos de board, con filtro adicional por `ProjectKind`. Es un user story explícito (§6: "admin quiere ver todo su trabajo de cliente en una vista separada de sus propios productos"). Sigue existiendo sólo `GET projects/{projectId}/board`.

**Avance relacionado (no cubre el gap):** `GET users/my-summary` (`Application/Users/GetMySummary/`) devuelve perfil, contadores (incluido `myOpenIssues`: items asignados al caller en estados `Active`) y la lista de proyectos no archivados del caller. Es un resumen, no un board: no devuelve los work items ni filtra por `ProjectKind`.

**A tener en cuenta:** `GetMySummaryHandler` carga con `Include` *todos* los work items (y sus `FlowState`) de todos los proyectos del caller para contar en memoria. Ese shape no escala y no debería reutilizarse como base del `/me/board`; el conteo también debería moverse a una query agregada.

### 5. No hay milestone board (`GET /milestones/{id}/board`)
También en §5.8/§11. Sigue sin endpoint; sólo existe la lista `GET projects/{id}/milestones`.

**Avance relacionado:** desde #21 el milestone tiene `Color` opcional y el project board y el detalle de work item ya muestran el milestone de cada item, lo que da la pieza visual pero no la vista filtrada por milestone.

### 6. El project board no soporta filtros (tipo, assignee, prioridad, componente, milestone)
Sin cambios: `GetProjectBoardQuery(Guid ProjectId)` sigue sin ningún filtro. §5.8 exige filtros por tipo, assignee, prioridad, componente (incl. "no component") y milestone (incl. "no milestone"), marcados como "first-class options".

Matiz: el handler sí acota por categoría — devuelve únicamente columnas `Active`, y por tanto sólo los work items en vuelo; `Completed` y `Cancelled` no producen columna. Eso es comportamiento fijo, no un filtro que el cliente pueda pedir o quitar, y no cubre nada de lo que exige §5.8.

Nota: §11 también lista `GET work-items?projectId=&milestoneId=&componentId=&type=&assigneeId=`. La lista por proyecto bajo `work-items/` se eliminó a propósito como duplicado del board (ver `CLAUDE.md`); la decisión vigente es que los filtros se añadan a `GetProjectBoardQuery`, no reintroducir ese endpoint.

### 7. ✅ RESUELTO — `GET work-items/{idOrCode}` no resuelve códigos humanos
~~El endpoint actual es `work-items/{id:guid}` — sólo GUID. §11 pide explícitamente que resuelva tanto GUID como código (`CNL-101`), y justifica que "esto es lo que hace útiles los códigos en conversación". Sin esto, el código persistido (§5.6) no cumple su propósito de UX.~~

**Resuelto:** el endpoint de detalle es `GET work-items/{code}` (`Application/WorkItems/GetByCode/`) y busca por `w.Code == query.Code`, que es lo que pedía §11. Salvedad: acepta *sólo* el código, no el GUID — los sub-recursos (`comments`, `change-logs`, `state-history`, `time-entries`) y todos los comandos sí van por `{id:guid}`. Si §11 exige literalmente que la *misma* ruta resuelva ambas formas, queda ese resto abierto.

### 8. 🟡 PARCIAL — Sin paginación en endpoints de colección
§8.5 exige que "all collection endpoints [be] paginated; cursor-based for board queries".

**Hecho:** existe el envoltorio compartido `PagedResponse<T>` (`Application/Abstractions/Pagination/`) con `PaginationDefaults` (`DefaultPageSize = 20`, `MaxPageSize = 100`) y reglas de validación `MustBeValidPage()`/`MustBeValidPageSize()`. Lo consumen las cuatro colecciones de actividad de work item: `comments`, `change-logs`, `state-history`, `time-entries`.

**Pendiente (re-confirmado 2026-09-22, sin cambios):** el resto de endpoints de colección sigue devolviendo todo sin paginar — `GET projects` (`Projects/GetAll`), `GET users` (`Users/GetAll`), `GET projects/{id}/milestones`, `GET projects/{id}/components` y el propio board `GET projects/{projectId}/board`. La paginación implementada es además **offset-based**; §8.5 pide explícitamente *cursor-based* para las queries de board, así que el board necesita un diseño distinto, no sólo aplicarle `PagedResponse<T>`. El argumento de performance original (5.000 work items activos por proyecto, §8.1) sigue vigente y aplica justamente al board.

Mitigación parcial ya en sitio: el board devuelve sólo columnas `Active`, así que el payload deja de crecer con cada item cerrado. Eso acota el crecimiento, pero no lo elimina — 5.000 items *en vuelo* siguen viajando en una sola respuesta.

### 9. Sin idempotencia en transición de estado (`If-Match` / expected-current-state)
Sin cambios: `MoveWorkItemCommand(Id, ToStateId, Reason)` no recibe estado esperado, y no hay ningún concurrency token en la solución (ni `IsConcurrencyToken`/`IsRowVersion` ni `xmin` en las configuraciones EF). §8.5 pide rechazar movimientos obsoletos con `409 Conflict`; hoy el último en escribir gana.

**Avance relacionado (no cubre el gap):** desde `266914d` las transiciones rechazan el rol `Viewer`, así que al menos el conjunto de quien puede mover está acotado. La transición sí valida que exista un `FlowTransition` desde el estado *actual* al destino, lo que convierte algunos moves obsoletos en `TransitionNotAllowed` por accidente, pero no los detecta cuando la transición desde el nuevo estado también es válida.

### 18. ✅ RESUELTO — El rol `Viewer` no es read-only
**Resuelto (2026-09-30, `faa42d1`, rama `fix/block-viewer`).** El guard vive en el dominio (`Domain/WorkItems/WorkItem.cs`):

- `EnsureCanBeModifiedBy` ahora rechaza a un miembro con rol `Viewer` con `WorkItemErrors.ViewerCannotModify` (`Forbidden` → **403**, no 404: el Viewer ya es miembro y puede leer el item). Cubre title, description, type, priority, points, fecha estimada, component, milestone, `Move`, `LogTime`, `AddTag` y `RemoveTag`. En `Move` el Viewer se rechaza antes de llegar a `FlowTransition.AllowedRoles`.
- `WorkItem.Create`, `Assign` y `Unassign` (que tienen sus checks inline) aplican el mismo guard.
- **Assignee Viewer rechazado:** `Create` con assignee Viewer y `Assign` a un Viewer fallan con `WorkItemErrors.AssigneeIsViewer` (`Validation` → 400). Un Viewer no puede mover ni editar el item, así que asignárselo no tiene sentido.
- **Comentarios permitidos** (decisión de #14): `AddComment` usa `EnsureCanParticipate` (membresía + `IsActive` + estado del proyecto, sin mirar el rol); `UpdateComment`/`RemoveComment` no cambian (sólo el autor).

Salvedad aceptada (cerrada tal cual): no hay `ChangeMemberRole` — si un miembro se quita y se re-añade como `Viewer`, las asignaciones previas se mantienen. No se limpian ni se considera un gap.

Texto original del gap:

> §5.4 define `Viewer` como read-only ("granting an external client visibility of their own project board without any ability to modify it"). En el código, sin embargo, el único punto que mira el rol de proyecto al modificar un work item es `WorkItem.Move` (vía `FlowTransition.AllowedRoles`, que ya excluye `Viewer`). El resto de guards (`WorkItem.Create` y `EnsureCanBeModifiedBy`, usado por título, descripción, tipo, prioridad, puntos, fecha estimada, assign/unassign, component, milestone, tags, comentarios y time entries) sólo comprueban `Project.IsMember` + `IsActive` + estado del proyecto.
>
> Consecuencia: un cliente externo con rol `Viewer` puede hoy crear work items, editarlos, reasignarlos, comentar y registrar horas vía API. Es un gap de autorización, no una pregunta de negocio — la pregunta abierta de §16 (#14) es sólo si `Viewer` puede *comentar*; todo lo demás el PRD ya lo decidió como read-only. El fix natural es un guard de rol en `WorkItem.Create`/`EnsureCanBeModifiedBy` (dominio, igual que `Project.IsAdmin` para milestones/components), dejando fuera `AddComment` hasta resolver #14.

---

## 🟡 Media

### 10. 🟡 PARCIAL — `TemplateFlow` sin comandos de escritura
~~El dominio existe (`Domain/FlowStateTemplates/`) pero no hay EF configuration, `DbSet`, migración, comandos ni endpoints.~~

El agregado se renombró a `TemplateFlow` y vive en `Domain/TemplateFlows/` (`TemplateFlow`, `TemplateFlowState`, `TemplateFlowErrors`), keyed por `ProjectKind` — global, sin `project_id`.

**Hecho:** EF configuration (`TemplateFlowConfiguration`, `TemplateFlowStateConfiguration`), `DbSet`s, tablas en `InitialMigration`, seeding al arranque (`SeedTemplateFlowsAsync`, `Api/Extensions/SeedingServiceExtensions.cs`) y la query de lectura `GET template-flows/{kind}` (`Application/TemplateFlows/GetByKind/`). El front ya tiene de dónde pre-cargar los flow states al crear un proyecto, y §5.7/§12.2 se puede materializar.

**Pendiente (re-confirmado 2026-09-22, sin cambios):** no hay comandos de escritura — crear, actualizar ni eliminar templates ni sus estados. Los templates sólo existen como los siembra el seeding; un administrador no puede ajustarlos vía API, sólo tocando el código de seeding y redesplegando. Cuando se agreguen, la autorización va a nivel de endpoint con `RequireAuthorization(Role.Administrator)` — el dominio no la comprueba, a diferencia de `Milestone`/`Component`.

### 11. 🟡 PARCIAL — Errores RFC 7807 con `type` estable
**Hecho (verificado 2026-09-22):** todas las respuestas no-2xx salen como `ProblemDetails`. `ApiResponses.Problem` (`Api/Responses/ApiResponses.cs`) mapea cada `BaseErrorType` a un `type` fijo; `GlobalExceptionHandler` y `ValidationExceptionHandler` hacen lo mismo para excepciones; `AddProblemDetails` + `UseStatusCodePages` cubren los 401/403 del pipeline de auth. Los errores de validación llevan `errors` como extensión. El `type` es por tanto estable **por categoría** (5 URIs de RFC 7231).

**Pendiente:** el `type` no distingue errores dentro de una misma categoría. El código de dominio (`WorkItem.TransitionRoleNotAllowed`, `Project.OperationNotAllowedInCurrentStatus`…) sólo aparece embebido en el texto libre de `title` ("The result of the request is a X failure"), así que un cliente que quiera reaccionar a un error concreto tiene que parsear strings. Si §8.5 se lee como "machine-readable por error", falta exponer `BaseError.Code` como extensión (`code`) o derivar el `type` de él. Además, las URIs apuntan a RFC 7231, que está obsoleta (reemplazada por RFC 9110) — cosmético, pero es parte del contrato.

### 12. ✅ RESUELTO — Bootstrap de administrador seedeado
~~§12.1 pide que un único `Administrator` se cree al crear el schema (vía seed, no por API) con credenciales provistas por entorno. El git status muestra que las migraciones fueron eliminadas (posible regeneración en curso) — vale la pena confirmar que el seed de superadmin sigue existiendo tras la regeneración de migraciones.~~

**Resuelto:** `SeedAdministratorAsync` (`Api/Extensions/SeedingServiceExtensions.cs`) se ejecuta en cada arranque justo después de aplicar migraciones (`Program.cs`) y crea un `Administrator` si no existe ninguno en `flowboard.user_roles` (no-op idempotente en caso contrario). Credenciales desde la sección `Bootstrap` (`BootstrapOptions`), `AdminEmail`/`AdminPassword` obligatorias por entorno. Las migraciones existen en `Infrastructure/Database/Migrations`. Diferencias menores respecto al texto de §12.1: el seed ocurre al arrancar la app, no dentro de la migración (efecto equivalente), y no hay cambio de contraseña forzado tras el primer login — rotarla es convención operativa.

### 13. Sin backlog view / vista plana de planificación
Sin cambios. Explícitamente en §15 como future enhancement #2, y el propio PRD nota que depende de decisiones de diseño no resueltas (qué contiene, cómo se persiste el orden). No urge, pero conviene resolver las preguntas abiertas de §16 antes de que alguien lo pida. Relacionado con #6: una vista plana con filtros cubriría también el `GET work-items?...` de §11.

---

## 🟢 Baja

### 14. ✅ RESUELTO — `Viewer` y comentarios
~~§16 lo deja como pregunta abierta ("should Viewer be able to comment?").~~

**Resuelto por decisión de producto (2026-09-30): `Viewer` sí puede comentar**, y editar/borrar sus propios comentarios. Tras el fix de #18, comentar es lo único que un `Viewer` puede modificar en un work item: `AddComment` usa a propósito `EnsureCanParticipate` en vez de `EnsureCanBeModifiedBy`, y `UpdateComment`/`RemoveComment` sólo exigen ser el autor. Cubierto por `WorkItemTests.ViewerRole` (`Should_AddComment_When_AuthorIsViewer`, `Should_UpdateAndRemoveOwnComment_When_AuthorIsViewer`).

### 15. Sin componente obligatorio en items tipo `Bug`
Sin cambios — no hay ninguna regla sobre `WorkItemType.Bug` en dominio ni aplicación. Pregunta abierta en §16, pendiente de decisión de producto.

### 16. Sin rate limiting / vencimiento de membresía
Sin cambios. No hay `RateLimiter` registrado (relevante sobre todo para `POST auth/login`, anónimo). `ProjectMember` no tiene fecha de vencimiento. Rate limiting no es requisito explícito del PRD; vencimiento de membresía es future enhancement #1 (§15) — mitigación manual aceptable por ahora según §14, aunque depende de #1 para ser efectiva con usuarios que dejan el equipo.

### 17. Sin exportación de facturación (`Client` billing export)
Sin cambios. Future enhancement #7 (§15), depende de time tracking ya implementado. No urge para MVP.

---

## Nota metodológica

Revisión 2026-09-22 hecha leyendo el código (handlers, dominio, endpoints, configuración de la API), no ejecutando la app. No se verificó en profundidad: cumplimiento exacto de `p95 < 300ms` (§8.1, requiere carga real), ni cobertura completa de tests. Resuelto #18 (`Viewer` podía modificar) el 2026-09-30, los puntos con mayor impacto de seguridad son **#3** (escalada a `Administrator` sin rastro, ya expuesta por API) y **#1** (no se puede cortar el acceso de un usuario).
