---
name: unit-testing
description: Generates unit tests for Domain and Application layers in a .NET Clean Architecture solution following strict testing rules, isolation principles, and deterministic behavior. Focuses on business logic validation, CQRS handlers, and domain invariants.
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

---

## When to Use This Skill

- Generating unit tests for Domain entities and Value Objects
- Testing CQRS Command and Query Handlers
- Validating business rules and invariants
- Testing validation logic (FluentValidation)
- Verifying Result pattern outcomes
- Refactoring safely with regression coverage

---

## Testing Strategy by Layer

| Layer | What to Test | What NOT to Test |
|------|-------------|------------------|
| Domain | Business rules, invariants, state transitions, domain events | EF Core, database, external services |
| Application | Handler logic, orchestration, validations, Result flow | Database implementation details, infrastructure |

---

## Project Structure (Tests)

```
test/
├── {name}.Domain.UnitTests/
│   ├── Abstractions/
│   │   └── BaseTest.cs
│   ├── {Feature}/
│   │   ├── {Feature}Data.cs
│   │   └── {Feature}Tests.cs
│   ├── GlobalUsings.cs
│   └── {name}.Domain.UnitTests.csproj
└── {name}.Application.UnitTests
    ├── {Feature}/
    │   ├── {Feature}Data.cs
    │   └── {Feature}Tests.cs
    ├── GlobalUsings.cs
    └── {name}.Application.UnitTests.csproj
```

---

## Naming Conventions

| Item | Convention | Example |
|------|------------|---------|
| Test Class | {ClassName}Tests | WorkItemTests |
| Test Method | Should_{ExpectedBehavior}_When_{Condition} | Should_CreateWorkItem_When_DataIsValid |
| Data Class | {ClassName}Data | WorkItemData |
| Variables | camelCase | result, command |

---

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
result.Error.Should().Be(WorkItemErrors.InvalidTitle);
``` 

### 3. Test Domain Events

- Verify:
  - Event is raised
  - Event contains correct data

```csharp
result.IsSuccessful.Should().BeTrue();
var domainEvent = AssertDomainEventWasPublished<WalletSavingsUpdatedEvent>(wallet);
domainEvent.Should().NotBeNull();
```

### 4. No Mocking in Domain Tests

Do NOT use:
- Moq
- Fake services
- Database

USE data static class.

```csharp
internal static class WalletData
{
    public const string Name = "Name of the wallet";
    public static readonly Money AvailableAmount = new(100.0m, Currency.Usd);
    public const WalletType Type = WalletType.Cash;
    public const bool AllowNegative = false;
    public static readonly Color Color = Color.FromHex("#000000");
    public const string? Notes = "Notes of the category";

    public static Wallet GetWallet() => Wallet.Create(
        UserData.GetUser().Id,
        Name,
        AvailableAmount,
        Type,
        AllowNegative,
        Color,
        Notes,
        DateOnly.FromDateTime(DateTime.UtcNow),
        DateTime.UtcNow);
}
```

### 5. Prefer Value Object Equality

```csharp
valueObject1.Should().Be(valueObject2);
```

---

## Application Layer Testing Rules

### 1. Test Command Handlers

Each test must validate:
- Input → Handler → Output (Result)
- Side effects (calls to dependencies)

### 2. Mock Dependencies

Use mocking for:
- DbContext interfaces
- External services
- DateTime providers

```csharp
var dbContextMock = new Mock<IApplicationDbContext>();
```

### 3. Verify Behavior, Not Implementation

```csharp
// ✅ CORRECT
dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

// ❌ WRONG
dbContextMock.Verify(x => x.WorkItems.Add(It.IsAny<WorkItem>()), Times.Once);
```

Avoid testing private logic.

### 4. Validate Result Pattern

```csharp
result.IsSuccess.Should().BeTrue();
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

```csharp
var result = validator.Validate(command);
result.IsValid.Should().BeFalse();
```

---

## Test Data Guidelines

- Use Mother Objects / Builders for complex entities
- Avoid duplication

```csharp
var command = CreateWorkItemCommandBuilder.Valid().Build();
```

---

## Deterministic Testing Rules

Never depend on:
- Current DateTime → use IDateTimeService
- Random values
- External APIs

---

## Tools & Libraries

Recommended:
- xUnit
- FluentAssertions
- Moq
- Bogus (optional)

---

## Critical Rules

1. Tests MUST be deterministic
2. Tests MUST NOT depend on Infrastructure
3. One behavior per test
4. Tests MUST be readable and intention-revealing
5. Avoid over-mocking
6. Prefer Arrange-Act-Assert structure
7. Cover both success and failure paths
8. Keep tests fast (< 10ms ideally)

---

## Common Pitfalls

- Testing EF Core behavior in unit tests
- Overusing mocks
- Testing implementation instead of behavior
- Not testing edge cases
- Ignoring failure scenarios

---

## Example Pattern (AAA)

```csharp
// Arrange
var command = new CreateWorkItemCommand(...);

// Act
var result = await handler.Handle(command, CancellationToken.None);

// Assert
result.IsSuccess.Should().BeTrue();
```

---

## Related Skills

- `domain-layer-setup` - Base abstractions for Domain layer
- `domain-entity` - Generate a domain entities, child entities, and domain events
- `application-layer-setup` - Base abstractions for Application layer
- `cqrs-command-generator` - Generate CQRS commands with handlers and validators
- `cqrs-query-generator` - Generate CQRS queries with handlers