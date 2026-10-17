---
paths:
  - "src/Aurora.Flowboard.Application/**/*.cs"
  - "src/Aurora.Flowboard.Infrastructure/Configurations/**/*.cs"
  - "src/Aurora.Flowboard.Infrastructure/Database/**/*.cs"
---

# Concurrency and persistence conflicts

Decision record: `docs/architecture/adr/0001-optimistic-concurrency.md`.

## `xmin` on aggregate roots

- `Project`, `Milestone`, `Component`, `WorkItem`, `User` and `UserToken` call `builder.HasXminConcurrencyToken()` (`Infrastructure/Database/ConcurrencyTokenExtensions.cs`): a shadow `uint Version` mapped to Postgres' `xmin` with `IsRowVersion()`. Npgsql emits no SQL for it; adding it to an entity only changes the model snapshot (check with `dotnet ef migrations script`).
- A new aggregate root that can be edited over HTTP calls it too. `TemplateFlow` does not: only the seeder writes it.
- `xmin` only guards the rows EF updates. A domain method that changes children an invariant of the root depends on must also write the root (`UpdatedOnUtc`), as `Project.AddMember` and `Project.RemoveMember` do; otherwise two concurrent changes to the children never compete for the root row. Append-only children with no invariant on the root (work item comments, time entries) don't.

## Serializable for write skew

`xmin` cannot see a race where each request reads one row and writes another. Those handlers open `dbContext.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)` before their first read, save, and `CommitAsync`; a domain error returns without committing. Postgres (SSI) aborts one side with `40001`.

| Reads to decide | Writes | Handlers (all Serializable) |
|---|---|---|
| Open work items of a milestone / the milestone status | Milestone / work item | `ChangeMilestoneStatusHandler`, `ChangeWorkItemMilestoneHandler`, `CreateWorkItemHandler` |
| Open work items of a component / the component status | Component / work item | `RetireComponentHandler`, `ChangeWorkItemComponentHandler`, `CreateWorkItemHandler` |
| Other active Administrators | The demoted user | `ChangeUserRoleHandler`, only when demoting an active Administrator |

- SSI only protects transactions that are **all** serializable: a new handler on either side of these races must open one too.
- `WorkItemFieldUpdateHandler<TCommand>` opens the transaction when a subclass overrides `TransactionIsolationLevel`.
- Handler tests substitute `IDbContextTransaction` for `BeginTransactionAsync(Arg.Is(IsolationLevel.Serializable), ...)` and assert `CommitAsync` on success and no commit on a domain error. `CreateWorkItemHandler` has no handler tests (`FromSqlRaw` can't run on the mocked `DbSet`).

## Translation to 409

`PersistenceConflictBehavior` is the innermost command decorator (`Logging → Performance → Validation → PersistenceConflict → Handler`); queries don't have it.

| Exception | Error (409) |
|---|---|
| `DbUpdateConcurrencyException` | `PersistenceErrors.ConcurrencyConflict` |
| `DbException` with SqlState `40001`, direct (query, `CommitAsync`) or inside a `DbUpdateException` | `PersistenceErrors.ConcurrencyConflict` |
| `DbUpdateException` with an inner `DbException` with SqlState `23505` | `PersistenceErrors.UniqueViolation` |

- Anything else propagates to `GlobalExceptionHandler` (500). The behavior logs a warning with the exception, which keeps the constraint name.
- Handlers don't catch these exceptions themselves, except when the answer is not a 409: `RefreshTokenHandler` (→ `InvalidRefreshToken`, 401) and `LogoutHandler` (→ `Ok`).
- Uniqueness pre-checks (`AnyAsync` on prefix, email, names) stay: they return the specific domain error in the normal case; the behavior only covers the race.
