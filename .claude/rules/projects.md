---
paths:
  - "src/**/Projects/**/*.cs"
  - "test/**/Projects/**/*.cs"
---

# Projects

## Command handlers on an existing project

Every command that mutates an existing project (`Update`, `ChangeKind`, `ChangeStatus`, `AddMember`, `RemoveMember`) derives from `ProjectAdminCommandHandler<TCommand>` (`Projects/Shared/`): it loads the project with `Members`, resolves the current user, calls `ApplyAsync` and saves. The admin check itself stays in the domain (`Project.IsAdmin`). Don't write that body again in a new handler.

- Lookups of other entities (the target user of `AddMember`/`RemoveMember`) go in `ApplyAsync`, so the current user is resolved first, as in the work item handlers.
- The base has no `Shape` or `TransactionIsolationLevel` hook: no project command needs another include or a write-skew transaction yet. Add the hook, as in `WorkItemFieldUpdateHandler`, when one does.
- `Create` does not derive from it: there is no project to load.
