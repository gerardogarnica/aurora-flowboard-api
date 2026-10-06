---
paths:
  - "src/**/Projects/**/*.cs"
  - "src/**/TemplateFlows/**/*.cs"
  - "test/**/Projects/**/*.cs"
  - "test/**/TemplateFlows/**/*.cs"
---

# Project flow and template flows

## One flow per project

There is no `Flow` aggregate. `FlowState` and `FlowTransition` are child entities of `Project` (FK `project_id`), and a work item reaches its transitions via `Project.FindFlowTransition(...)`.

## A project's flow is set at creation and is not editable over HTTP

The `projects/{id}/flow/...` endpoints (get flow, add/remove state, add/remove transition role) and their Application slices were removed — the front-end never called them and there is no plan to. **Don't reintroduce them without asking**; their absence is a deliberate scope decision. What remains:

- `CreateProjectCommand` takes the `FlowStates` list, and `CreateProjectHandler` calls `Project.AddFlowState` for each. This is the only path that writes flow states; the front-end pre-fills it from `GET template-flows/{kind}`.
- The domain methods (`Project.AddFlowState`, `RemoveFlowState`, `AddFlowTransitionRole`, `RemoveFlowTransitionRole`) and their `Domain.UnitTests` coverage are intentionally kept. Re-exposing any of them is a new slice + endpoint, not a domain change.
- Flow states and transitions are still *read* through `GET projects/{projectId:guid}/board` (columns — `Active` category only — each with its `availableTransitions` to every category) and `GET work-items/{code}` (`availableTransitions`, every category). Both share `GetAvailableTransitionsByStateAsync`; see `work-items.md`.

Consequence: a project created with the wrong flow can only be fixed by direct SQL.

## TemplateFlows

- `TemplateFlow` (`Domain/TemplateFlows/`) is a global aggregate root keyed by `ProjectKind` — not owned by a `Project` (no `project_id` FK). It holds suggested `TemplateFlowState` entries (`Name`, `FlowStateCategory`, `Color`, `SortOrder`) that the front-end uses to pre-fill a new project's flow states. Project creation needs no change for this: it already accepts an explicit `FlowStates` list.
- One template per `ProjectKind`; uniqueness is enforced at the Application layer (query + DB unique index), not in `TemplateFlow.Create`.
- Unlike `Milestone`/`Component`, authorization is **not** checked in the domain — future write endpoints must enforce it with `RequireAuthorization(policy => policy.RequireRole(Role.Administrator.Name))`, like `POST users`. `TemplateFlow.CreatedBy` is just an audit `Guid`.
- `TemplateFlowState.Category` is immutable after creation — `TemplateFlow.UpdateState` only takes `(stateId, name, color)`. To change a category, remove the state and add it again.
- Default templates are seeded at startup by `SeedTemplateFlowsAsync` (`Api/Extensions/SeedingServiceExtensions.cs`).
- Currently exposed: `GET template-flows/{kind}` (`Application/TemplateFlows/GetByKind/`). There are no write endpoints yet.
