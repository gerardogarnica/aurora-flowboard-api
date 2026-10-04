---
paths:
  - "test/Aurora.Flowboard.Domain.UnitTests/**/*.cs"
  - "test/Aurora.Flowboard.Application.UnitTests/**/*.cs"
---

# Unit test conventions

- **Domain tests** assert entity behavior and domain events. Use `BaseTest` helpers. Pattern: `*Data.cs` builders + `*Tests.cs` xUnit facts. Stack: xUnit **v3** + FluentAssertions.
- **Application tests** test CQRS handler logic with NSubstitute mocks and `MockDbSetHelper`. Stack: xUnit **v3** + NSubstitute + FluentAssertions.

## Assign mock `DbSet`s to a local before `Returns(...)`

`MockDbSetHelper.CreateMockDbSet(...)` builds a substitute internally, and NSubstitute throws `CouldNotSetReturnDueToNoLastCallException` if you nest it inside `Returns(...)`.

```csharp
// Correct
DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
_dbContext.Users.Returns(usersMock);

// Throws
_dbContext.Users.Returns(MockDbSetHelper.CreateMockDbSet([user]));
```

## Paginated handlers must be tested across a page boundary

`MockDbSetHelper` runs on real LINQ-to-Objects, so `Skip`/`Take`/`OrderBy` behave for real. Use 3+ items with `pageSize` 2 and assert that page 1 and page 2 hold *different* items. Asserting only page 1, or only an out-of-range page, does not exercise the `Skip` offset.

It does **not** exercise EF translation: provider-level concerns (`AsSplitQuery`, SQL shape, subquery translation, `AllowedRoles`) need the real Npgsql provider and must be verified by running the app and reading the SQL logs.
