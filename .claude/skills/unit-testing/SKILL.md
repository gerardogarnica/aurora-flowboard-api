---
name: unit-testing
description: Generates unit tests for the Domain and Application layers with xUnit v3, NSubstitute, and FluentAssertions — entity invariants, domain events, CQRS handlers, and FluentValidation validators. Use when the user asks to write, fix, or extend tests, wants coverage for a new entity/handler/validator, reports a failing test, or after adding domain or application logic that needs regression coverage.
argument-hint: <what to test, e.g. "ArchiveProjectHandler" or "the Project status transitions">
---

# Unit Testing Skill – Domain & Application Layers

## Overview

This skill defines rules and patterns for generating unit tests targeting:

- Domain Layer (Entities, Value Objects, Domain Services)
- Application Layer (Commands, Queries, Handlers, Validators, Behaviors)

The goal is to ensure:
- Deterministic tests
- Full business logic coverage
- Zero infrastructure dependencies
- High maintainability

## When to Use This Skill

- Generating unit tests for Domain entities and Value Objects
- Testing CQRS Command and Query Handlers
- Validating business rules and invariants
- Testing validation logic (FluentValidation)
- Verifying Result pattern outcomes
- Refactoring safely with regression coverage

## Testing Strategy by Layer

| Layer | What to Test | What NOT to Test |
|------|-------------|------------------|
| Domain | Business rules, invariants, state transitions, domain events | EF Core, database, external services |
| Application | Handler logic, orchestration, validations, Result flow | Database implementation details, infrastructure |

## Project Structure (Tests)

The two projects are organized differently. **Domain** groups one file per aggregate; **Application** groups one file per use case.

```
test/
├── {name}.Domain.UnitTests/
│   ├── Abstractions/
│   │   └── BaseTest.cs                  → AssertDomainEventWasPublished<T>
│   ├── {Aggregate}/
│   │   ├── {Aggregate}Data.cs           → ProjectData, UserData, WorkItemData
│   │   ├── {Aggregate}Tests.cs          → ProjectTests (nested classes inside)
│   │   └── {ValueObject}Tests.cs        → ProjectCodeTests, ColorTests, EmailTests
│   ├── GlobalUsings.cs
│   └── {name}.Domain.UnitTests.csproj
└── {name}.Application.UnitTests
    ├── Abstractions/
    │   └── MockDbSetHelper.cs           → CreateMockDbSet<T> + async query provider
    ├── {Feature}/
    │   ├── {Feature}CommandData.cs      → ProjectCommandData
    │   ├── {Feature}QueryData.cs        → ProjectQueryData
    │   ├── {UseCase}HandlerTests.cs     → CreateProjectHandlerTests
    │   └── {UseCase}ValidatorTests.cs   → CreateProjectValidatorTests
    ├── GlobalUsings.cs
    └── {name}.Application.UnitTests.csproj
```

One test file per use case in Application — never one big `{Feature}Tests.cs`. Handler tests and validator tests are always separate files.

Data classes are split by direction: `{Feature}CommandData` for write-side fixtures, `{Feature}QueryData` for read-side ones. A use case with unusually specific fixtures may get its own (`CreateUserCommandData`).

### Domain tests: one nested class per method under test

The aggregate's test file is a shell containing a `public sealed class` per method:

```csharp
namespace {name}.Domain.UnitTests.{Aggregate};

public sealed class ProjectTests
{
    public sealed class Create : BaseTest
    {
        [Fact]
        public void Should_CreateProject_When_DataIsValid() { }
    }

    public sealed class ChangeStatus : BaseTest
    {
        [Fact]
        public void Should_Fail_When_TransitionIsNotAllowed() { }
    }

    // No domain events asserted here — no base class needed
    public sealed class CanAddOrUpdateFlow
    {
        [Fact]
        public void Should_ReturnTrue_When_StatusIsActive() { }
    }
}
```

**Inherit `BaseTest` on the nested class**, and only when that group asserts domain events — that is the sole reason the base class exists. Groups that only assert return values or state inherit nothing.

Application test classes are flat — one class per handler or validator, no nesting, no base class. They compose their substitutes in the constructor.

## Naming Conventions

| Item | Convention | Example |
|------|------------|---------|
| Domain test class | `{Aggregate}Tests` | `ProjectTests` |
| Domain nested class | the method under test | `ProjectTests.ChangeStatus` |
| Application test class | `{UseCase}HandlerTests` / `{UseCase}ValidatorTests` | `CreateProjectHandlerTests` |
| Test method | `Should_{ExpectedBehavior}_When_{Condition}` | `Should_CreateProject_When_DataIsValid` |
| Domain data class | `{Aggregate}Data` | `ProjectData`, `UserData` |
| Application data class | `{Feature}CommandData` / `{Feature}QueryData` | `ProjectCommandData` |
| Variables | camelCase | `result`, `command` |

## Domain Layer Testing Rules

### 1. Test Business Invariants

- Always validate:
  - Required fields
  - State transitions
  - Constraints

```csharp
[Fact]
public void Should_Fail_When_TitleIsEmpty()
```

### 2. Use Result Pattern Assertions

```csharp
result.IsSuccessful.Should().BeFalse();
result.Error.Should().Be(WorkItemErrors.TitleRequired);
```

Compare against the `BaseError` instance itself, never against its code or message string.

### 3. Test Domain Events

- Verify:
  - Event is raised
  - Event contains correct data

```csharp
result.IsSuccessful.Should().BeTrue();

FlowCreatedDomainEvent domainEvent =
    AssertDomainEventWasPublished<FlowCreatedDomainEvent>(result.Value);

domainEvent.FlowId.Should().Be(result.Value.Id);
```

`AssertDomainEventWasPublished<T>` already asserts the event exists and is unique, then returns it — use the return value to check the payload.

### 4. No Mocking in Domain Tests

Do NOT use:
- Any mocking library (NSubstitute included)
- Fake services
- Database

Domain types have no dependencies to mock. Build real instances through their factory methods, using a static data class. Real reference: `Domain.UnitTests/Projects/ProjectData.cs`.

```csharp
namespace {name}.Domain.UnitTests.{Aggregate};

internal static class ProjectData
{
    public const string Name = "Aurora Flowboard";
    public const string Description = "Project management API";
    public const string Code = "AFB";
    public static readonly Color Color = Color.Create("white").Value;
    public static readonly DateOnly EstimatedCompletionDate = new(2026, 12, 31);
    public static readonly DateTime CreatedOnUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime UpdatedOnUtc = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    public static Project GetDraftProject(User? creator = null)
    {
        User user = creator ?? UserData.GetActiveUser();
        return Project.Create(Name, Description, Code, Color, EstimatedCompletionDate, user, CreatedOnUtc).Value;
    }

    // A builder per state the tests need, driven through the aggregate's own methods
    public static Project GetProjectWithStatus(ProjectStatus status, User? admin = null)
    {
        User user = admin ?? UserData.GetActiveUser();
        Project project = GetDraftProject(user);

        switch (status)
        {
            case ProjectStatus.Active:
                project.ChangeStatus(ProjectStatus.Active, user, UpdatedOnUtc);
                break;
            case ProjectStatus.Archived:
                project.ChangeStatus(ProjectStatus.Active, user, UpdatedOnUtc);
                project.ChangeStatus(ProjectStatus.Archived, user, UpdatedOnUtc);
                break;
        }

        return project;
    }
}
```

Notice the shape:

- Fixed `DateTime`/`DateOnly` constants — never `DateTime.UtcNow`.
- Value objects built through their real factory and unwrapped: `Color.Create("white").Value`.
- Entities reach a non-initial state by calling the aggregate's own methods, never by mutation.
- Data classes may depend on each other across aggregates (`ProjectData` uses `UserData`); every
  one of them is imported via `GlobalUsings.cs`.

Application data classes follow the same shape **and also build the commands**:

```csharp
// Application.UnitTests/Projects/ProjectCommandData.cs
public static CreateProjectCommand GetCreateCommand() =>
    new(Name, Description, Code, Color, EstimatedCompletionDate);

public static UpdateProjectCommand GetUpdateCommand(Guid projectId) =>
    new(projectId, UpdatedName, UpdatedDescription, Color, EstimatedCompletionDate);
```

### 5. Prefer Value Object Equality

```csharp
valueObject1.Should().Be(valueObject2);
```

## Application Layer Testing Rules

### 1. Test Command Handlers

Each test must validate:
- Input → Handler → Output (Result)
- Side effects (calls to dependencies)

### 2. Substitute Dependencies (NSubstitute)

Substitute:
- `IApplicationDbContext`
- External services (`ITokenProvider`, `IPasswordHasher`, ...)
- `IDateTimeProvider`

Create the substitutes in the test class constructor and build the handler once:

```csharp
private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
```

`DbSet<T>` cannot be substituted directly — EF Core's async LINQ operators need a real
async query provider. Use `MockDbSetHelper.CreateMockDbSet` from `Application.UnitTests/Abstractions/MockDbSetHelper.cs`:

```csharp
DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
_dbContext.Users.Returns(usersMock);
```

For the empty case use `MockDbSetHelper.CreateMockDbSet(Array.Empty<User>())`.

### 3. Verify Behavior, Not Implementation

```csharp
// ✅ CORRECT — the observable outcome
await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());

// ❌ WRONG — internal orchestration detail
_dbContext.WorkItems.Received(1).Add(Arg.Any<WorkItem>());
```

To assert on the shape of an argument, use `Arg.Is<T>(predicate)`:

```csharp
_tokenProvider.Received(1).CreateToken(Arg.Is<TokenRequest>(r => r.UserId == user.Id));
```

Avoid testing private logic.

### 4. Validate Result Pattern

The property is `IsSuccessful`, not `IsSuccess`:

```csharp
result.IsSuccessful.Should().BeTrue();
```

or

```csharp
result.Error.Should().Be(SomeError);
```

### 5. Test Failure Scenarios First

Always include:
- Invalid input
- Not found entities
- Business rule violations

### 6. Test Queries as Pure Reads

- No side effects
- Validate returned DTOs

### 7. Validators Must Be Tested Independently

Every command or query with a validator gets its own `{UseCase}ValidatorTests.cs`. A validator
with dependencies takes them the same way a handler does — substitute them in the constructor:

```csharp
public sealed class CreateProjectValidatorTests
{
    private readonly CreateProjectValidator _validator;

    public CreateProjectValidatorTests()
    {
        IDateTimeProvider dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.Today.Returns(ProjectCommandData.Today);
        _validator = new CreateProjectValidator(dateTimeProvider);
    }

    [Fact]
    public void Should_Fail_When_NameExceedsMaxLength()
    {
        string longName = new('A', Project.MaxNameLength + 1);
        var command = new CreateProjectCommand(
            longName, null, ProjectCommandData.Code, ProjectCommandData.Color, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
```

Two things to copy:

- **Pin the clock property the validator actually reads.** A date rule reads `.Today`, not `.UtcNow`. Pin it to the same constant the data class exposes so "yesterday" is stable.
- **Drive boundary cases off the domain constant** — `new('A', Project.MaxNameLength + 1)`, never a hardcoded length. When the invariant moves, the test moves with it.

Assert on `result.IsValid`; there is no `Result` here — FluentValidation returns its own `ValidationResult`.

## Test Data Guidelines

- Use NSubstitute carefully: avoid `Returns()` ambiguity by using `arg.Returns(...)` form tied to a specific call
- **Prefer `[Fact]` over `[Theory]`.** The suite is almost entirely `[Fact]` — one named scenario per test reads better than a parameter table, and the method name documents the case
- Watch for SonarAnalyzer violations before declaring tests complete — warnings are errors here
- Use the `{Aggregate}Data` / `{Feature}CommandData` classes for fixtures; never build an entity inline in a test when a builder already exists
- Avoid duplication

## Deterministic Testing Rules

Never depend on:
- Current DateTime → substitute `IDateTimeProvider` and pin it to a fixed `DateTime` constant
- Random values
- External APIs

```csharp
private static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

_dateTimeProvider.UtcNow.Returns(UtcNow);

// Validators with date rules read Today, not UtcNow — pin the one the code actually calls
_dateTimeProvider.Today.Returns(DateOnly.FromDateTime(UtcNow));
```

Domain factory methods take the timestamp as a parameter, so Domain tests stay deterministic without any substitute.

## Tools & Libraries

The stack is fixed — do not introduce others:
- xUnit v3
- FluentAssertions
- NSubstitute (Application tests only)

Both test projects use `GlobalUsings.cs`, so `Xunit`, `FluentAssertions`, `NSubstitute`, `Microsoft.EntityFrameworkCore`, and the domain/application namespaces are already imported — do not add per-file `using` directives for them.

## Critical Rules

1. Tests MUST be deterministic
2. Tests MUST NOT depend on Infrastructure
3. One behavior per test
4. Tests MUST be readable and intention-revealing
5. Avoid over-mocking
6. Prefer Arrange-Act-Assert structure
7. Cover both success and failure paths
8. Keep tests fast (< 10ms ideally)

## Common Pitfalls

- Testing EF Core behavior in unit tests
- Overusing mocks
- Testing implementation instead of behavior
- Not testing edge cases
- Ignoring failure scenarios

## Example Pattern (AAA)

```csharp
// Arrange
var command = new CreateWorkItemCommand(...);

// Act
Result<Guid> result = await handler.Handle(command, CancellationToken.None);

// Assert
result.IsSuccessful.Should().BeTrue();
```

## Related Skills

- `domain-entity` - Entities, value objects, domain events, and their EF mapping
- `create-feature` - Command/query slices and endpoints (invokes this skill for its tests)
