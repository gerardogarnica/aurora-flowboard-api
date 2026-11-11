---
paths:
  - "src/**/WorkItems/**/*.cs"
  - "src/**/Projects/GetBoard/**/*.cs"
  - "src/Aurora.Flowboard.Api/Endpoints/Projects/**/*.cs"
  - "test/**/WorkItems/**/*.cs"
  - "src/**/Dashboard/**/*.cs"
---

# Work items

## Project board — `GET projects/{projectId:guid}/board`

`Application/Projects/GetBoard/` returns work items grouped by the project's **`Active`-category** `FlowState`s (Kanban shape), not a flat list, ordered by `SortOrder`.

- `Completed` and `Cancelled` are terminal: they produce no column, and the work items in them are absent from the response — the board is a view of in-flight work, not an archive. There is no opt-in to include them; a closed item is reached through `GET work-items/{code}`.
- Only `Active` states get a real `SortOrder` (`Project.AddFlowState` assigns `0` to terminal ones), which is why ordering is safe once they are filtered out.
- A project with no flow states (or no `Active` ones) returns an empty board, not a 404.
- Each column carries `availableTransitions`: the transitions out of that state the requester's `ProjectRole` may take, for drag-and-drop on the web board. It **must** match `GET work-items/{code}` `availableTransitions` for an item in that state: destinations of every category (terminal ones included — the front-end intersects with the visible columns), no `Viewer` special-casing (whatever `AllowedRoles` says), ordered by `ToStateName`, `[]` rather than null. Both handlers build it through `GetAvailableTransitionsByStateAsync` (`Projects/Shared/FlowTransitionQueryExtensions.cs`) — change the contract there, never in one handler.
- `/board` is the only project board endpoint. A near-identical `GET projects/{id}/work-items` was removed as an unused duplicate — don't reintroduce a per-project work item list under `work-items/`; extend `GetProjectBoardQuery` instead. `GET work-items` (no project in the route) is the cross-project search below, not a per-project list.
- Each column carries `stage` (`Backlog` / `InFlight`, see below). Only `Active` states have a stage, so every column has one.

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

## Detail vs. activity collections

`GET work-items/{code}` returns only bounded data: the scalars, `tags`, and `availableTransitions`. The four unbounded activity collections live in their own paginated sub-endpoints, keyed by the work item's **`{id:guid}`** (not its code, matching sub-resources like `POST work-items/{id:guid}/comments`):

```
GET work-items/{id:guid}/comments
GET work-items/{id:guid}/change-logs
GET work-items/{id:guid}/state-history
GET work-items/{id:guid}/time-entries
```

Do not move these back into the detail payload. Projecting five sibling collections in one query produced a cartesian product (see `ef-core-queries.md`), and `change_logs` grows monotonically — every field update, move, assignment, comment and tag operation writes a row, and nothing prunes them.

## Change log semantics

`WorkItemChangeLog.AffectedEntityId` points at a different table depending on `ChangeType`: a `User` for `Assigned`, a `FlowState` for `Moved`, a `Component` for `ComponentChanged`, a `Milestone` for `MilestoneChanged`; null for the rest. `GetWorkItemChangeLogsHandler` resolves it into `AffectedEntityName` accordingly. `WorkItem.Create` writes `MilestoneChanged`/`ComponentChanged` entries when created with a milestone or component, mirroring `ChangeMilestone`/`ChangeComponent`.

## Command handlers on an existing work item

Every command that mutates an existing work item derives from `WorkItemFieldUpdateHandler<TCommand>` (`WorkItems/Shared/`): it loads the work item with `Project.Members`, resolves the current user, calls `ApplyAsync` and saves. Don't write that body again in a new handler.

- Extra includes go in the `Shape` override (`Tags`, `Comments`, `FlowState`, `Project.FlowTransitions`); the base adds `AsSplitQuery()` after it.
- Lookups of other entities (assignee, target state, milestone, component) go in `ApplyAsync`, so the current user is resolved first and `UserErrors.NotFound` takes precedence over the lookup's own not-found error.
- Override `TransactionIsolationLevel` only for the write-skew cases in `concurrency.md`.

## `Viewer` is read-only on work items, except comments

- `WorkItem.EnsureCanBeModifiedBy` rejects `ProjectRole.Viewer` with `WorkItemErrors.ViewerCannotModify` (403); `Create`/`Assign`/`Unassign` apply the same check inline. A Viewer cannot be an assignee (`AssigneeIsViewer`).
- **Any new work item mutation goes through `EnsureCanBeModifiedBy`.**
- `AddComment` deliberately uses the role-agnostic `EnsureCanParticipate`: it is a product decision that a Viewer may comment (and edit/remove their own comments) — don't "fix" it to `EnsureCanBeModifiedBy`.
- Accepted as-is: a member removed and re-added as Viewer keeps any existing assignments.
