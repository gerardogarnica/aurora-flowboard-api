---
paths:
  - "test/Aurora.Flowboard.Domain.UnitTests/**/*.cs"
  - "test/Aurora.Flowboard.Application.UnitTests/**/*.cs"
---

# Unit test conventions

- **Domain tests** assert entity behavior and domain events. Use `BaseTest` helpers. Pattern: `*Data.cs` builders + `*Tests.cs` xUnit facts. Stack: xUnit **v3** + FluentAssertions.
- **Application tests** test CQRS handler logic with NSubstitute mocks, seeding `DbSet`s with `_dbContext.Seed(...)` (built on `MockDbSetHelper`). Stack: xUnit **v3** + NSubstitute + FluentAssertions.

## Seed mock `DbSet`s with `_dbContext.Seed(...)`

Arrange a handler's data with `DbContextSeedExtensions.Seed` (`Abstractions/`), one line per `DbSet`. It accepts items, an existing collection, or nothing (an empty set), and returns the mock set for the rare test that asserts on it (`projectsMock.Received(1).Add(...)`).

```csharp
_dbContext.Seed(c => c.WorkItems, workItem);
_dbContext.Seed(c => c.Users, assignee, admin);
_dbContext.Seed(c => c.Projects);   // empty
```

Don't go back to calling `MockDbSetHelper.CreateMockDbSet` in a test. It builds a substitute internally, and NSubstitute throws `CouldNotSetReturnDueToNoLastCallException` when it is nested inside `Returns(...)` (`_dbContext.Users.Returns(MockDbSetHelper.CreateMockDbSet([user]))`). `Seed` creates the set before touching the context property, so the trap is handled in one place.

Per-aggregate fixtures that hide what each `DbSet` holds were considered and rejected: each test should still say what data its scenario has.

## Paginated handlers must be tested across a page boundary

`MockDbSetHelper` runs on real LINQ-to-Objects, so `Skip`/`Take`/`OrderBy` behave for real. Use 3+ items with `pageSize` 2 and assert that page 1 and page 2 hold *different* items. Asserting only page 1, or only an out-of-range page, does not exercise the `Skip` offset.

It does **not** exercise EF translation: provider-level concerns (`AsSplitQuery`, SQL shape, subquery translation, `AllowedRoles`) need the real Npgsql provider and must be verified by running the app and reading the SQL logs.
