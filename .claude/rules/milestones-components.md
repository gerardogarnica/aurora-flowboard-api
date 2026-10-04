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
- `Milestone` status state machine, enforced in `Milestone.ChangeStatus`:
  - `Draft → Active / Archived`
  - `Active → OnHold / Completed / Archived`
  - `OnHold → Active / Archived`
- Closing a `Milestone` (`Completed`/`Archived`) or retiring a `Component` is blocked while it has open work items.
- `WorkItem` optionally references a `Milestone` and/or `Component` via nullable `milestone_id` / `component_id`.
- Endpoints live under `projects/{id}/milestones/...` and `projects/{id}/components/...`.
