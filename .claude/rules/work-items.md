---
paths:
  - "src/**/WorkItems/**/*.cs"
  - "src/**/Projects/GetBoard/**/*.cs"
  - "src/Aurora.Flowboard.Api/Endpoints/Projects/**/*.cs"
  - "test/**/WorkItems/**/*.cs"
---

# Work items

## Project board — `GET projects/{projectId:guid}/board`

`Application/Projects/GetBoard/` returns work items grouped by the project's **`Active`-category** `FlowState`s (Kanban shape), not a flat list, ordered by `SortOrder`.

- `Completed` and `Cancelled` are terminal: they produce no column, and the work items in them are absent from the response — the board is a view of in-flight work, not an archive. There is no opt-in to include them; a closed item is reached through `GET work-items/{code}`.
- Only `Active` states get a real `SortOrder` (`Project.AddFlowState` assigns `0` to terminal ones), which is why ordering is safe once they are filtered out.
- A project with no flow states (or no `Active` ones) returns an empty board, not a 404.
- Each column carries `availableTransitions`: the transitions out of that state the requester's `ProjectRole` may take, for drag-and-drop on the web board. It **must** match `GET work-items/{code}` `availableTransitions` for an item in that state: destinations of every category (terminal ones included — the front-end intersects with the visible columns), no `Viewer` special-casing (whatever `AllowedRoles` says), ordered by `ToStateName`, `[]` rather than null. Both handlers build it through `GetAvailableTransitionsByStateAsync` (`Projects/Shared/FlowTransitionQueryExtensions.cs`) — change the contract there, never in one handler.
- `/board` is the only project board endpoint. A near-identical `GET projects/{id}/work-items` was removed as an unused duplicate — don't reintroduce a per-project work item list under `work-items/`; extend `GetProjectBoardQuery` instead.

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

## `Viewer` is read-only on work items, except comments

- `WorkItem.EnsureCanBeModifiedBy` rejects `ProjectRole.Viewer` with `WorkItemErrors.ViewerCannotModify` (403); `Create`/`Assign`/`Unassign` apply the same check inline. A Viewer cannot be an assignee (`AssigneeIsViewer`).
- **Any new work item mutation goes through `EnsureCanBeModifiedBy`.**
- `AddComment` deliberately uses the role-agnostic `EnsureCanParticipate`: it is a product decision that a Viewer may comment (and edit/remove their own comments) — don't "fix" it to `EnsureCanBeModifiedBy`.
- Accepted as-is: a member removed and re-added as Viewer keeps any existing assignments.
