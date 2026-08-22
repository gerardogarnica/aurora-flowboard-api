# Work Item Field Updates — Design

**Date:** 2026-08-22
**Status:** Approved for implementation
**Branch:** `feature/milestones-components`

## 1. Problem

The front-end edits a work item through inline controls that save on blur, with no submit button. Today only the title supports this, via `PATCH work-items/{id}/title`. The same behaviour is needed for description, type, priority, estimated completion date, estimated points, component and milestone — and for fields added later.

Two endpoints currently overlap and neither fits the need:

- `PUT work-items/{id}` (`UpdateWorkItem`) overwrites Title, Description, Priority, EstimatedPoints and EstimatedCompletionDate in one shot and records a generic `WorkItemChangeType.Updated` entry.
- `PATCH work-items/{id}/title` (`UpdateWorkItemTitle`) updates one field, records `TitleUpdated`, raises a domain event, and short-circuits when the value is unchanged.

`Type`, `ComponentId` and `MilestoneId` are not updatable anywhere — they are only set in `WorkItem.Create`.

## 2. Decisions

| # | Decision | Rationale |
|---|----------|-----------|
| 1 | One `PATCH` endpoint per field | The front-end always sends exactly one field per request, so putting the field name in the URL removes the "absent vs null" ambiguity entirely and keeps every payload strongly typed and correctly documented in Swagger. |
| 2 | Per-field changelog entries | Consistent with the existing `TitleUpdated`/`Moved`/`Assigned` pattern. Enum values are persisted as ints and appended, so no data migration is required. |
| 3 | Delete the `Update` use case and `WorkItem.Update(...)` | It bypasses per-field auditing and silently overwrites five fields at once. |
| 4 | No new domain events | No `IDomainEventHandler` implementation exists anywhere in the solution; seven new records would be dead code. `WorkItemTitleUpdatedDomainEvent` stays as-is. |
| 5 | Extract `EnsureCanBeModifiedBy` | Removes a 15-line guard block repeated across the aggregate. |
| 6 | Factor the Application handlers onto an abstract base | The seven handlers are the same ~30 lines; the base collapses each to ~8. |
| 7 | A caller who is not a project member gets `404`, not `403` | PRD §11 note: project existence must not be disclosed. |
| 8 | Milestone `Completed`/`Archived` stays `400`, PRD gets corrected | The PRD says `409`, but `MilestoneNotAcceptingAssignments` is already used by `WorkItem.Create`; changing it would silently alter the `POST work-items` contract. |
| 9 | No concurrency control | Each endpoint writes a single column, so lost-update damage is bounded. Explicitly out of scope, not an oversight. |
| 10 | Responses stay `202 Accepted` with an empty body | Matches every other command endpoint in the repo. `204` would be more honest but is not worth diverging for now. |

## 3. API surface

Seven new endpoints, one refactored, one deleted. All are `PATCH`, all `RequireAuthorization()`, all tagged `WorkItems`, all return `202 Accepted` with an empty body.

```
PATCH work-items/{id}/description               { "description": string? }
PATCH work-items/{id}/type                      { "type": WorkItemType }
PATCH work-items/{id}/priority                  { "priority": Priority }
PATCH work-items/{id}/estimated-points          { "estimatedPoints": int? }
PATCH work-items/{id}/estimated-completion-date { "estimatedCompletionDate": DateOnly? }
PATCH work-items/{id}/component                 { "componentId": Guid? }
PATCH work-items/{id}/milestone                 { "milestoneId": Guid? }

PATCH work-items/{id}/title    unchanged contract, migrates to the base handler
PUT   work-items/{id}          DELETED
```

`null` in the body unambiguously means "clear this field", because the field being written is named in the route.

Declared status codes per endpoint: `202`, `400`, `404`, `500`. `403` is not produced by any of these routes: the only failures reachable are `ProjectErrors.OperationNotAllowedInCurrentStatus` and `UserErrors.Inactive` (both `Validation` → 400), the field validators (400), and `WorkItemErrors.NotFound` / `UserErrors.NotFound` / `ComponentErrors.NotFound` / `MilestoneErrors.NotFound` (404). See §6.

## 4. Domain layer

### 4.1 `EnsureCanBeModifiedBy`

```csharp
private Result EnsureCanBeModifiedBy(User changedBy)
{
    if (!Project.CanAddOrUpdateWorkItem())
    {
        return Result.Fail(ProjectErrors.OperationNotAllowedInCurrentStatus);
    }

    if (!Project.IsMember(changedBy.Id))
    {
        return Result.Fail(WorkItemErrors.NotFound);
    }

    if (!changedBy.IsActive)
    {
        return Result.Fail(UserErrors.Inactive);
    }

    return Result.Ok();
}
```

Applied to the seven new methods plus `UpdateTitle`, `Move`, `AddComment`, `LogTime`, `AddTag` and `RemoveTag` — the methods where the trio already appears contiguously and in this order, so the extraction is behaviour-preserving.

**Not** applied to `Assign`, `Unassign`, `UpdateComment` or `RemoveComment`. `Assign` interleaves `IsMember(assignee)` and the `Cancelled` check between the guards; `Unassign` checks `AssigneeId` before membership; `UpdateComment` and `RemoveComment` never check `IsActive`. Forcing the method into these would change which error surfaces first and break existing tests.

Extraction also normalises the `Result.Fail<WorkItem>(ProjectErrors.OperationNotAllowedInCurrentStatus)` calls that appear inside methods returning a non-generic `Result`.

### 4.2 New methods on `WorkItem`

```csharp
public Result UpdateDescription(string? description, User changedBy, DateTime updatedOnUtc)
public Result UpdateType(WorkItemType type, User changedBy, DateTime updatedOnUtc)
public Result UpdatePriority(Priority priority, User changedBy, DateTime updatedOnUtc)
public Result UpdateEstimatedPoints(int? estimatedPoints, User changedBy, DateTime updatedOnUtc)
public Result UpdateEstimatedCompletionDate(DateOnly? date, User changedBy, DateTime updatedOnUtc)
public Result ChangeComponent(Component? component, User changedBy, DateTime updatedOnUtc)
public Result ChangeMilestone(Milestone? milestone, User changedBy, DateTime updatedOnUtc)
```

Every one follows the same shape:

1. `EnsureCanBeModifiedBy(changedBy)` — return on failure.
2. Field-specific validation.
3. **No-op short-circuit**: if the new value equals the current one, return `Result.Ok()` without touching `UpdatedOnUtc` or writing a changelog entry.
4. Assign the field, set `UpdatedOnUtc`, append the changelog entry.

Step 3 is a hard requirement, not a nicety: inline controls fire on every blur, and without it the changelog fills with meaningless entries.

Field-specific validation:

- `UpdateDescription` — `MaxDescriptionLength` (4000); trims; `null` allowed.
- `UpdateType`, `UpdatePriority` — no domain rule beyond the guards.
- `UpdateEstimatedPoints` — must be greater than zero when present. Needs a new `WorkItemErrors.EstimatedPointsInvalid`.
- `UpdateEstimatedCompletionDate` — no domain rule; the "not in the past" rule lives in the validator, which already has `IDateTimeProvider` injected. This matches how `UpdateWorkItemValidator` handled it.
- `ChangeComponent` — reuses `ComponentNotInProject` and `ComponentRetired`.
- `ChangeMilestone` — reuses `MilestoneNotInProject` and `MilestoneNotAcceptingAssignments`.

`ChangeComponent` and `ChangeMilestone` take the **entity, not the id**. The domain cannot validate `ProjectId` or `Status` from a bare `Guid`, and this matches `Create` (which already accepts `Milestone?`/`Component?`) and `Assign(User)`. Passing `null` clears the association and skips the entity rules.

Both set the FK **and** the navigation property so the in-memory graph stays consistent.

### 4.3 `WorkItemChangeType`

Seven values appended (ints 12–18):

```csharp
DescriptionUpdated = 12,
TypeUpdated = 13,
PriorityUpdated = 14,
EstimatedPointsUpdated = 15,
EstimatedCompletionDateUpdated = 16,
ComponentChanged = 17,
MilestoneChanged = 18
```

For `ComponentChanged` and `MilestoneChanged`, `AffectedEntityId` carries the new component/milestone id, or `null` when the association is cleared. This reuses the existing column rather than adding one.

`Updated = 1` stays in the enum, marked `// legacy — no longer written; historical rows only`. Existing rows in `flowboard.work_item_change_logs` hold `change_type = 1` and must keep resolving.

These values are part of the public contract: `WorkItemChangeLogResponse.ChangeType` is serialised **as a string** (`JsonStringEnumConverter`, `Api/DependencyInjection.cs:23`) and returned by `GET work-items/{code}`. The addition is backward compatible, but the front-end must tolerate unknown values rather than throw.

### 4.4 Deletions

`WorkItem.Update(...)` is removed. No production caller remains after §7.

## 5. Application layer

### 5.1 Base handler

New file `Application/WorkItems/Shared/WorkItemFieldUpdateHandler.cs`:

```csharp
internal abstract class WorkItemFieldUpdateHandler<TCommand>(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    protected IApplicationDbContext DbContext => dbContext;

    public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
    {
        WorkItem? workItem = await dbContext
            .WorkItems
            .Include(w => w.Project)
            .ThenInclude(p => p.Members)
            .AsSplitQuery()
            .SingleOrDefaultAsync(w => w.Id == GetWorkItemId(command), cancellationToken);

        if (workItem is null)
        {
            return Result.Fail(WorkItemErrors.NotFound);
        }

        User? changedBy = await dbContext
            .Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);

        if (changedBy is null)
        {
            return Result.Fail(UserErrors.NotFound);
        }

        Result result = await ApplyAsync(
            workItem, command, changedBy, dateTimeProvider.UtcNow, cancellationToken);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    protected abstract Guid GetWorkItemId(TCommand command);

    protected abstract Task<Result> ApplyAsync(
        WorkItem workItem,
        TCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken);
}
```

`ApplyAsync` is asynchronous because the component and milestone handlers must load an entity. The five scalar handlers return `Task.FromResult(...)`.

Scrutor registration is unaffected: `AddClasses` skips abstract types, so only the concrete handlers register as `ICommandHandler<TCommand>`.

### 5.2 Slices

Each field gets `Command` + `Validator` + `Handler` under `Application/WorkItems/<Feature>/`:

| Field | Folder | Command |
|-------|--------|---------|
| Description | `UpdateDescription/` | `UpdateWorkItemDescriptionCommand(Guid Id, string? Description)` |
| Type | `UpdateType/` | `UpdateWorkItemTypeCommand(Guid Id, WorkItemType Type)` |
| Priority | `UpdatePriority/` | `UpdateWorkItemPriorityCommand(Guid Id, Priority Priority)` |
| Estimated points | `UpdateEstimatedPoints/` | `UpdateWorkItemEstimatedPointsCommand(Guid Id, int? EstimatedPoints)` |
| Estimated date | `UpdateEstimatedCompletionDate/` | `UpdateWorkItemEstimatedCompletionDateCommand(Guid Id, DateOnly? EstimatedCompletionDate)` |
| Component | `ChangeComponent/` | `ChangeWorkItemComponentCommand(Guid Id, Guid? ComponentId)` |
| Milestone | `ChangeMilestone/` | `ChangeWorkItemMilestoneCommand(Guid Id, Guid? MilestoneId)` |

`UpdateTitle/` keeps its command and validator and its handler is rewritten onto the base.

Validators: `Id` `NotEmpty()` in all seven; `MaximumLength(WorkItem.MaxDescriptionLength)` when description is not null; `GreaterThan(0)` when points has a value; `GreaterThanOrEqualTo(dateTimeProvider.Today)` when the date has a value; `IsInEnum()` for type and priority. Component and milestone need no rule beyond `Id`.

### 5.3 Loading component and milestone

`ChangeComponentHandler` and `ChangeMilestoneHandler` load their entity **tracked** — no `AsNoTracking()`:

```csharp
Component? component = command.ComponentId is null
    ? null
    : await DbContext.Components
        .SingleOrDefaultAsync(c => c.Id == command.ComponentId, cancellationToken);

if (command.ComponentId is not null && component is null)
{
    return Result.Fail(ComponentErrors.NotFound);
}
```

Tracking matters: primary keys are configured `ValueGeneratedNever`, so attaching an untracked entity to a navigation property makes EF try to INSERT it.

## 6. Non-member returns 404

`WorkItemErrors.UserNotProjectMember` is deleted. All twelve call sites in `WorkItem.cs` return `WorkItemErrors.NotFound` instead.

Flipping the existing error's type to `NotFound` would not be enough. `ApiResponses.Problem` puts `Error.Code` into the response `title` and `Error.Message` into `detail`, so a `404` carrying `"WorkItem.UserNotProjectMember"` and `"Only project members can perform this operation on a work item"` would disclose precisely what the change is meant to hide. A non-member must receive a response byte-identical to the one for a work item that does not exist.

**Unchanged, deliberately:**

- `AssigneeNotProjectMember` (`Validation`, 400) — this is about the *assignee*, not the caller. The caller is a member and already has visibility into the project, so naming the problem discloses nothing.
- `TransitionRoleNotAllowed` (`Forbidden`, 403) — the caller is a member and can see the item; they simply lack the project role for that transition.
- `CommentNotOwnedByUser` (`Forbidden`, 403) — same reasoning.
- The `OnlyAdminCan*` errors in `ProjectErrors`, `MilestoneErrors` and `ComponentErrors` — these are member-but-not-admin cases, which leak nothing.

## 7. Deletions

| Path | Note |
|------|------|
| `src/.../Application/WorkItems/Update/UpdateWorkItemCommand.cs` | |
| `src/.../Application/WorkItems/Update/UpdateWorkItemHandler.cs` | |
| `src/.../Application/WorkItems/Update/UpdateWorkItemValidator.cs` | |
| `src/.../Api/Endpoints/WorkItems/UpdateWorkItem.cs` | Removes `PUT work-items/{id}` |
| `WorkItem.Update(...)` in `Domain/WorkItems/WorkItem.cs` | |
| `WorkItemErrors.UserNotProjectMember` | Replaced by `WorkItemErrors.NotFound` |
| `test/.../WorkItems/UpdateWorkItemHandlerTests.cs` | |
| `test/.../WorkItems/UpdateWorkItemValidatorTests.cs` | |

Orphaned builders in `test/.../WorkItems/WorkItemCommandData.cs` and the `Update` tests in `test/.../WorkItems/WorkItemTests.cs` are removed with them.

## 8. Tests

- **Domain** — for each of the seven new methods: happy path, each field-specific validation failure, the no-op short-circuit (asserting no changelog entry is appended and `UpdatedOnUtc` is untouched), and the three guard failures. Plus regression coverage that the five methods refactored onto `EnsureCanBeModifiedBy` still fail with the same errors in the same order.
- **Application** — a handler test class and a validator test class per field, following `UpdateWorkItemTitleHandlerTests` and the `MockDbSetHelper` pattern.
- **Existing tests to update** — the 14 domain assertions and 4 application assertions on `WorkItemErrors.UserNotProjectMember` now expect `WorkItemErrors.NotFound`.

Volume is the bulk of the work here; each class is mechanical.

## 9. Documentation

`docs/flowboard-prd.md`:

- §11 — add the seven routes. `PATCH /work-items/{id}/component` is missing from the document entirely.
- Line 532 — correct `409 Conflict` to `400 Bad Request` for a `Completed`/`Archived` target milestone, per decision 8.
- Line 535 — already states the 404-for-non-members rule; the code now matches it.

`CLAUDE.md` — the "Work item board response" section is unaffected, but the WorkItems row of the aggregate table stays accurate.

## 10. Out of scope

- Optimistic concurrency (`IsRowVersion` / `xmin`) on `WorkItem`.
- Old-value/new-value columns on `WorkItemChangeLog` — no diff capability exists today and none is added.
- Changing `202 Accepted` to `204 No Content` across command endpoints.
- Multi-field partial updates.

## 11. Verification

```bash
dotnet build "Aurora Flowboard.slnx"
./test/Aurora.Flowboard.Domain.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Domain.UnitTests.exe
./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests.exe
```

`dotnet test` does not work on this solution — the .NET 10 SDK dropped the VSTest bridge that Microsoft.Testing.Platform needs here. Run the built test executables directly.

Application unit tests stand at 556 passing before this change.
