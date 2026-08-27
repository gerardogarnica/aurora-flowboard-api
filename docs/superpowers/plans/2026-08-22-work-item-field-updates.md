# Work Item Field Updates Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the coarse `PUT work-items/{id}` with seven per-field `PATCH` endpoints so the front-end's inline autosave controls can update description, type, priority, estimated points, estimated completion date, component and milestone one field at a time.

**Architecture:** Each field gets its own route, command, validator and handler. The field name lives in the URL, so a `null` body value unambiguously means "clear this field". Every handler derives from one abstract base that owns the load/authorise/save cycle. Each domain method records its own `WorkItemChangeType` entry and short-circuits when the value is unchanged.

**Tech Stack:** .NET 10, ASP.NET Core Minimal APIs, EF Core + Npgsql, FluentValidation, Scrutor, xUnit v3 + NSubstitute + FluentAssertions.

**Spec:** `docs/superpowers/specs/2026-08-22-work-item-field-updates-design.md`

## Global Constraints

- Solution file is `Aurora Flowboard.slnx`. Build with `dotnet build "Aurora Flowboard.slnx"`.
- **`dotnet test` does not work here.** The .NET 10 SDK dropped the VSTest bridge that Microsoft.Testing.Platform needs. Run the built executables directly:
  - `./test/Aurora.Flowboard.Domain.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Domain.UnitTests.exe`
  - `./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests.exe`
  - Filter a single class with `--filter-class "Aurora.Flowboard.Domain.UnitTests.WorkItems.WorkItemTests+UpdateDescription"`.
- `Directory.Build.props` treats **warnings as errors** and enables SonarAnalyzer. An unused `using` or an unused private field fails the build.
- `Directory.Packages.props` manages all NuGet versions centrally. Never add a `Version` attribute to a `.csproj`.
- `.editorconfig`: file-scoped namespaces; no `var` for built-in types (`var` is allowed when the type is apparent from the right-hand side); no `this.`; namespace must match folder structure; no magic numbers or strings.
- Always pass `CancellationToken` through async calls. No sync-over-async. No `Task.Run` in handlers.
- Do not introduce new architectural layers or new frameworks.
- Application unit tests stand at **556 passing** before this work. Domain unit tests must also stay green.
- Commit after every task. Branch is `feature/milestones-components`.

## File Structure

**Domain** (`src/Aurora.Flowboard.Domain/WorkItems/`)
- `WorkItem.cs` — gains `EnsureCanBeModifiedBy` plus seven update methods; loses `Update(...)`.
- `WorkItemChangeType.cs` — gains seven enum values.
- `WorkItemErrors.cs` — gains `EstimatedPointsInvalid`; loses `UserNotProjectMember`.

**Application** (`src/Aurora.Flowboard.Application/WorkItems/`)
- `Shared/WorkItemFieldUpdateHandler.cs` — new abstract base handler.
- `UpdateDescription/`, `UpdateType/`, `UpdatePriority/`, `UpdateEstimatedPoints/`, `UpdateEstimatedCompletionDate/`, `ChangeComponent/`, `ChangeMilestone/` — three files each.
- `UpdateTitle/UpdateWorkItemTitleHandler.cs` — rewritten onto the base.
- `Update/` — deleted.

**Api** (`src/Aurora.Flowboard.Api/Endpoints/WorkItems/`)
- Seven new endpoint files; `UpdateWorkItem.cs` deleted.

**Tests**
- `test/Aurora.Flowboard.Domain.UnitTests/WorkItems/WorkItemTests.cs` — nested class per new method.
- `test/Aurora.Flowboard.Application.UnitTests/WorkItems/` — handler + validator test class per field.

---

### Task 1: Delete the Update use case

Removing this first means Task 2 has fewer files to touch.

**Files:**
- Delete: `src/Aurora.Flowboard.Application/WorkItems/Update/UpdateWorkItemCommand.cs`
- Delete: `src/Aurora.Flowboard.Application/WorkItems/Update/UpdateWorkItemHandler.cs`
- Delete: `src/Aurora.Flowboard.Application/WorkItems/Update/UpdateWorkItemValidator.cs`
- Delete: `src/Aurora.Flowboard.Api/Endpoints/WorkItems/UpdateWorkItem.cs`
- Delete: `test/Aurora.Flowboard.Application.UnitTests/WorkItems/UpdateWorkItemHandlerTests.cs`
- Delete: `test/Aurora.Flowboard.Application.UnitTests/WorkItems/UpdateWorkItemValidatorTests.cs`
- Modify: `src/Aurora.Flowboard.Domain/WorkItems/WorkItem.cs:215-264` (remove `Update`)
- Modify: `test/Aurora.Flowboard.Application.UnitTests/GlobalUsings.cs:39`
- Modify: `test/Aurora.Flowboard.Domain.UnitTests/WorkItems/WorkItemTests.cs:383-531` (remove the `Update` nested class)

**Interfaces:**
- Consumes: nothing.
- Produces: nothing. This task only removes code.

- [ ] **Step 1: Delete the Application slice, the endpoint and their tests**

```bash
cd "C:/SourcesGG/aurora-flowboard-api"
rm -r src/Aurora.Flowboard.Application/WorkItems/Update
rm src/Aurora.Flowboard.Api/Endpoints/WorkItems/UpdateWorkItem.cs
rm test/Aurora.Flowboard.Application.UnitTests/WorkItems/UpdateWorkItemHandlerTests.cs
rm test/Aurora.Flowboard.Application.UnitTests/WorkItems/UpdateWorkItemValidatorTests.cs
```

- [ ] **Step 2: Remove the now-dangling global using**

In `test/Aurora.Flowboard.Application.UnitTests/GlobalUsings.cs`, delete this line:

```csharp
global using Aurora.Flowboard.Application.WorkItems.Update;
```

Leave `global using Aurora.Flowboard.Application.Projects.Update;` and `global using Aurora.Flowboard.Application.Milestones.Update;` alone — those are different features.

- [ ] **Step 3: Remove the `Update` method from the domain**

In `src/Aurora.Flowboard.Domain/WorkItems/WorkItem.cs`, delete the whole `public Result Update(...)` method (currently lines 215-264, starting `public Result Update(` and ending at the closing brace before `public Result UpdateTitle`).

- [ ] **Step 4: Remove the `Update` domain tests**

In `test/Aurora.Flowboard.Domain.UnitTests/WorkItems/WorkItemTests.cs`, delete the entire `public sealed class Update : BaseTest { ... }` nested class (currently lines 383-531). Keep `UpdateTitle` immediately after it.

- [ ] **Step 5: Build and run both test suites**

```bash
dotnet build "Aurora Flowboard.slnx"
./test/Aurora.Flowboard.Domain.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Domain.UnitTests.exe
./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests.exe
```

Expected: build succeeds with 0 warnings, both suites pass. Application total drops below 556 because two test classes were removed — that is correct.

If the build complains about an unused `using Aurora.Flowboard.Domain.WorkItems;` anywhere, remove that `using`; `Priority` was only referenced by the deleted endpoint.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "Remove the coarse PUT work-items/{id} update use case"
```

---

### Task 2: Extract EnsureCanBeModifiedBy and return 404 for non-members

**Files:**
- Modify: `src/Aurora.Flowboard.Domain/WorkItems/WorkItem.cs`
- Modify: `src/Aurora.Flowboard.Domain/WorkItems/WorkItemErrors.cs:21-23`
- Modify: `test/Aurora.Flowboard.Domain.UnitTests/WorkItems/WorkItemTests.cs` (12 assertions)
- Modify: `test/Aurora.Flowboard.Application.UnitTests/WorkItems/AddWorkItemCommentHandlerTests.cs:164`
- Modify: `test/Aurora.Flowboard.Application.UnitTests/WorkItems/UpdateWorkItemTitleHandlerTests.cs:164`
- Modify: `test/Aurora.Flowboard.Application.UnitTests/WorkItems/LogWorkItemTimeHandlerTests.cs:164`

**Interfaces:**
- Consumes: nothing.
- Produces: `private Result EnsureCanBeModifiedBy(User changedBy)` on `WorkItem` — Tasks 4-8 call it as the first or second statement of every new update method.

- [ ] **Step 1: Update the failing test assertions first**

A non-member must now be indistinguishable from a missing work item. In `test/Aurora.Flowboard.Domain.UnitTests/WorkItems/WorkItemTests.cs`, replace **every** occurrence of:

```csharp
result.Error.Should().Be(WorkItemErrors.UserNotProjectMember);
```

with:

```csharp
result.Error.Should().Be(WorkItemErrors.NotFound);
```

There are 12 of them after Task 1 removed the `Update` class. Do the same in these three Application test files, each at line 164:
- `AddWorkItemCommentHandlerTests.cs`
- `UpdateWorkItemTitleHandlerTests.cs`
- `LogWorkItemTimeHandlerTests.cs`

Leave `AssigneeNotProjectMember` assertions untouched — that is a different error about the assignee, not the caller, and it stays a 400.

- [ ] **Step 2: Run the tests to verify they fail**

```bash
dotnet build "Aurora Flowboard.slnx"
./test/Aurora.Flowboard.Domain.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Domain.UnitTests.exe
```

Expected: FAIL. Assertions report the actual error is `WorkItem.UserNotProjectMember` while `WorkItem.NotFound` was expected.

- [ ] **Step 3: Delete the error**

In `src/Aurora.Flowboard.Domain/WorkItems/WorkItemErrors.cs`, delete:

```csharp
    public static readonly BaseError UserNotProjectMember = BaseError.Forbidden(
        "WorkItem.UserNotProjectMember",
        "Only project members can perform this operation on a work item");
```

Flipping its type to `NotFound` would not be enough: `ApiResponses.Problem` puts `Error.Code` into the response `title` and `Error.Message` into `detail`, so the old code and message would still disclose that the work item exists.

- [ ] **Step 4: Add the private guard method**

In `src/Aurora.Flowboard.Domain/WorkItems/WorkItem.cs`, add this as the **last member of the class**, after `RemoveTag`:

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

- [ ] **Step 5: Replace the guard block in the six compatible methods**

In `UpdateTitle`, `Move`, `AddComment`, `LogTime`, `AddTag` and `RemoveTag`, replace this three-check block:

```csharp
        if (!Project.CanAddOrUpdateWorkItem())
        {
            return Result.Fail<WorkItem>(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        if (!Project.IsMember(changedBy.Id))
        {
            return Result.Fail(WorkItemErrors.UserNotProjectMember);
        }

        if (!changedBy.IsActive)
        {
            return Result.Fail(UserErrors.Inactive);
        }
```

with:

```csharp
        Result guardResult = EnsureCanBeModifiedBy(changedBy);

        if (!guardResult.IsSuccessful)
        {
            return guardResult;
        }
```

Keep the replacement in exactly the same position the block occupied, so the order in which errors surface does not change. In `LogTime` the parameter is named `user`, not `changedBy` — call `EnsureCanBeModifiedBy(user)` there.

**Do NOT touch these four methods** — their guards are ordered differently and extracting would change which error wins:
- `Assign` — interleaves `IsMember(assignee)` and the `Cancelled` check between the guards.
- `Unassign` — checks `AssigneeId` before membership.
- `UpdateComment` and `RemoveComment` — never check `IsActive`.

In those four, only swap the error constant: `WorkItemErrors.UserNotProjectMember` becomes `WorkItemErrors.NotFound`.

- [ ] **Step 6: Fix the static `Create` method**

`Create` is static, so it cannot call the instance guard, and its checks run in a different order (`IsMember` before `CanAddOrUpdateWorkItem`). Leave the structure alone and change only line 117:

```csharp
            return Result.Fail<WorkItem>(WorkItemErrors.NotFound);
```

This means `POST work-items` now returns 404 instead of 403 when the caller is not a member of the target project. That is intended.

- [ ] **Step 7: Run both suites to verify they pass**

```bash
dotnet build "Aurora Flowboard.slnx"
./test/Aurora.Flowboard.Domain.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Domain.UnitTests.exe
./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests.exe
```

Expected: PASS, 0 warnings. If the compiler still reports `UserNotProjectMember` as used, a call site was missed — search for it and convert it.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "Return 404 for non-members and extract EnsureCanBeModifiedBy guard"
```

---

### Task 3: Add the base handler and migrate UpdateTitle onto it

Migrating an already-tested feature first proves the base works before any new field depends on it.

**Files:**
- Create: `src/Aurora.Flowboard.Application/WorkItems/Shared/WorkItemFieldUpdateHandler.cs`
- Modify: `src/Aurora.Flowboard.Application/GlobalUsings.cs`
- Modify: `src/Aurora.Flowboard.Application/WorkItems/UpdateTitle/UpdateWorkItemTitleHandler.cs`
- Test: `test/Aurora.Flowboard.Application.UnitTests/WorkItems/UpdateWorkItemTitleHandlerTests.cs` (unchanged — it is the regression gate)

**Interfaces:**
- Consumes: `WorkItem.EnsureCanBeModifiedBy` indirectly via the domain methods.
- Produces:
  - `internal abstract class WorkItemFieldUpdateHandler<TCommand> : ICommandHandler<TCommand> where TCommand : ICommand`
  - `protected IApplicationDbContext DbContext { get; }`
  - `protected abstract Guid GetWorkItemId(TCommand command)`
  - `protected abstract Task<Result> ApplyAsync(WorkItem workItem, TCommand command, User changedBy, DateTime utcNow, CancellationToken cancellationToken)`
  - Constructor signature every derived handler must forward: `(IApplicationDbContext dbContext, IDateTimeProvider dateTimeProvider, IUserContext userContext)`

- [ ] **Step 1: Create the base handler**

Create `src/Aurora.Flowboard.Application/WorkItems/Shared/WorkItemFieldUpdateHandler.cs`:

```csharp
namespace Aurora.Flowboard.Application.WorkItems.Shared;

internal abstract class WorkItemFieldUpdateHandler<TCommand>(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    protected IApplicationDbContext DbContext => dbContext;

    public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
    {
        Guid workItemId = GetWorkItemId(command);

        WorkItem? workItem = await dbContext
            .WorkItems
            .Include(w => w.Project)
            .ThenInclude(p => p.Members)
            .AsSplitQuery()
            .SingleOrDefaultAsync(w => w.Id == workItemId, cancellationToken);

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
            workItem,
            command,
            changedBy,
            dateTimeProvider.UtcNow,
            cancellationToken);

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

Scrutor's `AddClasses` skips abstract types, so this class is never registered in DI — only the concrete subclasses are, via `AsImplementedInterfaces()`.

- [ ] **Step 2: Make the namespace globally available**

Add to `src/Aurora.Flowboard.Application/GlobalUsings.cs`, keeping alphabetical order among the `Aurora.Flowboard.Application.*` entries:

```csharp
global using Aurora.Flowboard.Application.WorkItems.Shared;
```

- [ ] **Step 3: Rewrite the title handler onto the base**

Replace the whole contents of `src/Aurora.Flowboard.Application/WorkItems/UpdateTitle/UpdateWorkItemTitleHandler.cs`:

```csharp
namespace Aurora.Flowboard.Application.WorkItems.UpdateTitle;

internal sealed class UpdateWorkItemTitleHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : WorkItemFieldUpdateHandler<UpdateWorkItemTitleCommand>(dbContext, dateTimeProvider, userContext)
{
    protected override Guid GetWorkItemId(UpdateWorkItemTitleCommand command) => command.Id;

    protected override Task<Result> ApplyAsync(
        WorkItem workItem,
        UpdateWorkItemTitleCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        Task.FromResult(workItem.UpdateTitle(command.Title, changedBy, utcNow));
}
```

- [ ] **Step 4: Run the existing title tests to verify nothing regressed**

```bash
dotnet build "Aurora Flowboard.slnx"
./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests.exe --filter-class "Aurora.Flowboard.Application.UnitTests.WorkItems.UpdateWorkItemTitleHandlerTests"
```

Expected: PASS, all 8 tests. These tests were written against the old hand-rolled handler and were not modified, so passing them proves the base reproduces the previous behaviour exactly.

- [ ] **Step 5: Run the full Application suite**

```bash
./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests.exe
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "Add WorkItemFieldUpdateHandler base and migrate UpdateTitle onto it"
```

---

### Task 4: Description field

The first full vertical slice. It establishes the shape every later field copies: enum value, domain method, command, validator, handler, endpoint, tests.

**Files:**
- Modify: `src/Aurora.Flowboard.Domain/WorkItems/WorkItemChangeType.cs`
- Modify: `src/Aurora.Flowboard.Domain/WorkItems/WorkItem.cs`
- Create: `src/Aurora.Flowboard.Application/WorkItems/UpdateDescription/UpdateWorkItemDescriptionCommand.cs`
- Create: `src/Aurora.Flowboard.Application/WorkItems/UpdateDescription/UpdateWorkItemDescriptionValidator.cs`
- Create: `src/Aurora.Flowboard.Application/WorkItems/UpdateDescription/UpdateWorkItemDescriptionHandler.cs`
- Create: `src/Aurora.Flowboard.Api/Endpoints/WorkItems/UpdateWorkItemDescription.cs`
- Modify: `test/Aurora.Flowboard.Application.UnitTests/GlobalUsings.cs`
- Modify: `test/Aurora.Flowboard.Domain.UnitTests/WorkItems/WorkItemTests.cs`
- Create: `test/Aurora.Flowboard.Application.UnitTests/WorkItems/UpdateWorkItemDescriptionHandlerTests.cs`
- Create: `test/Aurora.Flowboard.Application.UnitTests/WorkItems/UpdateWorkItemDescriptionValidatorTests.cs`

**Interfaces:**
- Consumes: `WorkItem.EnsureCanBeModifiedBy` (Task 2), `WorkItemFieldUpdateHandler<TCommand>` (Task 3).
- Produces:
  - `WorkItemChangeType.DescriptionUpdated = 12`
  - `public Result UpdateDescription(string? description, User changedBy, DateTime updatedOnUtc)` on `WorkItem`
  - `public sealed record UpdateWorkItemDescriptionCommand(Guid Id, string? Description) : ICommand`

- [ ] **Step 1: Write the failing domain test**

In `test/Aurora.Flowboard.Domain.UnitTests/WorkItems/WorkItemTests.cs`, add this nested class immediately after the `UpdateTitle` class:

```csharp
    public sealed class UpdateDescription : BaseTest
    {
        [Fact]
        public void Should_UpdateDescription_When_DataIsValid()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();
            const string newDescription = "Updated description";

            // Act
            Result result = workItem.UpdateDescription(newDescription, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.Description.Should().Be(newDescription);
            workItem.UpdatedOnUtc.Should().Be(WorkItemData.UpdatedOnUtc);
        }

        [Fact]
        public void Should_TrimDescription_When_Updated()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            workItem.UpdateDescription("  New description  ", admin, WorkItemData.UpdatedOnUtc);

            // Assert
            workItem.Description.Should().Be("New description");
        }

        [Fact]
        public void Should_ClearDescription_When_NullIsProvided()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.UpdateDescription(null, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.Description.Should().BeNull();
        }

        [Fact]
        public void Should_CreateDescriptionUpdatedChangeLog_When_Updated()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            workItem.UpdateDescription("New description", admin, WorkItemData.UpdatedOnUtc);

            // Assert
            workItem.ChangeLogs.Should().Contain(c => c.ChangeType == WorkItemChangeType.DescriptionUpdated);
        }

        [Fact]
        public void Should_BeNoOp_When_DescriptionIsUnchanged()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.UpdateDescription(WorkItemData.Description, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.UpdatedOnUtc.Should().BeNull();
            workItem.ChangeLogs.Should().NotContain(c => c.ChangeType == WorkItemChangeType.DescriptionUpdated);
        }

        [Fact]
        public void Should_Fail_When_DescriptionExceedsMaxLength()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();
            string longDescription = new('A', WorkItem.MaxDescriptionLength + 1);

            // Act
            Result result = workItem.UpdateDescription(longDescription, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.DescriptionTooLong);
        }

        [Fact]
        public void Should_Fail_When_ChangedByIsNotProjectMember()
        {
            // Arrange
            var (workItem, _, _) = WorkItemData.GetWorkItemWithContext();
            User nonMember = UserData.GetActiveUser();

            // Act
            Result result = workItem.UpdateDescription("New description", nonMember, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.NotFound);
        }

        [Fact]
        public void Should_Fail_When_ChangedByIsInactive()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            User user = UserData.GetActiveUser();
            project.AddMember(user, ProjectRole.Developer, admin, WorkItemData.CreatedOnUtc);
            user.Deactivate(WorkItemData.CreatedOnUtc);

            // Act
            Result result = workItem.UpdateDescription("New description", user, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(UserErrors.Inactive);
        }

        [Fact]
        public void Should_Fail_When_ProjectDoesNotAllowWorkItems()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            project.ChangeStatus(ProjectStatus.Archived, admin, WorkItemData.UpdatedOnUtc);

            // Act
            Result result = workItem.UpdateDescription("New description", admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }
    }
```

- [ ] **Step 2: Run it to verify it fails**

```bash
dotnet build "Aurora Flowboard.slnx"
```

Expected: FAIL to compile — `'WorkItem' does not contain a definition for 'UpdateDescription'` and `'WorkItemChangeType' does not contain a definition for 'DescriptionUpdated'`.

- [ ] **Step 3: Add the enum value**

Replace `src/Aurora.Flowboard.Domain/WorkItems/WorkItemChangeType.cs` with:

```csharp
namespace Aurora.Flowboard.Domain.WorkItems;

public enum WorkItemChangeType
{
    Created = 0,
    Updated = 1, // legacy — no longer written; historical rows only
    Moved = 2,
    Assigned = 3,
    Unassigned = 4,
    CommentAdded = 5,
    CommentUpdated = 6,
    CommentRemoved = 7,
    TimeLogged = 8,
    TagAdded = 9,
    TagRemoved = 10,
    TitleUpdated = 11,
    DescriptionUpdated = 12
}
```

Values are persisted as ints, so appending needs no migration. `Updated = 1` must stay: existing rows in `flowboard.work_item_change_logs` hold it.

- [ ] **Step 4: Add the domain method**

In `src/Aurora.Flowboard.Domain/WorkItems/WorkItem.cs`, add immediately after `UpdateTitle`:

```csharp
    public Result UpdateDescription(string? description, User changedBy, DateTime updatedOnUtc)
    {
        if (description?.Length > MaxDescriptionLength)
        {
            return Result.Fail(WorkItemErrors.DescriptionTooLong);
        }

        Result guardResult = EnsureCanBeModifiedBy(changedBy);

        if (!guardResult.IsSuccessful)
        {
            return guardResult;
        }

        string? trimmedDescription = description?.Trim();

        if (trimmedDescription == Description)
        {
            return Result.Ok();
        }

        Description = trimmedDescription;
        UpdatedOnUtc = updatedOnUtc;

        _changeLogs.Add(WorkItemChangeLog.Create(this, changedBy, WorkItemChangeType.DescriptionUpdated, null, updatedOnUtc));

        return Result.Ok();
    }
```

Field validation runs before the guards, matching `UpdateTitle`. The no-op short-circuit is mandatory: inline controls fire on every blur, and without it the changelog fills with meaningless entries.

- [ ] **Step 5: Run the domain tests to verify they pass**

```bash
dotnet build "Aurora Flowboard.slnx"
./test/Aurora.Flowboard.Domain.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Domain.UnitTests.exe --filter-class "Aurora.Flowboard.Domain.UnitTests.WorkItems.WorkItemTests+UpdateDescription"
```

Expected: PASS, 9 tests.

- [ ] **Step 6: Write the failing Application tests**

Create `test/Aurora.Flowboard.Application.UnitTests/WorkItems/UpdateWorkItemDescriptionValidatorTests.cs`:

```csharp
namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class UpdateWorkItemDescriptionValidatorTests
{
    private readonly UpdateWorkItemDescriptionValidator _validator = new();

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        UpdateWorkItemDescriptionCommand command = new(Guid.NewGuid(), "A new description");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Pass_When_DescriptionIsNull()
    {
        UpdateWorkItemDescriptionCommand command = new(Guid.NewGuid(), null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_IdIsEmpty()
    {
        UpdateWorkItemDescriptionCommand command = new(Guid.Empty, "A new description");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_DescriptionExceedsMaxLength()
    {
        string longDescription = new('A', WorkItem.MaxDescriptionLength + 1);
        UpdateWorkItemDescriptionCommand command = new(Guid.NewGuid(), longDescription);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
```

Create `test/Aurora.Flowboard.Application.UnitTests/WorkItems/UpdateWorkItemDescriptionHandlerTests.cs`:

```csharp
namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class UpdateWorkItemDescriptionHandlerTests
{
    private const string NewDescription = "Updated description";

    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUserContext _userContext;
    private readonly UpdateWorkItemDescriptionHandler _handler;

    public UpdateWorkItemDescriptionHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _userContext = Substitute.For<IUserContext>();
        _handler = new UpdateWorkItemDescriptionHandler(_dbContext, _dateTimeProvider, _userContext);
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_CommandIsValid()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        WorkItem workItem = WorkItemCommandData.GetWorkItem(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(WorkItemCommandData.UtcNow);

        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);

        UpdateWorkItemDescriptionCommand command = new(workItem.Id, NewDescription);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        workItem.Description.Should().Be(NewDescription);
    }

    [Fact]
    public async Task Should_PersistChanges_When_CommandIsValid()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        WorkItem workItem = WorkItemCommandData.GetWorkItem(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(WorkItemCommandData.UtcNow);

        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);

        UpdateWorkItemDescriptionCommand command = new(workItem.Id, NewDescription);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnWorkItemNotFoundError_When_WorkItemDoesNotExist()
    {
        // Arrange
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<WorkItem>());
        _dbContext.WorkItems.Returns(workItemsMock);

        UpdateWorkItemDescriptionCommand command = new(Guid.NewGuid(), NewDescription);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(WorkItemErrors.NotFound);
    }

    [Fact]
    public async Task Should_ReturnUserNotFoundError_When_UserDoesNotExist()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        WorkItem workItem = WorkItemCommandData.GetWorkItem(admin);
        _userContext.UserId.Returns(Guid.NewGuid());

        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<User>());
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);

        UpdateWorkItemDescriptionCommand command = new(workItem.Id, NewDescription);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(UserErrors.NotFound);
    }

    [Fact]
    public async Task Should_ReturnNotFoundError_When_UserIsNotProjectMember()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        WorkItem workItem = WorkItemCommandData.GetWorkItem(admin);
        User nonMember = WorkItemCommandData.GetNonMember();
        _userContext.UserId.Returns(nonMember.Id);
        _dateTimeProvider.UtcNow.Returns(WorkItemCommandData.UtcNow);

        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([nonMember]);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);

        UpdateWorkItemDescriptionCommand command = new(workItem.Id, NewDescription);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(WorkItemErrors.NotFound);
    }

    [Fact]
    public async Task Should_NotPersist_When_UserIsNotProjectMember()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        WorkItem workItem = WorkItemCommandData.GetWorkItem(admin);
        User nonMember = WorkItemCommandData.GetNonMember();
        _userContext.UserId.Returns(nonMember.Id);
        _dateTimeProvider.UtcNow.Returns(WorkItemCommandData.UtcNow);

        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([nonMember]);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);

        UpdateWorkItemDescriptionCommand command = new(workItem.Id, NewDescription);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
```

Add the namespace to `test/Aurora.Flowboard.Application.UnitTests/GlobalUsings.cs`, alphabetically among the `WorkItems.*` entries:

```csharp
global using Aurora.Flowboard.Application.WorkItems.UpdateDescription;
```

- [ ] **Step 7: Run to verify they fail**

```bash
dotnet build "Aurora Flowboard.slnx"
```

Expected: FAIL to compile — `UpdateWorkItemDescriptionCommand`, `UpdateWorkItemDescriptionValidator` and `UpdateWorkItemDescriptionHandler` do not exist.

- [ ] **Step 8: Write the Application slice**

Create `src/Aurora.Flowboard.Application/WorkItems/UpdateDescription/UpdateWorkItemDescriptionCommand.cs`:

```csharp
namespace Aurora.Flowboard.Application.WorkItems.UpdateDescription;

public sealed record UpdateWorkItemDescriptionCommand(Guid Id, string? Description) : ICommand;
```

Create `src/Aurora.Flowboard.Application/WorkItems/UpdateDescription/UpdateWorkItemDescriptionValidator.cs`:

```csharp
namespace Aurora.Flowboard.Application.WorkItems.UpdateDescription;

internal sealed class UpdateWorkItemDescriptionValidator : AbstractValidator<UpdateWorkItemDescriptionCommand>
{
    public UpdateWorkItemDescriptionValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Description)
            .MaximumLength(WorkItem.MaxDescriptionLength)
            .When(x => x.Description is not null);
    }
}
```

Create `src/Aurora.Flowboard.Application/WorkItems/UpdateDescription/UpdateWorkItemDescriptionHandler.cs`:

```csharp
namespace Aurora.Flowboard.Application.WorkItems.UpdateDescription;

internal sealed class UpdateWorkItemDescriptionHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : WorkItemFieldUpdateHandler<UpdateWorkItemDescriptionCommand>(dbContext, dateTimeProvider, userContext)
{
    protected override Guid GetWorkItemId(UpdateWorkItemDescriptionCommand command) => command.Id;

    protected override Task<Result> ApplyAsync(
        WorkItem workItem,
        UpdateWorkItemDescriptionCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        Task.FromResult(workItem.UpdateDescription(command.Description, changedBy, utcNow));
}
```

- [ ] **Step 9: Run the Application tests to verify they pass**

```bash
dotnet build "Aurora Flowboard.slnx"
./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests.exe
```

Expected: PASS.

- [ ] **Step 10: Add the endpoint**

Create `src/Aurora.Flowboard.Api/Endpoints/WorkItems/UpdateWorkItemDescription.cs`:

```csharp
using Aurora.Flowboard.Application.WorkItems.UpdateDescription;

namespace Aurora.Flowboard.Api.Endpoints.WorkItems;

public sealed class UpdateWorkItemDescription : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch(
            "work-items/{id:guid}/description",
            async (
                Guid id,
                [FromBody] UpdateWorkItemDescriptionRequest request,
                ICommandHandler<UpdateWorkItemDescriptionCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateWorkItemDescriptionCommand(id, request.Description);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    () => Results.Accepted(string.Empty),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("UpdateWorkItemDescription")
            .WithTags(EndpointTags.WorkItems)
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record UpdateWorkItemDescriptionRequest(string? Description);
}
```

No `403` is declared: the only reachable failures are `ProjectErrors.OperationNotAllowedInCurrentStatus` and `UserErrors.Inactive` (both `Validation` → 400), the validator (400), and the `NotFound` errors (404).

- [ ] **Step 11: Build and run everything**

```bash
dotnet build "Aurora Flowboard.slnx"
./test/Aurora.Flowboard.Domain.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Domain.UnitTests.exe
./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests.exe
```

Expected: build clean with 0 warnings, both suites pass.

- [ ] **Step 12: Commit**

```bash
git add -A
git commit -m "Add PATCH work-items/{id}/description endpoint"
```

---

### Task 5: Type and Priority fields

Two enum fields with no validation beyond the shared guards, so they ship together.

**A note on test coverage from here on.** Task 4 tested all three guard failures (non-member, inactive user, archived project) against `UpdateDescription`, and Task 2 tested `EnsureCanBeModifiedBy` through the six pre-existing methods. Every method from here calls that same private method, so re-testing all three guards per field would be redundant coverage of one code path. Each remaining task keeps **one** non-member test — enough to prove the guard is actually invoked — plus the tests for what is genuinely different about that field.

**Files:**
- Modify: `src/Aurora.Flowboard.Domain/WorkItems/WorkItemChangeType.cs`
- Modify: `src/Aurora.Flowboard.Domain/WorkItems/WorkItem.cs`
- Create: `src/Aurora.Flowboard.Application/WorkItems/UpdateType/UpdateWorkItemTypeCommand.cs`
- Create: `src/Aurora.Flowboard.Application/WorkItems/UpdateType/UpdateWorkItemTypeValidator.cs`
- Create: `src/Aurora.Flowboard.Application/WorkItems/UpdateType/UpdateWorkItemTypeHandler.cs`
- Create: `src/Aurora.Flowboard.Application/WorkItems/UpdatePriority/UpdateWorkItemPriorityCommand.cs`
- Create: `src/Aurora.Flowboard.Application/WorkItems/UpdatePriority/UpdateWorkItemPriorityValidator.cs`
- Create: `src/Aurora.Flowboard.Application/WorkItems/UpdatePriority/UpdateWorkItemPriorityHandler.cs`
- Create: `src/Aurora.Flowboard.Api/Endpoints/WorkItems/UpdateWorkItemType.cs`
- Create: `src/Aurora.Flowboard.Api/Endpoints/WorkItems/UpdateWorkItemPriority.cs`
- Modify: `test/Aurora.Flowboard.Application.UnitTests/GlobalUsings.cs`
- Modify: `test/Aurora.Flowboard.Domain.UnitTests/WorkItems/WorkItemTests.cs`
- Create: `test/Aurora.Flowboard.Application.UnitTests/WorkItems/UpdateWorkItemTypeHandlerTests.cs`
- Create: `test/Aurora.Flowboard.Application.UnitTests/WorkItems/UpdateWorkItemTypeValidatorTests.cs`
- Create: `test/Aurora.Flowboard.Application.UnitTests/WorkItems/UpdateWorkItemPriorityValidatorTests.cs`

**Interfaces:**
- Consumes: `WorkItem.EnsureCanBeModifiedBy` (Task 2), `WorkItemFieldUpdateHandler<TCommand>` (Task 3).
- Produces:
  - `WorkItemChangeType.TypeUpdated = 13`, `WorkItemChangeType.PriorityUpdated = 14`
  - `public Result UpdateType(WorkItemType type, User changedBy, DateTime updatedOnUtc)`
  - `public Result UpdatePriority(Priority priority, User changedBy, DateTime updatedOnUtc)`
  - `public sealed record UpdateWorkItemTypeCommand(Guid Id, WorkItemType Type) : ICommand`
  - `public sealed record UpdateWorkItemPriorityCommand(Guid Id, Priority Priority) : ICommand`

- [ ] **Step 1: Write the failing domain tests**

In `test/Aurora.Flowboard.Domain.UnitTests/WorkItems/WorkItemTests.cs`, add both nested classes after `UpdateDescription`.

`WorkItemData.Type` is `WorkItemType.Story` and `WorkItemData.Priority` is `Priority.Medium`, so the tests below change to a genuinely different value.

```csharp
    public sealed class UpdateType : BaseTest
    {
        [Fact]
        public void Should_UpdateType_When_DataIsValid()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.UpdateType(WorkItemType.Bug, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.Type.Should().Be(WorkItemType.Bug);
            workItem.UpdatedOnUtc.Should().Be(WorkItemData.UpdatedOnUtc);
        }

        [Fact]
        public void Should_CreateTypeUpdatedChangeLog_When_Updated()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            workItem.UpdateType(WorkItemType.Bug, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            workItem.ChangeLogs.Should().Contain(c => c.ChangeType == WorkItemChangeType.TypeUpdated);
        }

        [Fact]
        public void Should_BeNoOp_When_TypeIsUnchanged()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.UpdateType(WorkItemData.Type, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.UpdatedOnUtc.Should().BeNull();
            workItem.ChangeLogs.Should().NotContain(c => c.ChangeType == WorkItemChangeType.TypeUpdated);
        }

        [Fact]
        public void Should_Fail_When_ChangedByIsNotProjectMember()
        {
            // Arrange
            var (workItem, _, _) = WorkItemData.GetWorkItemWithContext();
            User nonMember = UserData.GetActiveUser();

            // Act
            Result result = workItem.UpdateType(WorkItemType.Bug, nonMember, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.NotFound);
        }
    }

    public sealed class UpdatePriority : BaseTest
    {
        [Fact]
        public void Should_UpdatePriority_When_DataIsValid()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.UpdatePriority(Priority.High, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.Priority.Should().Be(Priority.High);
            workItem.UpdatedOnUtc.Should().Be(WorkItemData.UpdatedOnUtc);
        }

        [Fact]
        public void Should_CreatePriorityUpdatedChangeLog_When_Updated()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            workItem.UpdatePriority(Priority.High, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            workItem.ChangeLogs.Should().Contain(c => c.ChangeType == WorkItemChangeType.PriorityUpdated);
        }

        [Fact]
        public void Should_BeNoOp_When_PriorityIsUnchanged()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.UpdatePriority(WorkItemData.Priority, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.UpdatedOnUtc.Should().BeNull();
            workItem.ChangeLogs.Should().NotContain(c => c.ChangeType == WorkItemChangeType.PriorityUpdated);
        }

        [Fact]
        public void Should_Fail_When_ChangedByIsNotProjectMember()
        {
            // Arrange
            var (workItem, _, _) = WorkItemData.GetWorkItemWithContext();
            User nonMember = UserData.GetActiveUser();

            // Act
            Result result = workItem.UpdatePriority(Priority.High, nonMember, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.NotFound);
        }
    }
```

`WorkItemType` is `Story, Bug, TechnicalTask, Investigation` and `Priority` is `Low, Medium, High, Critical`, so `Bug` and `High` are both genuinely different from the seeded values.

- [ ] **Step 2: Run to verify they fail**

```bash
dotnet build "Aurora Flowboard.slnx"
```

Expected: FAIL to compile — `UpdateType`, `UpdatePriority`, `TypeUpdated` and `PriorityUpdated` are undefined.

- [ ] **Step 3: Add the enum values**

In `src/Aurora.Flowboard.Domain/WorkItems/WorkItemChangeType.cs`, extend the list so it ends:

```csharp
    TitleUpdated = 11,
    DescriptionUpdated = 12,
    TypeUpdated = 13,
    PriorityUpdated = 14
}
```

- [ ] **Step 4: Add the domain methods**

In `src/Aurora.Flowboard.Domain/WorkItems/WorkItem.cs`, add after `UpdateDescription`:

```csharp
    public Result UpdateType(WorkItemType type, User changedBy, DateTime updatedOnUtc)
    {
        Result guardResult = EnsureCanBeModifiedBy(changedBy);

        if (!guardResult.IsSuccessful)
        {
            return guardResult;
        }

        if (type == Type)
        {
            return Result.Ok();
        }

        Type = type;
        UpdatedOnUtc = updatedOnUtc;

        _changeLogs.Add(WorkItemChangeLog.Create(this, changedBy, WorkItemChangeType.TypeUpdated, null, updatedOnUtc));

        return Result.Ok();
    }

    public Result UpdatePriority(Priority priority, User changedBy, DateTime updatedOnUtc)
    {
        Result guardResult = EnsureCanBeModifiedBy(changedBy);

        if (!guardResult.IsSuccessful)
        {
            return guardResult;
        }

        if (priority == Priority)
        {
            return Result.Ok();
        }

        Priority = priority;
        UpdatedOnUtc = updatedOnUtc;

        _changeLogs.Add(WorkItemChangeLog.Create(this, changedBy, WorkItemChangeType.PriorityUpdated, null, updatedOnUtc));

        return Result.Ok();
    }
```

- [ ] **Step 5: Run the domain tests to verify they pass**

```bash
dotnet build "Aurora Flowboard.slnx"
./test/Aurora.Flowboard.Domain.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Domain.UnitTests.exe
```

Expected: PASS.

- [ ] **Step 6: Write the failing Application tests**

Create `test/Aurora.Flowboard.Application.UnitTests/WorkItems/UpdateWorkItemTypeValidatorTests.cs`:

```csharp
namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class UpdateWorkItemTypeValidatorTests
{
    private const int UndefinedEnumValue = 999;

    private readonly UpdateWorkItemTypeValidator _validator = new();

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        UpdateWorkItemTypeCommand command = new(Guid.NewGuid(), WorkItemType.Bug);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_IdIsEmpty()
    {
        UpdateWorkItemTypeCommand command = new(Guid.Empty, WorkItemType.Bug);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_TypeIsNotDefined()
    {
        UpdateWorkItemTypeCommand command = new(Guid.NewGuid(), (WorkItemType)UndefinedEnumValue);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
```

Create `test/Aurora.Flowboard.Application.UnitTests/WorkItems/UpdateWorkItemPriorityValidatorTests.cs`:

```csharp
namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class UpdateWorkItemPriorityValidatorTests
{
    private const int UndefinedEnumValue = 999;

    private readonly UpdateWorkItemPriorityValidator _validator = new();

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        UpdateWorkItemPriorityCommand command = new(Guid.NewGuid(), Priority.High);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_IdIsEmpty()
    {
        UpdateWorkItemPriorityCommand command = new(Guid.Empty, Priority.High);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_PriorityIsNotDefined()
    {
        UpdateWorkItemPriorityCommand command = new(Guid.NewGuid(), (Priority)UndefinedEnumValue);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
```

Create `test/Aurora.Flowboard.Application.UnitTests/WorkItems/UpdateWorkItemTypeHandlerTests.cs`. One handler test class covers both fields' wiring, because both handlers derive from the same base and differ only in the domain call:

```csharp
namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class UpdateWorkItemTypeHandlerTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUserContext _userContext;
    private readonly UpdateWorkItemTypeHandler _handler;

    public UpdateWorkItemTypeHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _userContext = Substitute.For<IUserContext>();
        _handler = new UpdateWorkItemTypeHandler(_dbContext, _dateTimeProvider, _userContext);
    }

    [Fact]
    public async Task Should_UpdateTypeAndPersist_When_CommandIsValid()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        WorkItem workItem = WorkItemCommandData.GetWorkItem(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(WorkItemCommandData.UtcNow);

        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);

        UpdateWorkItemTypeCommand command = new(workItem.Id, WorkItemType.Bug);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        workItem.Type.Should().Be(WorkItemType.Bug);
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnWorkItemNotFoundError_When_WorkItemDoesNotExist()
    {
        // Arrange
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<WorkItem>());
        _dbContext.WorkItems.Returns(workItemsMock);

        UpdateWorkItemTypeCommand command = new(Guid.NewGuid(), WorkItemType.Bug);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(WorkItemErrors.NotFound);
    }

    [Fact]
    public async Task Should_ReturnNotFoundError_When_UserIsNotProjectMember()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        WorkItem workItem = WorkItemCommandData.GetWorkItem(admin);
        User nonMember = WorkItemCommandData.GetNonMember();
        _userContext.UserId.Returns(nonMember.Id);
        _dateTimeProvider.UtcNow.Returns(WorkItemCommandData.UtcNow);

        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([nonMember]);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);

        UpdateWorkItemTypeCommand command = new(workItem.Id, WorkItemType.Bug);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(WorkItemErrors.NotFound);
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
```

Add both namespaces to `test/Aurora.Flowboard.Application.UnitTests/GlobalUsings.cs`:

```csharp
global using Aurora.Flowboard.Application.WorkItems.UpdatePriority;
global using Aurora.Flowboard.Application.WorkItems.UpdateType;
```

- [ ] **Step 7: Run to verify they fail**

```bash
dotnet build "Aurora Flowboard.slnx"
```

Expected: FAIL to compile — the commands, validators and handlers do not exist.

- [ ] **Step 8: Write both Application slices**

Create `src/Aurora.Flowboard.Application/WorkItems/UpdateType/UpdateWorkItemTypeCommand.cs`:

```csharp
namespace Aurora.Flowboard.Application.WorkItems.UpdateType;

public sealed record UpdateWorkItemTypeCommand(Guid Id, WorkItemType Type) : ICommand;
```

Create `src/Aurora.Flowboard.Application/WorkItems/UpdateType/UpdateWorkItemTypeValidator.cs`:

```csharp
namespace Aurora.Flowboard.Application.WorkItems.UpdateType;

internal sealed class UpdateWorkItemTypeValidator : AbstractValidator<UpdateWorkItemTypeCommand>
{
    public UpdateWorkItemTypeValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Type)
            .IsInEnum();
    }
}
```

Create `src/Aurora.Flowboard.Application/WorkItems/UpdateType/UpdateWorkItemTypeHandler.cs`:

```csharp
namespace Aurora.Flowboard.Application.WorkItems.UpdateType;

internal sealed class UpdateWorkItemTypeHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : WorkItemFieldUpdateHandler<UpdateWorkItemTypeCommand>(dbContext, dateTimeProvider, userContext)
{
    protected override Guid GetWorkItemId(UpdateWorkItemTypeCommand command) => command.Id;

    protected override Task<Result> ApplyAsync(
        WorkItem workItem,
        UpdateWorkItemTypeCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        Task.FromResult(workItem.UpdateType(command.Type, changedBy, utcNow));
}
```

Create `src/Aurora.Flowboard.Application/WorkItems/UpdatePriority/UpdateWorkItemPriorityCommand.cs`:

```csharp
namespace Aurora.Flowboard.Application.WorkItems.UpdatePriority;

public sealed record UpdateWorkItemPriorityCommand(Guid Id, Priority Priority) : ICommand;
```

Create `src/Aurora.Flowboard.Application/WorkItems/UpdatePriority/UpdateWorkItemPriorityValidator.cs`:

```csharp
namespace Aurora.Flowboard.Application.WorkItems.UpdatePriority;

internal sealed class UpdateWorkItemPriorityValidator : AbstractValidator<UpdateWorkItemPriorityCommand>
{
    public UpdateWorkItemPriorityValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Priority)
            .IsInEnum();
    }
}
```

Create `src/Aurora.Flowboard.Application/WorkItems/UpdatePriority/UpdateWorkItemPriorityHandler.cs`:

```csharp
namespace Aurora.Flowboard.Application.WorkItems.UpdatePriority;

internal sealed class UpdateWorkItemPriorityHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : WorkItemFieldUpdateHandler<UpdateWorkItemPriorityCommand>(dbContext, dateTimeProvider, userContext)
{
    protected override Guid GetWorkItemId(UpdateWorkItemPriorityCommand command) => command.Id;

    protected override Task<Result> ApplyAsync(
        WorkItem workItem,
        UpdateWorkItemPriorityCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        Task.FromResult(workItem.UpdatePriority(command.Priority, changedBy, utcNow));
}
```

- [ ] **Step 9: Run the Application tests to verify they pass**

```bash
dotnet build "Aurora Flowboard.slnx"
./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests.exe
```

Expected: PASS.

- [ ] **Step 10: Add both endpoints**

Create `src/Aurora.Flowboard.Api/Endpoints/WorkItems/UpdateWorkItemType.cs`:

```csharp
using Aurora.Flowboard.Application.WorkItems.UpdateType;
using Aurora.Flowboard.Domain.WorkItems;

namespace Aurora.Flowboard.Api.Endpoints.WorkItems;

public sealed class UpdateWorkItemType : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch(
            "work-items/{id:guid}/type",
            async (
                Guid id,
                [FromBody] UpdateWorkItemTypeRequest request,
                ICommandHandler<UpdateWorkItemTypeCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateWorkItemTypeCommand(id, request.Type);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    () => Results.Accepted(string.Empty),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("UpdateWorkItemType")
            .WithTags(EndpointTags.WorkItems)
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record UpdateWorkItemTypeRequest(WorkItemType Type);
}
```

Create `src/Aurora.Flowboard.Api/Endpoints/WorkItems/UpdateWorkItemPriority.cs`:

```csharp
using Aurora.Flowboard.Application.WorkItems.UpdatePriority;
using Aurora.Flowboard.Domain.WorkItems;

namespace Aurora.Flowboard.Api.Endpoints.WorkItems;

public sealed class UpdateWorkItemPriority : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch(
            "work-items/{id:guid}/priority",
            async (
                Guid id,
                [FromBody] UpdateWorkItemPriorityRequest request,
                ICommandHandler<UpdateWorkItemPriorityCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateWorkItemPriorityCommand(id, request.Priority);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    () => Results.Accepted(string.Empty),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("UpdateWorkItemPriority")
            .WithTags(EndpointTags.WorkItems)
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record UpdateWorkItemPriorityRequest(Priority Priority);
}
```

Enums arrive and leave as strings — `JsonStringEnumConverter` is registered in `Api/DependencyInjection.cs:23`.

- [ ] **Step 11: Build and run everything**

```bash
dotnet build "Aurora Flowboard.slnx"
./test/Aurora.Flowboard.Domain.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Domain.UnitTests.exe
./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests.exe
```

Expected: build clean with 0 warnings, both suites pass.

- [ ] **Step 12: Commit**

```bash
git add -A
git commit -m "Add PATCH type and priority endpoints for work items"
```

---

### Task 6: Estimated points and estimated completion date

Both are nullable scalars that the UI must be able to clear. This task also adds the one new error the feature needs.

**Files:**
- Modify: `src/Aurora.Flowboard.Domain/WorkItems/WorkItemChangeType.cs`
- Modify: `src/Aurora.Flowboard.Domain/WorkItems/WorkItemErrors.cs`
- Modify: `src/Aurora.Flowboard.Domain/WorkItems/WorkItem.cs`
- Create: `src/Aurora.Flowboard.Application/WorkItems/UpdateEstimatedPoints/UpdateWorkItemEstimatedPointsCommand.cs`
- Create: `src/Aurora.Flowboard.Application/WorkItems/UpdateEstimatedPoints/UpdateWorkItemEstimatedPointsValidator.cs`
- Create: `src/Aurora.Flowboard.Application/WorkItems/UpdateEstimatedPoints/UpdateWorkItemEstimatedPointsHandler.cs`
- Create: `src/Aurora.Flowboard.Application/WorkItems/UpdateEstimatedCompletionDate/UpdateWorkItemEstimatedCompletionDateCommand.cs`
- Create: `src/Aurora.Flowboard.Application/WorkItems/UpdateEstimatedCompletionDate/UpdateWorkItemEstimatedCompletionDateValidator.cs`
- Create: `src/Aurora.Flowboard.Application/WorkItems/UpdateEstimatedCompletionDate/UpdateWorkItemEstimatedCompletionDateHandler.cs`
- Create: `src/Aurora.Flowboard.Api/Endpoints/WorkItems/UpdateWorkItemEstimatedPoints.cs`
- Create: `src/Aurora.Flowboard.Api/Endpoints/WorkItems/UpdateWorkItemEstimatedCompletionDate.cs`
- Modify: `test/Aurora.Flowboard.Application.UnitTests/GlobalUsings.cs`
- Modify: `test/Aurora.Flowboard.Domain.UnitTests/WorkItems/WorkItemTests.cs`
- Create: `test/Aurora.Flowboard.Application.UnitTests/WorkItems/UpdateWorkItemEstimatedPointsValidatorTests.cs`
- Create: `test/Aurora.Flowboard.Application.UnitTests/WorkItems/UpdateWorkItemEstimatedCompletionDateValidatorTests.cs`
- Create: `test/Aurora.Flowboard.Application.UnitTests/WorkItems/UpdateWorkItemEstimatedPointsHandlerTests.cs`

**Interfaces:**
- Consumes: `WorkItem.EnsureCanBeModifiedBy` (Task 2), `WorkItemFieldUpdateHandler<TCommand>` (Task 3).
- Produces:
  - `WorkItemChangeType.EstimatedPointsUpdated = 15`, `WorkItemChangeType.EstimatedCompletionDateUpdated = 16`
  - `WorkItemErrors.EstimatedPointsInvalid`
  - `public Result UpdateEstimatedPoints(int? estimatedPoints, User changedBy, DateTime updatedOnUtc)`
  - `public Result UpdateEstimatedCompletionDate(DateOnly? estimatedCompletionDate, User changedBy, DateTime updatedOnUtc)`
  - `public sealed record UpdateWorkItemEstimatedPointsCommand(Guid Id, int? EstimatedPoints) : ICommand`
  - `public sealed record UpdateWorkItemEstimatedCompletionDateCommand(Guid Id, DateOnly? EstimatedCompletionDate) : ICommand`

- [ ] **Step 1: Write the failing domain tests**

`WorkItemData.EstimatedPoints` is `5` and `WorkItemData.EstimatedCompletionDate` is `2026-06-30`. Add both nested classes after `UpdatePriority` in `test/Aurora.Flowboard.Domain.UnitTests/WorkItems/WorkItemTests.cs`:

```csharp
    public sealed class UpdateEstimatedPoints : BaseTest
    {
        private const int NewPoints = 8;

        [Fact]
        public void Should_UpdateEstimatedPoints_When_DataIsValid()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.UpdateEstimatedPoints(NewPoints, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.EstimatedPoints.Should().Be(NewPoints);
            workItem.UpdatedOnUtc.Should().Be(WorkItemData.UpdatedOnUtc);
        }

        [Fact]
        public void Should_ClearEstimatedPoints_When_NullIsProvided()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.UpdateEstimatedPoints(null, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.EstimatedPoints.Should().BeNull();
        }

        [Fact]
        public void Should_CreateEstimatedPointsUpdatedChangeLog_When_Updated()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            workItem.UpdateEstimatedPoints(NewPoints, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            workItem.ChangeLogs.Should().Contain(c => c.ChangeType == WorkItemChangeType.EstimatedPointsUpdated);
        }

        [Fact]
        public void Should_BeNoOp_When_EstimatedPointsIsUnchanged()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.UpdateEstimatedPoints(WorkItemData.EstimatedPoints, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.UpdatedOnUtc.Should().BeNull();
            workItem.ChangeLogs.Should().NotContain(c => c.ChangeType == WorkItemChangeType.EstimatedPointsUpdated);
        }

        [Fact]
        public void Should_Fail_When_EstimatedPointsIsZero()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.UpdateEstimatedPoints(0, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.EstimatedPointsInvalid);
        }

        [Fact]
        public void Should_Fail_When_EstimatedPointsIsNegative()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.UpdateEstimatedPoints(-1, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.EstimatedPointsInvalid);
        }

        [Fact]
        public void Should_Fail_When_ChangedByIsNotProjectMember()
        {
            // Arrange
            var (workItem, _, _) = WorkItemData.GetWorkItemWithContext();
            User nonMember = UserData.GetActiveUser();

            // Act
            Result result = workItem.UpdateEstimatedPoints(NewPoints, nonMember, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.NotFound);
        }
    }

    public sealed class UpdateEstimatedCompletionDate : BaseTest
    {
        private static readonly DateOnly NewDate = new(2026, 9, 30);

        [Fact]
        public void Should_UpdateEstimatedCompletionDate_When_DataIsValid()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.UpdateEstimatedCompletionDate(NewDate, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.EstimatedCompletionDate.Should().Be(NewDate);
            workItem.UpdatedOnUtc.Should().Be(WorkItemData.UpdatedOnUtc);
        }

        [Fact]
        public void Should_ClearEstimatedCompletionDate_When_NullIsProvided()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.UpdateEstimatedCompletionDate(null, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.EstimatedCompletionDate.Should().BeNull();
        }

        [Fact]
        public void Should_CreateEstimatedCompletionDateUpdatedChangeLog_When_Updated()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            workItem.UpdateEstimatedCompletionDate(NewDate, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            workItem.ChangeLogs.Should().Contain(c => c.ChangeType == WorkItemChangeType.EstimatedCompletionDateUpdated);
        }

        [Fact]
        public void Should_BeNoOp_When_EstimatedCompletionDateIsUnchanged()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.UpdateEstimatedCompletionDate(
                WorkItemData.EstimatedCompletionDate, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.UpdatedOnUtc.Should().BeNull();
            workItem.ChangeLogs.Should().NotContain(c => c.ChangeType == WorkItemChangeType.EstimatedCompletionDateUpdated);
        }

        [Fact]
        public void Should_Fail_When_ChangedByIsNotProjectMember()
        {
            // Arrange
            var (workItem, _, _) = WorkItemData.GetWorkItemWithContext();
            User nonMember = UserData.GetActiveUser();

            // Act
            Result result = workItem.UpdateEstimatedCompletionDate(NewDate, nonMember, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.NotFound);
        }
    }
```

There is deliberately no "date in the past" domain test. That rule depends on the clock and lives in the validator, which already has `IDateTimeProvider` injected — the same split the deleted `UpdateWorkItemValidator` used.

- [ ] **Step 2: Run to verify they fail**

```bash
dotnet build "Aurora Flowboard.slnx"
```

Expected: FAIL to compile — the two methods, the two enum values and `EstimatedPointsInvalid` are undefined.

- [ ] **Step 3: Add the enum values and the new error**

In `src/Aurora.Flowboard.Domain/WorkItems/WorkItemChangeType.cs`, extend the list so it ends:

```csharp
    TitleUpdated = 11,
    DescriptionUpdated = 12,
    TypeUpdated = 13,
    PriorityUpdated = 14,
    EstimatedPointsUpdated = 15,
    EstimatedCompletionDateUpdated = 16
}
```

In `src/Aurora.Flowboard.Domain/WorkItems/WorkItemErrors.cs`, add after `DescriptionTooLong`:

```csharp
    public static readonly BaseError EstimatedPointsInvalid = BaseError.Validation(
        "WorkItem.EstimatedPointsInvalid",
        "Estimated points must be greater than zero");
```

- [ ] **Step 4: Add the domain methods**

In `src/Aurora.Flowboard.Domain/WorkItems/WorkItem.cs`, add after `UpdatePriority`:

```csharp
    public Result UpdateEstimatedPoints(int? estimatedPoints, User changedBy, DateTime updatedOnUtc)
    {
        if (estimatedPoints is <= 0)
        {
            return Result.Fail(WorkItemErrors.EstimatedPointsInvalid);
        }

        Result guardResult = EnsureCanBeModifiedBy(changedBy);

        if (!guardResult.IsSuccessful)
        {
            return guardResult;
        }

        if (estimatedPoints == EstimatedPoints)
        {
            return Result.Ok();
        }

        EstimatedPoints = estimatedPoints;
        UpdatedOnUtc = updatedOnUtc;

        _changeLogs.Add(WorkItemChangeLog.Create(this, changedBy, WorkItemChangeType.EstimatedPointsUpdated, null, updatedOnUtc));

        return Result.Ok();
    }

    public Result UpdateEstimatedCompletionDate(DateOnly? estimatedCompletionDate, User changedBy, DateTime updatedOnUtc)
    {
        Result guardResult = EnsureCanBeModifiedBy(changedBy);

        if (!guardResult.IsSuccessful)
        {
            return guardResult;
        }

        if (estimatedCompletionDate == EstimatedCompletionDate)
        {
            return Result.Ok();
        }

        EstimatedCompletionDate = estimatedCompletionDate;
        UpdatedOnUtc = updatedOnUtc;

        _changeLogs.Add(WorkItemChangeLog.Create(this, changedBy, WorkItemChangeType.EstimatedCompletionDateUpdated, null, updatedOnUtc));

        return Result.Ok();
    }
```

`estimatedPoints is <= 0` is null-safe: a `null` `int?` does not match the relational pattern, so clearing the field passes straight through.

- [ ] **Step 5: Run the domain tests to verify they pass**

```bash
dotnet build "Aurora Flowboard.slnx"
./test/Aurora.Flowboard.Domain.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Domain.UnitTests.exe
```

Expected: PASS.

- [ ] **Step 6: Write the failing Application tests**

Create `test/Aurora.Flowboard.Application.UnitTests/WorkItems/UpdateWorkItemEstimatedPointsValidatorTests.cs`:

```csharp
namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class UpdateWorkItemEstimatedPointsValidatorTests
{
    private const int ValidPoints = 8;

    private readonly UpdateWorkItemEstimatedPointsValidator _validator = new();

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        UpdateWorkItemEstimatedPointsCommand command = new(Guid.NewGuid(), ValidPoints);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Pass_When_EstimatedPointsIsNull()
    {
        UpdateWorkItemEstimatedPointsCommand command = new(Guid.NewGuid(), null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_IdIsEmpty()
    {
        UpdateWorkItemEstimatedPointsCommand command = new(Guid.Empty, ValidPoints);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_EstimatedPointsIsZero()
    {
        UpdateWorkItemEstimatedPointsCommand command = new(Guid.NewGuid(), 0);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
```

Create `test/Aurora.Flowboard.Application.UnitTests/WorkItems/UpdateWorkItemEstimatedCompletionDateValidatorTests.cs`:

```csharp
namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class UpdateWorkItemEstimatedCompletionDateValidatorTests
{
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly UpdateWorkItemEstimatedCompletionDateValidator _validator;

    public UpdateWorkItemEstimatedCompletionDateValidatorTests()
    {
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _dateTimeProvider.Today.Returns(WorkItemCommandData.Today);
        _validator = new UpdateWorkItemEstimatedCompletionDateValidator(_dateTimeProvider);
    }

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        UpdateWorkItemEstimatedCompletionDateCommand command =
            new(Guid.NewGuid(), WorkItemCommandData.EstimatedCompletionDate);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Pass_When_DateIsNull()
    {
        UpdateWorkItemEstimatedCompletionDateCommand command = new(Guid.NewGuid(), null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_IdIsEmpty()
    {
        UpdateWorkItemEstimatedCompletionDateCommand command =
            new(Guid.Empty, WorkItemCommandData.EstimatedCompletionDate);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_DateIsInThePast()
    {
        DateOnly yesterday = WorkItemCommandData.Today.AddDays(-1);
        UpdateWorkItemEstimatedCompletionDateCommand command = new(Guid.NewGuid(), yesterday);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
```

Create `test/Aurora.Flowboard.Application.UnitTests/WorkItems/UpdateWorkItemEstimatedPointsHandlerTests.cs`:

```csharp
namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class UpdateWorkItemEstimatedPointsHandlerTests
{
    private const int NewPoints = 8;

    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUserContext _userContext;
    private readonly UpdateWorkItemEstimatedPointsHandler _handler;

    public UpdateWorkItemEstimatedPointsHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _userContext = Substitute.For<IUserContext>();
        _handler = new UpdateWorkItemEstimatedPointsHandler(_dbContext, _dateTimeProvider, _userContext);
    }

    [Fact]
    public async Task Should_UpdatePointsAndPersist_When_CommandIsValid()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        WorkItem workItem = WorkItemCommandData.GetWorkItem(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(WorkItemCommandData.UtcNow);

        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);

        UpdateWorkItemEstimatedPointsCommand command = new(workItem.Id, NewPoints);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        workItem.EstimatedPoints.Should().Be(NewPoints);
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnDomainError_When_PointsAreNotPositive()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        WorkItem workItem = WorkItemCommandData.GetWorkItem(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(WorkItemCommandData.UtcNow);

        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);

        UpdateWorkItemEstimatedPointsCommand command = new(workItem.Id, 0);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(WorkItemErrors.EstimatedPointsInvalid);
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnNotFoundError_When_UserIsNotProjectMember()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        WorkItem workItem = WorkItemCommandData.GetWorkItem(admin);
        User nonMember = WorkItemCommandData.GetNonMember();
        _userContext.UserId.Returns(nonMember.Id);
        _dateTimeProvider.UtcNow.Returns(WorkItemCommandData.UtcNow);

        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([nonMember]);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);

        UpdateWorkItemEstimatedPointsCommand command = new(workItem.Id, NewPoints);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(WorkItemErrors.NotFound);
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
```

Add both namespaces to `test/Aurora.Flowboard.Application.UnitTests/GlobalUsings.cs`:

```csharp
global using Aurora.Flowboard.Application.WorkItems.UpdateEstimatedCompletionDate;
global using Aurora.Flowboard.Application.WorkItems.UpdateEstimatedPoints;
```

- [ ] **Step 7: Run to verify they fail**

```bash
dotnet build "Aurora Flowboard.slnx"
```

Expected: FAIL to compile — the commands, validators and handlers do not exist.

- [ ] **Step 8: Write both Application slices**

Create `src/Aurora.Flowboard.Application/WorkItems/UpdateEstimatedPoints/UpdateWorkItemEstimatedPointsCommand.cs`:

```csharp
namespace Aurora.Flowboard.Application.WorkItems.UpdateEstimatedPoints;

public sealed record UpdateWorkItemEstimatedPointsCommand(Guid Id, int? EstimatedPoints) : ICommand;
```

Create `src/Aurora.Flowboard.Application/WorkItems/UpdateEstimatedPoints/UpdateWorkItemEstimatedPointsValidator.cs`:

```csharp
namespace Aurora.Flowboard.Application.WorkItems.UpdateEstimatedPoints;

internal sealed class UpdateWorkItemEstimatedPointsValidator : AbstractValidator<UpdateWorkItemEstimatedPointsCommand>
{
    public UpdateWorkItemEstimatedPointsValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.EstimatedPoints)
            .GreaterThan(0)
            .When(x => x.EstimatedPoints.HasValue);
    }
}
```

Create `src/Aurora.Flowboard.Application/WorkItems/UpdateEstimatedPoints/UpdateWorkItemEstimatedPointsHandler.cs`:

```csharp
namespace Aurora.Flowboard.Application.WorkItems.UpdateEstimatedPoints;

internal sealed class UpdateWorkItemEstimatedPointsHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : WorkItemFieldUpdateHandler<UpdateWorkItemEstimatedPointsCommand>(dbContext, dateTimeProvider, userContext)
{
    protected override Guid GetWorkItemId(UpdateWorkItemEstimatedPointsCommand command) => command.Id;

    protected override Task<Result> ApplyAsync(
        WorkItem workItem,
        UpdateWorkItemEstimatedPointsCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        Task.FromResult(workItem.UpdateEstimatedPoints(command.EstimatedPoints, changedBy, utcNow));
}
```

Create `src/Aurora.Flowboard.Application/WorkItems/UpdateEstimatedCompletionDate/UpdateWorkItemEstimatedCompletionDateCommand.cs`:

```csharp
namespace Aurora.Flowboard.Application.WorkItems.UpdateEstimatedCompletionDate;

public sealed record UpdateWorkItemEstimatedCompletionDateCommand(
    Guid Id,
    DateOnly? EstimatedCompletionDate) : ICommand;
```

Create `src/Aurora.Flowboard.Application/WorkItems/UpdateEstimatedCompletionDate/UpdateWorkItemEstimatedCompletionDateValidator.cs`:

```csharp
namespace Aurora.Flowboard.Application.WorkItems.UpdateEstimatedCompletionDate;

internal sealed class UpdateWorkItemEstimatedCompletionDateValidator
    : AbstractValidator<UpdateWorkItemEstimatedCompletionDateCommand>
{
    public UpdateWorkItemEstimatedCompletionDateValidator(IDateTimeProvider dateTimeProvider)
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.EstimatedCompletionDate)
            .GreaterThanOrEqualTo(_ => dateTimeProvider.Today)
            .When(x => x.EstimatedCompletionDate.HasValue);
    }
}
```

Create `src/Aurora.Flowboard.Application/WorkItems/UpdateEstimatedCompletionDate/UpdateWorkItemEstimatedCompletionDateHandler.cs`:

```csharp
namespace Aurora.Flowboard.Application.WorkItems.UpdateEstimatedCompletionDate;

internal sealed class UpdateWorkItemEstimatedCompletionDateHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : WorkItemFieldUpdateHandler<UpdateWorkItemEstimatedCompletionDateCommand>(dbContext, dateTimeProvider, userContext)
{
    protected override Guid GetWorkItemId(UpdateWorkItemEstimatedCompletionDateCommand command) => command.Id;

    protected override Task<Result> ApplyAsync(
        WorkItem workItem,
        UpdateWorkItemEstimatedCompletionDateCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        Task.FromResult(workItem.UpdateEstimatedCompletionDate(command.EstimatedCompletionDate, changedBy, utcNow));
}
```

- [ ] **Step 9: Run the Application tests to verify they pass**

```bash
dotnet build "Aurora Flowboard.slnx"
./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests.exe
```

Expected: PASS.

- [ ] **Step 10: Add both endpoints**

Create `src/Aurora.Flowboard.Api/Endpoints/WorkItems/UpdateWorkItemEstimatedPoints.cs`:

```csharp
using Aurora.Flowboard.Application.WorkItems.UpdateEstimatedPoints;

namespace Aurora.Flowboard.Api.Endpoints.WorkItems;

public sealed class UpdateWorkItemEstimatedPoints : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch(
            "work-items/{id:guid}/estimated-points",
            async (
                Guid id,
                [FromBody] UpdateWorkItemEstimatedPointsRequest request,
                ICommandHandler<UpdateWorkItemEstimatedPointsCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateWorkItemEstimatedPointsCommand(id, request.EstimatedPoints);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    () => Results.Accepted(string.Empty),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("UpdateWorkItemEstimatedPoints")
            .WithTags(EndpointTags.WorkItems)
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record UpdateWorkItemEstimatedPointsRequest(int? EstimatedPoints);
}
```

Create `src/Aurora.Flowboard.Api/Endpoints/WorkItems/UpdateWorkItemEstimatedCompletionDate.cs`:

```csharp
using Aurora.Flowboard.Application.WorkItems.UpdateEstimatedCompletionDate;

namespace Aurora.Flowboard.Api.Endpoints.WorkItems;

public sealed class UpdateWorkItemEstimatedCompletionDate : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch(
            "work-items/{id:guid}/estimated-completion-date",
            async (
                Guid id,
                [FromBody] UpdateWorkItemEstimatedCompletionDateRequest request,
                ICommandHandler<UpdateWorkItemEstimatedCompletionDateCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateWorkItemEstimatedCompletionDateCommand(
                    id,
                    request.EstimatedCompletionDate);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    () => Results.Accepted(string.Empty),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("UpdateWorkItemEstimatedCompletionDate")
            .WithTags(EndpointTags.WorkItems)
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record UpdateWorkItemEstimatedCompletionDateRequest(DateOnly? EstimatedCompletionDate);
}
```

- [ ] **Step 11: Build and run everything**

```bash
dotnet build "Aurora Flowboard.slnx"
./test/Aurora.Flowboard.Domain.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Domain.UnitTests.exe
./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests.exe
```

Expected: build clean with 0 warnings, both suites pass.

- [ ] **Step 12: Commit**

```bash
git add -A
git commit -m "Add PATCH estimated points and estimated completion date endpoints"
```

---

### Task 7: Component field

The first field whose handler must load another aggregate, because the domain rules need the entity, not just its id.

**Files:**
- Modify: `src/Aurora.Flowboard.Domain/WorkItems/WorkItemChangeType.cs`
- Modify: `src/Aurora.Flowboard.Domain/WorkItems/WorkItem.cs`
- Create: `src/Aurora.Flowboard.Application/WorkItems/ChangeComponent/ChangeWorkItemComponentCommand.cs`
- Create: `src/Aurora.Flowboard.Application/WorkItems/ChangeComponent/ChangeWorkItemComponentValidator.cs`
- Create: `src/Aurora.Flowboard.Application/WorkItems/ChangeComponent/ChangeWorkItemComponentHandler.cs`
- Create: `src/Aurora.Flowboard.Api/Endpoints/WorkItems/ChangeWorkItemComponent.cs`
- Modify: `test/Aurora.Flowboard.Domain.UnitTests/GlobalUsings.cs`
- Modify: `test/Aurora.Flowboard.Domain.UnitTests/WorkItems/WorkItemTests.cs`
- Modify: `test/Aurora.Flowboard.Application.UnitTests/GlobalUsings.cs`
- Modify: `test/Aurora.Flowboard.Application.UnitTests/WorkItems/WorkItemCommandData.cs`
- Create: `test/Aurora.Flowboard.Application.UnitTests/WorkItems/ChangeWorkItemComponentHandlerTests.cs`
- Create: `test/Aurora.Flowboard.Application.UnitTests/WorkItems/ChangeWorkItemComponentValidatorTests.cs`

**Interfaces:**
- Consumes: `WorkItem.EnsureCanBeModifiedBy` (Task 2), `WorkItemFieldUpdateHandler<TCommand>` (Task 3).
- Produces:
  - `WorkItemChangeType.ComponentChanged = 17`
  - `public Result ChangeComponent(Component? component, User changedBy, DateTime updatedOnUtc)`
  - `public sealed record ChangeWorkItemComponentCommand(Guid Id, Guid? ComponentId) : ICommand`
  - `WorkItemCommandData.GetWorkItemWithComponent(User admin, out Component component)`

- [ ] **Step 1: Make the test data helpers visible**

`ComponentData` and `MilestoneData` live in namespaces the domain test project does not import globally. Add to `test/Aurora.Flowboard.Domain.UnitTests/GlobalUsings.cs`, keeping alphabetical order:

```csharp
global using Aurora.Flowboard.Domain.UnitTests.Components;
global using Aurora.Flowboard.Domain.UnitTests.Milestones;
```

- [ ] **Step 2: Write the failing domain tests**

Add after `UpdateEstimatedCompletionDate` in `test/Aurora.Flowboard.Domain.UnitTests/WorkItems/WorkItemTests.cs`:

```csharp
    public sealed class ChangeComponent : BaseTest
    {
        [Fact]
        public void Should_SetComponent_When_DataIsValid()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            Component component = ComponentData.GetComponent(project, admin);

            // Act
            Result result = workItem.ChangeComponent(component, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.ComponentId.Should().Be(component.Id);
            workItem.Component.Should().Be(component);
            workItem.UpdatedOnUtc.Should().Be(WorkItemData.UpdatedOnUtc);
        }

        [Fact]
        public void Should_ClearComponent_When_NullIsProvided()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            Component component = ComponentData.GetComponent(project, admin);
            workItem.ChangeComponent(component, admin, WorkItemData.UpdatedOnUtc);

            // Act
            Result result = workItem.ChangeComponent(null, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.ComponentId.Should().BeNull();
            workItem.Component.Should().BeNull();
        }

        [Fact]
        public void Should_CreateComponentChangedChangeLog_WithComponentIdAsAffectedEntity()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            Component component = ComponentData.GetComponent(project, admin);

            // Act
            workItem.ChangeComponent(component, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            workItem.ChangeLogs.Should().Contain(c =>
                c.ChangeType == WorkItemChangeType.ComponentChanged &&
                c.AffectedEntityId == component.Id);
        }

        [Fact]
        public void Should_BeNoOp_When_ComponentIsUnchanged()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act — the work item was created without a component, so null is unchanged
            Result result = workItem.ChangeComponent(null, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.UpdatedOnUtc.Should().BeNull();
            workItem.ChangeLogs.Should().NotContain(c => c.ChangeType == WorkItemChangeType.ComponentChanged);
        }

        [Fact]
        public void Should_Fail_When_ComponentBelongsToAnotherProject()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();
            var (otherProject, otherAdmin) = WorkItemData.GetActiveProjectWithFlow();
            Component foreignComponent = ComponentData.GetComponent(otherProject, otherAdmin);

            // Act
            Result result = workItem.ChangeComponent(foreignComponent, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.ComponentNotInProject);
        }

        [Fact]
        public void Should_Fail_When_ComponentIsRetired()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            Component component = ComponentData.GetComponent(project, admin);
            component.Retire(admin, 0, WorkItemData.UpdatedOnUtc);

            // Act
            Result result = workItem.ChangeComponent(component, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.ComponentRetired);
        }

        [Fact]
        public void Should_Fail_When_ChangedByIsNotProjectMember()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            Component component = ComponentData.GetComponent(project, admin);
            User nonMember = UserData.GetActiveUser();

            // Act
            Result result = workItem.ChangeComponent(component, nonMember, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.NotFound);
        }
    }
```

- [ ] **Step 3: Run to verify they fail**

```bash
dotnet build "Aurora Flowboard.slnx"
```

Expected: FAIL to compile — `ChangeComponent` and `ComponentChanged` are undefined.

- [ ] **Step 4: Add the enum value and the domain method**

In `src/Aurora.Flowboard.Domain/WorkItems/WorkItemChangeType.cs`, extend the list so it ends:

```csharp
    EstimatedPointsUpdated = 15,
    EstimatedCompletionDateUpdated = 16,
    ComponentChanged = 17
}
```

In `src/Aurora.Flowboard.Domain/WorkItems/WorkItem.cs`, add after `UpdateEstimatedCompletionDate`:

```csharp
    public Result ChangeComponent(Component? component, User changedBy, DateTime updatedOnUtc)
    {
        if (component is not null)
        {
            if (component.ProjectId != ProjectId)
            {
                return Result.Fail(WorkItemErrors.ComponentNotInProject);
            }

            if (component.Status == ComponentStatus.Retired)
            {
                return Result.Fail(WorkItemErrors.ComponentRetired);
            }
        }

        Result guardResult = EnsureCanBeModifiedBy(changedBy);

        if (!guardResult.IsSuccessful)
        {
            return guardResult;
        }

        if (component?.Id == ComponentId)
        {
            return Result.Ok();
        }

        ComponentId = component?.Id;
        Component = component;
        UpdatedOnUtc = updatedOnUtc;

        _changeLogs.Add(WorkItemChangeLog.Create(this, changedBy, WorkItemChangeType.ComponentChanged, component?.Id, updatedOnUtc));

        return Result.Ok();
    }
```

`component?.Id == ComponentId` compares two `Guid?` values, so passing `null` when there is already no component is correctly a no-op, and passing `null` when one is set correctly proceeds to clear it. Both the FK and the navigation property are assigned so the in-memory graph stays consistent.

- [ ] **Step 5: Run the domain tests to verify they pass**

```bash
dotnet build "Aurora Flowboard.slnx"
./test/Aurora.Flowboard.Domain.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Domain.UnitTests.exe
```

Expected: PASS. `Component.Retire` is declared at `src/Aurora.Flowboard.Domain/Components/Component.cs:110` as `Retire(User changedBy, int openWorkItemCount, DateTime updatedOnUtc)`.

- [ ] **Step 6: Add the Application test data builder**

In `test/Aurora.Flowboard.Application.UnitTests/WorkItems/WorkItemCommandData.cs`, add this method at the end of the class:

```csharp
    public static WorkItem GetWorkItemWithProjectComponent(User admin, out Component component)
    {
        Project project = GetActiveProjectWithFlow(admin);
        component = Component.Create(ComponentName, project, admin, UtcNow).Value;

        return WorkItem.Create(
            Title,
            null,
            WorkItemType.Story,
            Priority.Medium,
            project,
            admin,
            null,
            null,
            UtcNow).Value;
    }
```

and add this constant next to the other `const string` fields at the top of the class:

```csharp
    public const string ComponentName = "Billing";
```

- [ ] **Step 7: Write the failing Application tests**

Create `test/Aurora.Flowboard.Application.UnitTests/WorkItems/ChangeWorkItemComponentValidatorTests.cs`:

```csharp
namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class ChangeWorkItemComponentValidatorTests
{
    private readonly ChangeWorkItemComponentValidator _validator = new();

    [Fact]
    public void Should_Pass_When_ComponentIdIsProvided()
    {
        ChangeWorkItemComponentCommand command = new(Guid.NewGuid(), Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Pass_When_ComponentIdIsNull()
    {
        ChangeWorkItemComponentCommand command = new(Guid.NewGuid(), null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_IdIsEmpty()
    {
        ChangeWorkItemComponentCommand command = new(Guid.Empty, Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
```

Create `test/Aurora.Flowboard.Application.UnitTests/WorkItems/ChangeWorkItemComponentHandlerTests.cs`:

```csharp
namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class ChangeWorkItemComponentHandlerTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUserContext _userContext;
    private readonly ChangeWorkItemComponentHandler _handler;

    public ChangeWorkItemComponentHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _userContext = Substitute.For<IUserContext>();
        _handler = new ChangeWorkItemComponentHandler(_dbContext, _dateTimeProvider, _userContext);
    }

    [Fact]
    public async Task Should_SetComponentAndPersist_When_CommandIsValid()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        WorkItem workItem = WorkItemCommandData.GetWorkItemWithProjectComponent(admin, out Component component);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(WorkItemCommandData.UtcNow);

        _dbContext.WorkItems.Returns(MockDbSetHelper.CreateMockDbSet([workItem]));
        _dbContext.Users.Returns(MockDbSetHelper.CreateMockDbSet([admin]));
        _dbContext.Components.Returns(MockDbSetHelper.CreateMockDbSet([component]));

        ChangeWorkItemComponentCommand command = new(workItem.Id, component.Id);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        workItem.ComponentId.Should().Be(component.Id);
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnComponentNotFoundError_When_ComponentDoesNotExist()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        WorkItem workItem = WorkItemCommandData.GetWorkItem(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(WorkItemCommandData.UtcNow);

        _dbContext.WorkItems.Returns(MockDbSetHelper.CreateMockDbSet([workItem]));
        _dbContext.Users.Returns(MockDbSetHelper.CreateMockDbSet([admin]));
        _dbContext.Components.Returns(MockDbSetHelper.CreateMockDbSet(Array.Empty<Component>()));

        ChangeWorkItemComponentCommand command = new(workItem.Id, Guid.NewGuid());

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ComponentErrors.NotFound);
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ClearComponent_When_ComponentIdIsNull()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        WorkItem workItem = WorkItemCommandData.GetWorkItemWithProjectComponent(admin, out Component component);
        workItem.ChangeComponent(component, admin, WorkItemCommandData.UtcNow);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(WorkItemCommandData.UtcNow);

        _dbContext.WorkItems.Returns(MockDbSetHelper.CreateMockDbSet([workItem]));
        _dbContext.Users.Returns(MockDbSetHelper.CreateMockDbSet([admin]));

        ChangeWorkItemComponentCommand command = new(workItem.Id, null);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        workItem.ComponentId.Should().BeNull();
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnNotFoundError_When_UserIsNotProjectMember()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        WorkItem workItem = WorkItemCommandData.GetWorkItemWithProjectComponent(admin, out Component component);
        User nonMember = WorkItemCommandData.GetNonMember();
        _userContext.UserId.Returns(nonMember.Id);
        _dateTimeProvider.UtcNow.Returns(WorkItemCommandData.UtcNow);

        _dbContext.WorkItems.Returns(MockDbSetHelper.CreateMockDbSet([workItem]));
        _dbContext.Users.Returns(MockDbSetHelper.CreateMockDbSet([nonMember]));
        _dbContext.Components.Returns(MockDbSetHelper.CreateMockDbSet([component]));

        ChangeWorkItemComponentCommand command = new(workItem.Id, component.Id);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(WorkItemErrors.NotFound);
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
```

Add the namespace to `test/Aurora.Flowboard.Application.UnitTests/GlobalUsings.cs`:

```csharp
global using Aurora.Flowboard.Application.WorkItems.ChangeComponent;
```

- [ ] **Step 8: Run to verify they fail**

```bash
dotnet build "Aurora Flowboard.slnx"
```

Expected: FAIL to compile — the command, validator and handler do not exist.

- [ ] **Step 9: Write the Application slice**

Create `src/Aurora.Flowboard.Application/WorkItems/ChangeComponent/ChangeWorkItemComponentCommand.cs`:

```csharp
namespace Aurora.Flowboard.Application.WorkItems.ChangeComponent;

public sealed record ChangeWorkItemComponentCommand(Guid Id, Guid? ComponentId) : ICommand;
```

Create `src/Aurora.Flowboard.Application/WorkItems/ChangeComponent/ChangeWorkItemComponentValidator.cs`:

```csharp
namespace Aurora.Flowboard.Application.WorkItems.ChangeComponent;

internal sealed class ChangeWorkItemComponentValidator : AbstractValidator<ChangeWorkItemComponentCommand>
{
    public ChangeWorkItemComponentValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
```

A `null` `ComponentId` is valid — it clears the association. A non-null one is checked against the database in the handler, not here.

Create `src/Aurora.Flowboard.Application/WorkItems/ChangeComponent/ChangeWorkItemComponentHandler.cs`:

```csharp
namespace Aurora.Flowboard.Application.WorkItems.ChangeComponent;

internal sealed class ChangeWorkItemComponentHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : WorkItemFieldUpdateHandler<ChangeWorkItemComponentCommand>(dbContext, dateTimeProvider, userContext)
{
    protected override Guid GetWorkItemId(ChangeWorkItemComponentCommand command) => command.Id;

    protected override async Task<Result> ApplyAsync(
        WorkItem workItem,
        ChangeWorkItemComponentCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        Component? component = null;

        if (command.ComponentId is not null)
        {
            component = await DbContext
                .Components
                .SingleOrDefaultAsync(c => c.Id == command.ComponentId, cancellationToken);

            if (component is null)
            {
                return Result.Fail(ComponentErrors.NotFound);
            }
        }

        return workItem.ChangeComponent(component, changedBy, utcNow);
    }
}
```

**Do not add `AsNoTracking()` to that query.** The entity is assigned to `WorkItem.Component`, a navigation property, and primary keys are configured `ValueGeneratedNever` — attaching an untracked entity there makes EF try to INSERT a row that already exists.

- [ ] **Step 10: Run the Application tests to verify they pass**

```bash
dotnet build "Aurora Flowboard.slnx"
./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests.exe
```

Expected: PASS.

- [ ] **Step 11: Add the endpoint**

Create `src/Aurora.Flowboard.Api/Endpoints/WorkItems/ChangeWorkItemComponent.cs`:

```csharp
using Aurora.Flowboard.Application.WorkItems.ChangeComponent;

namespace Aurora.Flowboard.Api.Endpoints.WorkItems;

public sealed class ChangeWorkItemComponent : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch(
            "work-items/{id:guid}/component",
            async (
                Guid id,
                [FromBody] ChangeWorkItemComponentRequest request,
                ICommandHandler<ChangeWorkItemComponentCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new ChangeWorkItemComponentCommand(id, request.ComponentId);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    () => Results.Accepted(string.Empty),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("ChangeWorkItemComponent")
            .WithTags(EndpointTags.WorkItems)
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record ChangeWorkItemComponentRequest(Guid? ComponentId);
}
```

- [ ] **Step 12: Build and run everything**

```bash
dotnet build "Aurora Flowboard.slnx"
./test/Aurora.Flowboard.Domain.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Domain.UnitTests.exe
./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests.exe
```

Expected: build clean with 0 warnings, both suites pass.

- [ ] **Step 13: Commit**

```bash
git add -A
git commit -m "Add PATCH work-items/{id}/component endpoint"
```

---

### Task 8: Milestone field

Mirrors Task 7, with the milestone's status rule in place of the component's retired rule.

**Files:**
- Modify: `src/Aurora.Flowboard.Domain/WorkItems/WorkItemChangeType.cs`
- Modify: `src/Aurora.Flowboard.Domain/WorkItems/WorkItem.cs`
- Create: `src/Aurora.Flowboard.Application/WorkItems/ChangeMilestone/ChangeWorkItemMilestoneCommand.cs`
- Create: `src/Aurora.Flowboard.Application/WorkItems/ChangeMilestone/ChangeWorkItemMilestoneValidator.cs`
- Create: `src/Aurora.Flowboard.Application/WorkItems/ChangeMilestone/ChangeWorkItemMilestoneHandler.cs`
- Create: `src/Aurora.Flowboard.Api/Endpoints/WorkItems/ChangeWorkItemMilestone.cs`
- Modify: `test/Aurora.Flowboard.Domain.UnitTests/WorkItems/WorkItemTests.cs`
- Modify: `test/Aurora.Flowboard.Application.UnitTests/GlobalUsings.cs`
- Modify: `test/Aurora.Flowboard.Application.UnitTests/WorkItems/WorkItemCommandData.cs`
- Create: `test/Aurora.Flowboard.Application.UnitTests/WorkItems/ChangeWorkItemMilestoneHandlerTests.cs`
- Create: `test/Aurora.Flowboard.Application.UnitTests/WorkItems/ChangeWorkItemMilestoneValidatorTests.cs`

**Interfaces:**
- Consumes: `WorkItem.EnsureCanBeModifiedBy` (Task 2), `WorkItemFieldUpdateHandler<TCommand>` (Task 3), the `GlobalUsings` entries added in Task 7.
- Produces:
  - `WorkItemChangeType.MilestoneChanged = 18`
  - `public Result ChangeMilestone(Milestone? milestone, User changedBy, DateTime updatedOnUtc)`
  - `public sealed record ChangeWorkItemMilestoneCommand(Guid Id, Guid? MilestoneId) : ICommand`
  - `WorkItemCommandData.GetWorkItemWithProjectMilestone(User admin, out Milestone milestone)`

- [ ] **Step 1: Write the failing domain tests**

Add after `ChangeComponent` in `test/Aurora.Flowboard.Domain.UnitTests/WorkItems/WorkItemTests.cs`:

```csharp
    public sealed class ChangeMilestone : BaseTest
    {
        [Fact]
        public void Should_SetMilestone_When_DataIsValid()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            Milestone milestone = MilestoneData.GetMilestone(project, admin);

            // Act
            Result result = workItem.ChangeMilestone(milestone, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.MilestoneId.Should().Be(milestone.Id);
            workItem.Milestone.Should().Be(milestone);
            workItem.UpdatedOnUtc.Should().Be(WorkItemData.UpdatedOnUtc);
        }

        [Fact]
        public void Should_ClearMilestone_When_NullIsProvided()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            Milestone milestone = MilestoneData.GetMilestone(project, admin);
            workItem.ChangeMilestone(milestone, admin, WorkItemData.UpdatedOnUtc);

            // Act
            Result result = workItem.ChangeMilestone(null, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.MilestoneId.Should().BeNull();
            workItem.Milestone.Should().BeNull();
        }

        [Fact]
        public void Should_CreateMilestoneChangedChangeLog_WithMilestoneIdAsAffectedEntity()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            Milestone milestone = MilestoneData.GetMilestone(project, admin);

            // Act
            workItem.ChangeMilestone(milestone, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            workItem.ChangeLogs.Should().Contain(c =>
                c.ChangeType == WorkItemChangeType.MilestoneChanged &&
                c.AffectedEntityId == milestone.Id);
        }

        [Fact]
        public void Should_BeNoOp_When_MilestoneIsUnchanged()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act — the work item was created without a milestone, so null is unchanged
            Result result = workItem.ChangeMilestone(null, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.UpdatedOnUtc.Should().BeNull();
            workItem.ChangeLogs.Should().NotContain(c => c.ChangeType == WorkItemChangeType.MilestoneChanged);
        }

        [Fact]
        public void Should_Fail_When_MilestoneBelongsToAnotherProject()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();
            var (otherProject, otherAdmin) = WorkItemData.GetActiveProjectWithFlow();
            Milestone foreignMilestone = MilestoneData.GetMilestone(otherProject, otherAdmin);

            // Act
            Result result = workItem.ChangeMilestone(foreignMilestone, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.MilestoneNotInProject);
        }

        [Fact]
        public void Should_Fail_When_MilestoneIsCompleted()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            Milestone milestone = MilestoneData.GetMilestoneWithStatus(MilestoneStatus.Completed, project, admin);

            // Act
            Result result = workItem.ChangeMilestone(milestone, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.MilestoneNotAcceptingAssignments);
        }

        [Fact]
        public void Should_Fail_When_MilestoneIsArchived()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            Milestone milestone = MilestoneData.GetMilestoneWithStatus(MilestoneStatus.Archived, project, admin);

            // Act
            Result result = workItem.ChangeMilestone(milestone, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.MilestoneNotAcceptingAssignments);
        }

        [Fact]
        public void Should_Fail_When_ChangedByIsNotProjectMember()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            Milestone milestone = MilestoneData.GetMilestone(project, admin);
            User nonMember = UserData.GetActiveUser();

            // Act
            Result result = workItem.ChangeMilestone(milestone, nonMember, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.NotFound);
        }
    }
```

- [ ] **Step 2: Run to verify they fail**

```bash
dotnet build "Aurora Flowboard.slnx"
```

Expected: FAIL to compile — `ChangeMilestone` and `MilestoneChanged` are undefined.

- [ ] **Step 3: Add the enum value and the domain method**

In `src/Aurora.Flowboard.Domain/WorkItems/WorkItemChangeType.cs`, extend the list so it ends:

```csharp
    EstimatedCompletionDateUpdated = 16,
    ComponentChanged = 17,
    MilestoneChanged = 18
}
```

In `src/Aurora.Flowboard.Domain/WorkItems/WorkItem.cs`, add after `ChangeComponent`:

```csharp
    public Result ChangeMilestone(Milestone? milestone, User changedBy, DateTime updatedOnUtc)
    {
        if (milestone is not null)
        {
            if (milestone.ProjectId != ProjectId)
            {
                return Result.Fail(WorkItemErrors.MilestoneNotInProject);
            }

            if (milestone.Status is MilestoneStatus.Completed or MilestoneStatus.Archived)
            {
                return Result.Fail(WorkItemErrors.MilestoneNotAcceptingAssignments);
            }
        }

        Result guardResult = EnsureCanBeModifiedBy(changedBy);

        if (!guardResult.IsSuccessful)
        {
            return guardResult;
        }

        if (milestone?.Id == MilestoneId)
        {
            return Result.Ok();
        }

        MilestoneId = milestone?.Id;
        Milestone = milestone;
        UpdatedOnUtc = updatedOnUtc;

        _changeLogs.Add(WorkItemChangeLog.Create(this, changedBy, WorkItemChangeType.MilestoneChanged, milestone?.Id, updatedOnUtc));

        return Result.Ok();
    }
```

`MilestoneNotAcceptingAssignments` stays a `BaseError.Validation`, so this returns **400**, not 409. The PRD claims 409; Task 9 corrects the PRD. Changing the error type would also change the `POST work-items` contract, because `WorkItem.Create` uses the same error.

- [ ] **Step 4: Run the domain tests to verify they pass**

```bash
dotnet build "Aurora Flowboard.slnx"
./test/Aurora.Flowboard.Domain.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Domain.UnitTests.exe
```

Expected: PASS.

- [ ] **Step 5: Add the Application test data builder**

In `test/Aurora.Flowboard.Application.UnitTests/WorkItems/WorkItemCommandData.cs`, add at the end of the class:

```csharp
    public static WorkItem GetWorkItemWithProjectMilestone(User admin, out Milestone milestone)
    {
        Project project = GetActiveProjectWithFlow(admin);
        milestone = Milestone.Create(
            MilestoneName,
            null,
            MilestoneStartDate,
            MilestoneEndDate,
            project,
            admin,
            UtcNow).Value;

        return WorkItem.Create(
            Title,
            null,
            WorkItemType.Story,
            Priority.Medium,
            project,
            admin,
            null,
            null,
            UtcNow).Value;
    }
```

and these constants next to the other fields at the top of the class:

```csharp
    public const string MilestoneName = "Phase 1 delivery";
    public static readonly DateOnly MilestoneStartDate = new(2026, 1, 15);
    public static readonly DateOnly MilestoneEndDate = new(2026, 2, 15);
```

`Milestone.Create` declares `string? description`, so passing `null` is valid.

- [ ] **Step 6: Write the failing Application tests**

Create `test/Aurora.Flowboard.Application.UnitTests/WorkItems/ChangeWorkItemMilestoneValidatorTests.cs`:

```csharp
namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class ChangeWorkItemMilestoneValidatorTests
{
    private readonly ChangeWorkItemMilestoneValidator _validator = new();

    [Fact]
    public void Should_Pass_When_MilestoneIdIsProvided()
    {
        ChangeWorkItemMilestoneCommand command = new(Guid.NewGuid(), Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Pass_When_MilestoneIdIsNull()
    {
        ChangeWorkItemMilestoneCommand command = new(Guid.NewGuid(), null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_IdIsEmpty()
    {
        ChangeWorkItemMilestoneCommand command = new(Guid.Empty, Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
```

Create `test/Aurora.Flowboard.Application.UnitTests/WorkItems/ChangeWorkItemMilestoneHandlerTests.cs`:

```csharp
namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class ChangeWorkItemMilestoneHandlerTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUserContext _userContext;
    private readonly ChangeWorkItemMilestoneHandler _handler;

    public ChangeWorkItemMilestoneHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _userContext = Substitute.For<IUserContext>();
        _handler = new ChangeWorkItemMilestoneHandler(_dbContext, _dateTimeProvider, _userContext);
    }

    [Fact]
    public async Task Should_SetMilestoneAndPersist_When_CommandIsValid()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        WorkItem workItem = WorkItemCommandData.GetWorkItemWithProjectMilestone(admin, out Milestone milestone);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(WorkItemCommandData.UtcNow);

        _dbContext.WorkItems.Returns(MockDbSetHelper.CreateMockDbSet([workItem]));
        _dbContext.Users.Returns(MockDbSetHelper.CreateMockDbSet([admin]));
        _dbContext.Milestones.Returns(MockDbSetHelper.CreateMockDbSet([milestone]));

        ChangeWorkItemMilestoneCommand command = new(workItem.Id, milestone.Id);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        workItem.MilestoneId.Should().Be(milestone.Id);
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnMilestoneNotFoundError_When_MilestoneDoesNotExist()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        WorkItem workItem = WorkItemCommandData.GetWorkItem(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(WorkItemCommandData.UtcNow);

        _dbContext.WorkItems.Returns(MockDbSetHelper.CreateMockDbSet([workItem]));
        _dbContext.Users.Returns(MockDbSetHelper.CreateMockDbSet([admin]));
        _dbContext.Milestones.Returns(MockDbSetHelper.CreateMockDbSet(Array.Empty<Milestone>()));

        ChangeWorkItemMilestoneCommand command = new(workItem.Id, Guid.NewGuid());

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(MilestoneErrors.NotFound);
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ClearMilestone_When_MilestoneIdIsNull()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        WorkItem workItem = WorkItemCommandData.GetWorkItemWithProjectMilestone(admin, out Milestone milestone);
        workItem.ChangeMilestone(milestone, admin, WorkItemCommandData.UtcNow);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(WorkItemCommandData.UtcNow);

        _dbContext.WorkItems.Returns(MockDbSetHelper.CreateMockDbSet([workItem]));
        _dbContext.Users.Returns(MockDbSetHelper.CreateMockDbSet([admin]));

        ChangeWorkItemMilestoneCommand command = new(workItem.Id, null);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        workItem.MilestoneId.Should().BeNull();
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnNotFoundError_When_UserIsNotProjectMember()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        WorkItem workItem = WorkItemCommandData.GetWorkItemWithProjectMilestone(admin, out Milestone milestone);
        User nonMember = WorkItemCommandData.GetNonMember();
        _userContext.UserId.Returns(nonMember.Id);
        _dateTimeProvider.UtcNow.Returns(WorkItemCommandData.UtcNow);

        _dbContext.WorkItems.Returns(MockDbSetHelper.CreateMockDbSet([workItem]));
        _dbContext.Users.Returns(MockDbSetHelper.CreateMockDbSet([nonMember]));
        _dbContext.Milestones.Returns(MockDbSetHelper.CreateMockDbSet([milestone]));

        ChangeWorkItemMilestoneCommand command = new(workItem.Id, milestone.Id);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(WorkItemErrors.NotFound);
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
```

Add the namespace to `test/Aurora.Flowboard.Application.UnitTests/GlobalUsings.cs`:

```csharp
global using Aurora.Flowboard.Application.WorkItems.ChangeMilestone;
```

- [ ] **Step 7: Run to verify they fail**

```bash
dotnet build "Aurora Flowboard.slnx"
```

Expected: FAIL to compile — the command, validator and handler do not exist.

- [ ] **Step 8: Write the Application slice**

Create `src/Aurora.Flowboard.Application/WorkItems/ChangeMilestone/ChangeWorkItemMilestoneCommand.cs`:

```csharp
namespace Aurora.Flowboard.Application.WorkItems.ChangeMilestone;

public sealed record ChangeWorkItemMilestoneCommand(Guid Id, Guid? MilestoneId) : ICommand;
```

Create `src/Aurora.Flowboard.Application/WorkItems/ChangeMilestone/ChangeWorkItemMilestoneValidator.cs`:

```csharp
namespace Aurora.Flowboard.Application.WorkItems.ChangeMilestone;

internal sealed class ChangeWorkItemMilestoneValidator : AbstractValidator<ChangeWorkItemMilestoneCommand>
{
    public ChangeWorkItemMilestoneValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
```

Create `src/Aurora.Flowboard.Application/WorkItems/ChangeMilestone/ChangeWorkItemMilestoneHandler.cs`:

```csharp
namespace Aurora.Flowboard.Application.WorkItems.ChangeMilestone;

internal sealed class ChangeWorkItemMilestoneHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : WorkItemFieldUpdateHandler<ChangeWorkItemMilestoneCommand>(dbContext, dateTimeProvider, userContext)
{
    protected override Guid GetWorkItemId(ChangeWorkItemMilestoneCommand command) => command.Id;

    protected override async Task<Result> ApplyAsync(
        WorkItem workItem,
        ChangeWorkItemMilestoneCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        Milestone? milestone = null;

        if (command.MilestoneId is not null)
        {
            milestone = await DbContext
                .Milestones
                .SingleOrDefaultAsync(m => m.Id == command.MilestoneId, cancellationToken);

            if (milestone is null)
            {
                return Result.Fail(MilestoneErrors.NotFound);
            }
        }

        return workItem.ChangeMilestone(milestone, changedBy, utcNow);
    }
}
```

**Do not add `AsNoTracking()`** — same reason as Task 7: the entity is assigned to a navigation property and the keys are `ValueGeneratedNever`.

- [ ] **Step 9: Run the Application tests to verify they pass**

```bash
dotnet build "Aurora Flowboard.slnx"
./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests.exe
```

Expected: PASS.

- [ ] **Step 10: Add the endpoint**

Create `src/Aurora.Flowboard.Api/Endpoints/WorkItems/ChangeWorkItemMilestone.cs`:

```csharp
using Aurora.Flowboard.Application.WorkItems.ChangeMilestone;

namespace Aurora.Flowboard.Api.Endpoints.WorkItems;

public sealed class ChangeWorkItemMilestone : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch(
            "work-items/{id:guid}/milestone",
            async (
                Guid id,
                [FromBody] ChangeWorkItemMilestoneRequest request,
                ICommandHandler<ChangeWorkItemMilestoneCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new ChangeWorkItemMilestoneCommand(id, request.MilestoneId);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    () => Results.Accepted(string.Empty),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("ChangeWorkItemMilestone")
            .WithTags(EndpointTags.WorkItems)
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record ChangeWorkItemMilestoneRequest(Guid? MilestoneId);
}
```

- [ ] **Step 11: Build and run everything**

```bash
dotnet build "Aurora Flowboard.slnx"
./test/Aurora.Flowboard.Domain.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Domain.UnitTests.exe
./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests.exe
```

Expected: build clean with 0 warnings, both suites pass.

- [ ] **Step 12: Commit**

```bash
git add -A
git commit -m "Add PATCH work-items/{id}/milestone endpoint"
```

---

### Task 9: Update the PRD

**Files:**
- Modify: `docs/flowboard-prd.md:511-515` (the work-items block of §11)
- Modify: `docs/flowboard-prd.md:532` (the milestone status-code note)

**Interfaces:**
- Consumes: the routes shipped in Tasks 4-8.
- Produces: nothing consumed by later tasks.

- [ ] **Step 1: Add the seven routes to §11**

In `docs/flowboard-prd.md`, replace the work-items block:

```
GET    /api/v1/flowboard/work-items?projectId=&milestoneId=&componentId=&type=&assigneeId=
POST   /api/v1/flowboard/work-items
GET    /api/v1/flowboard/work-items/{code}
PATCH  /api/v1/flowboard/work-items/{id}/move
PATCH  /api/v1/flowboard/work-items/{id}/milestone
```

with:

```
GET    /api/v1/flowboard/work-items?projectId=&milestoneId=&componentId=&type=&assigneeId=
POST   /api/v1/flowboard/work-items
GET    /api/v1/flowboard/work-items/{code}
PATCH  /api/v1/flowboard/work-items/{id}/move
PATCH  /api/v1/flowboard/work-items/{id}/title
PATCH  /api/v1/flowboard/work-items/{id}/description
PATCH  /api/v1/flowboard/work-items/{id}/type
PATCH  /api/v1/flowboard/work-items/{id}/priority
PATCH  /api/v1/flowboard/work-items/{id}/estimated-points
PATCH  /api/v1/flowboard/work-items/{id}/estimated-completion-date
PATCH  /api/v1/flowboard/work-items/{id}/component
PATCH  /api/v1/flowboard/work-items/{id}/milestone
```

- [ ] **Step 2: Correct the milestone status-code note**

Replace the bullet at line 532:

```
* `PATCH /work-items/{id}/milestone` is the only way to change milestone assignment; it is deliberately separate from the general update endpoint so it can be audited independently. It rejects a target milestone that is `Completed` or `Archived` with `409 Conflict`.
```

with:

```
* Every editable field of a work item has its own `PATCH` route, so each change can be audited independently and the front-end's inline controls can save one field at a time. There is no general update endpoint. A `null` body value clears the field.
* `PATCH /work-items/{id}/milestone` rejects a target milestone that is `Completed` or `Archived` with `400 Bad Request` (`WorkItem.MilestoneNotAcceptingAssignments`). `POST /work-items` returns the same error for the same reason.
```

- [ ] **Step 3: Verify the 404 note still matches the code**

Confirm the bullet stating that a non-member receives `404` rather than `403` is still present at roughly line 535. Task 2 made the work-item code match it. No edit needed unless it is missing.

- [ ] **Step 4: Commit**

```bash
git add docs/flowboard-prd.md
git commit -m "Document per-field work item PATCH routes in the PRD"
```

---

## Final verification

After Task 9, run the whole thing once more from a clean build:

```bash
cd "C:/SourcesGG/aurora-flowboard-api"
dotnet build "Aurora Flowboard.slnx"
./test/Aurora.Flowboard.Domain.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Domain.UnitTests.exe
./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests.exe
```

Expected: 0 warnings, 0 errors, both suites green.

Then confirm the routes are actually mapped by starting the API and checking Swagger:

```bash
dotnet run --project "src/Aurora.Flowboard.AppHost"
```

Open the Swagger UI and confirm that under the **WorkItems** tag there are eight `PATCH work-items/{id}/…` routes (title, description, type, priority, estimated-points, estimated-completion-date, component, milestone) and that `PUT work-items/{id}` is gone.
