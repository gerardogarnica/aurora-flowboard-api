---
paths:
  - "src/**/Milestones/**/*.cs"
  - "src/**/Components/**/*.cs"
  - "test/**/Milestones/**/*.cs"
  - "test/**/Components/**/*.cs"
---

# Milestones and Components

- Both are project-owned **aggregate roots** (own `Domain/Milestones` and `Domain/Components` folders, FK `project_id`), not child collections mapped through `Project` the way `FlowState`/`FlowTransition` are.
- Only a project admin (`Project.IsAdmin`) can create, update or change status — checked in the domain.
- Every mutation (`Milestone.Create/Update/ChangeStatus`, `Component.Create/Rename/Retire`) also requires the project to be modifiable (`Project.IsModifiable`: `Active` or `Maintenance`) and fails with `ProjectErrors.OperationNotAllowedInCurrentStatus` otherwise — including archiving a milestone of a `Completed` project. The check runs right after the admin check, so handlers must load `Project` (they already do, for `IsAdmin`).
- `Milestone` status state machine, enforced in `Milestone.ChangeStatus`:
  - `Draft → Active / Archived`
  - `Active → OnHold / Completed / Archived`
  - `OnHold → Active / Archived`
- Closing a `Milestone` (`Completed`/`Archived`) or retiring a `Component` is blocked while it has open work items.
- `WorkItem` optionally references a `Milestone` and/or `Component` via nullable `milestone_id` / `component_id`.
- Endpoints live under `projects/{id}/milestones/...` and `projects/{id}/components/...`.

## Command handlers on an existing milestone or component

Every command that mutates an existing milestone or component derives from `MilestoneCommandHandler<TCommand>` (`Milestones/Shared/`) or `ComponentCommandHandler<TCommand>` (`Components/Shared/`): they load the entity with `Project.Members`, resolve the current user, call `ApplyAsync` and save. `Create` does not derive from them. Don't write that body again in a new handler.

- Extra includes go in the `Shape` override. Unlike `WorkItemFieldUpdateHandler`, these bases don't add `AsSplitQuery()`: with `Project.Members` as the only collection it would just cost a round trip (`ef-core-queries.md`). A `Shape` that adds a second collection adds it (`RenameComponentHandler` with `Project.Components`).
- Closing a milestone and retiring a component count the open work items with `CountOpenAsync` (`WorkItems/Shared/OpenWorkItemQueryExtensions.cs`) and override `TransactionIsolationLevel` to `Serializable` (`concurrency.md`). Don't copy the "not `Completed`/`Cancelled`" predicate into a handler.
