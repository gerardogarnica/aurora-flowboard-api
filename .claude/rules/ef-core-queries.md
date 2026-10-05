---
paths:
  - "src/Aurora.Flowboard.Application/**/*.cs"
  - "src/Aurora.Flowboard.Infrastructure/Database/**/*.cs"
---

# EF Core queries, tracking and pagination

There is no repository pattern; handlers query `IApplicationDbContext` directly with LINQ, so the shape of the query is the handler's responsibility. `IApplicationDbContext` is the data-access boundary.

## Raw SQL in handlers is intentional

Schema-qualified raw SQL (`FromSqlRaw` / `ExecuteSqlRawAsync`, e.g. `FROM flowboard.projects FOR UPDATE` for pessimistic locking in `CreateWorkItemHandler`) is allowed in Application handlers. Do not flag it as a Clean Architecture violation and do not suggest extracting it into a repository interface.

## Query shape

- **Never project more than one collection per query.** Sibling collections become same-level `LEFT JOIN`s and the row count is their *product*, not their sum. A work item with 3 tags × 15 comments × 8 time entries × 6 transitions × 40 change logs returned 86,400 rows — each carrying the full duplicated scalar payload. Split the collections into separate queries (or separate endpoints, as the work item activity collections are). `AsSplitQuery()` mitigates it when several collections genuinely must load together, but it is not a substitute for splitting an unbounded collection out; with a single collection it only buys an extra round trip, so don't add it reflexively.
- **Display names are resolved with correlated subqueries** against `dbContext.Users` / `FlowStates` / `Components` / `Milestones`, e.g. `dbContext.Users.Where(u => u.Id == x.UserId).Select(u => u.FirstName + " " + u.LastName).FirstOrDefault() ?? string.Empty`. `Comment`, `StateTransitionHistory` and `WorkItemChangeLog` deliberately hold raw `Guid` FKs with no navigation properties, so there is nothing to join through. Keep these subqueries inside a page-limited query — they are evaluated per returned row.
- **Expression trees cannot contain `switch` expressions (CS8514).** Inside an `IQueryable` projection the lambda is an `Expression<Func<>>`, so multi-branch logic has to be a ternary chain. `GetWorkItemChangeLogsHandler`'s `AffectedEntityName` looks verbose for exactly this reason and carries a comment saying so — don't "simplify" it into a `switch`, it won't compile.
- **No collection expressions in expression trees (CS9175).** An empty placeholder `[]` inside an `IQueryable.Select(...)` does not compile; use `Array.Empty<T>()` when a projection needs a placeholder filled in after materialization.
- **`FlowTransition.AllowedRoles` is not translatable.** EF maps the private `_allowedRoles` field as a `PrimitiveCollection`, not the public property, so referencing `AllowedRoles` in a `.Where`/`.Select` that becomes SQL throws `InvalidOperationException ... could not be translated`. Query the translatable data first, materialize the transitions with a separate `dbContext.FlowTransitions.Where(...).ToListAsync(...)`, then filter in memory (`t.AllowedRoles.Contains(role)`). See `GetWorkItemByCodeHandler`.

## Tracking (Guid keys are `ValueGeneratedNever`)

`ApplicationDbContext.OnModelCreating` forces every `Guid` primary key to `ValueGenerated.Never`: the domain assigns ids up front, never the store. Consequences:

- **Never call `dbContext.X.Update(entity)` on a tracked aggregate.** It marks the whole graph `Modified`, so new children (whose keys are already set) emit an `UPDATE` for a row that doesn't exist → `DbUpdateConcurrencyException` ("expected to affect 1 row(s), but actually affected 0"). Mutate the tracked entity and call `SaveChangesAsync`.
- **Command handlers load the aggregate they mutate without `AsNoTracking()`** (query handlers always use `AsNoTracking()`).
- **Any entity assigned to a navigation property on a tracked aggregate must itself be tracked.** `WorkItem.Move(toState, ...)` sets `FlowState = toState`; loading `toState` with `AsNoTracking()` made EF treat it as `Added` → `INSERT INTO flow_states` → `23505 duplicate key ... pk_flow_states`. Entities read only for a scalar `.Id` (e.g. the assignee `User` in `AssignWorkItemHandler`) can stay `AsNoTracking()`.

## Pagination

- `PagedResponse<T>` (`Application/Abstractions/Pagination/`) is the shared envelope: `Items`, `Page`, `PageSize`, `TotalCount`, and a computed `TotalPages`. Offset-based, `Page` is 1-based.
- `PaginationDefaults` holds `DefaultPage = 1`, `DefaultPageSize = 20`, `MaxPageSize = 100`; endpoints take `page`/`pageSize` as optional query params defaulted from those constants.
- Validators apply `MustBeValidPage()` / `MustBeValidPageSize()` (`Abstractions/Validations/PaginationRuleExtensions.cs`) — exceeding `MaxPageSize` is a 400, not a silent clamp.
- Activity collections are ordered newest-first, always with the entity `Id` as tie-breaker so paging stays stable.
- A page past the end returns 200 with an empty `Items` and the real `TotalCount`, never a 404.
