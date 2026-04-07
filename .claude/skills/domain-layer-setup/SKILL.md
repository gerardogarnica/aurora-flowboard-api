---
name: domain-layer-setup
description: Domain layer base abstractions following DDD principles for domain agreggate roots.
language: C#
framework: .NET
pattern: Domain-Driven Design
---

# Domain Layer Setup

## Overview

This skill provides base abstractions for the Domain layer of a Clean Architecture application, following Domain-Driven Design (DDD) principles. It includes templates and guidelines for creating rich domain models with entities, value objects, domain events, and error handling using the Result pattern.

Domain layer abstraction setup includes:

- **Base entity** - Base class with Id and domain event management for domain aggregate roots
- **Base error** - Base record for typed errors with static factory methods
- **Base domain event** - Interface and base implementation for domain events
- **Result pattern** - Explicit success/failure results for domain operations without exceptions

---

## Domain Abstractions

All domain abstractions live in `src/Aurora.Flowboard.Domain/Abstractions/`.

**Result pattern (railway-oriented programming):** Business operations return `Result` or `Result<T>` — never throw exceptions for business errors. Use `Result.Ok(value)` for success and `Result.Fail(error)` for failure. `BaseError` has static factory methods: `Failure`, `Validation`, `NotFound`, `Conflict`.

**Entities:** Inherit from `BaseEntity`, which provides a `Guid Id` (init-only) and domain event management (`AddDomainEvent`, `ClearDomainEvents`). The EF Core parameterless constructor must call `: base(Guid.Empty)`.

**Domain events:** Use `sealed class` inheriting from `DomainEvent` with primary constructor syntax — `DomainEvent` is an abstract class, not a record, so `sealed record` is not allowed.

**Password hashing:** Always go through `IPasswordHasher` — never hash directly in domain or application code.

## Domain Layer structure

```
src/Aurora.Flowboard.Domain/
├── Abstractions/          — BaseEntity, BaseError, Result, DomainEvent, IPasswordHasher, SerializerSettings
├── Shared/                — Cross-aggregate value objects only (Email, EmailErrors)
├── Users/                 — User aggregate (no child entities)
│   └── Events/
├── Projects/              — Project aggregate + ProjectMember child entity
│   └── Events/
├── Flows/                 — Flow aggregate + FlowState, FlowTransition child entities
│   └── Events/
├── WorkItems/             — WorkItem aggregate + Comment, TimeEntry, StateTransitionHistory,
│   │                        WorkItemChangeLog child entities
│   └── Events/
└── GlobalUsings.cs
```

Each aggregate folder contains: the aggregate root, child entities, an error static class, and an `Events/` subfolder. **Enums belong in their owning aggregate folder** (e.g., `ProjectRole` in `Projects/`, `WorkItemType` and `Priority` in `WorkItems/`), never in `Shared/`.

---

## Entity Base class

```csharp
// src/{name}.Domain/Abstractions/BaseEntity.cs
namespace {name}.Domain.Abstractions;

public abstract class BaseEntity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid Id { get; init; }

    protected BaseEntity(Guid id)
    {
        Id = id;
    }

    [NotMapped]
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();

    public void RemoveDomainEvent(IDomainEvent domainEvent) => _domainEvents.Remove(domainEvent);
}
```

## Domain Event interface and implementation

```csharp
// src/{name}.Domain/Abstractions/DomainEvent.cs
namespace {name}.Domain.Abstractions;

public interface IDomainEvent
{
    Guid Id { get; }
    DateTime OccurredOnUtc { get; }
}

public abstract class DomainEvent : IDomainEvent
{
    public Guid Id { get; init; }

    public DateTime OccurredOnUtc { get; init; }

    protected DomainEvent()
    {
        Id = Guid.NewGuid();
        OccurredOnUtc = DateTime.UtcNow;
    }

    protected DomainEvent(Guid id, DateTime occurredOnUtc)
    {
        Id = id;
        OccurredOnUtc = occurredOnUtc;
    }
}
```

## Error Base class

```csharp
// src/{name}.Domain/Abstractions/BaseErrorType.cs
namespace {name}.Domain.Abstractions;

public enum BaseErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Forbidden = 4
}
```

```csharp
// src/{name}.Domain/Abstractions/BaseError.cs
namespace {name}.Domain.Abstractions;

public record BaseError(string Code, string Message, BaseErrorType ErrorType)
{
    public static readonly BaseError None = new(string.Empty, string.Empty, BaseErrorType.Failure);
    public static readonly BaseError NullValue = new("Error.NullValue", "The result value is null.", BaseErrorType.Failure);

    public static implicit operator Result(BaseError error) => Result.Fail(error);

    public static BaseError Failure(string code, string message) => new(code, message, BaseErrorType.Failure);

    public static BaseError Validation(string code, string message) => new(code, message, BaseErrorType.Validation);

    public static BaseError NotFound(string code, string message) => new(code, message, BaseErrorType.NotFound);

    public static BaseError Conflict(string code, string message) => new(code, message, BaseErrorType.Conflict);

    public static BaseError Forbidden(string code, string message) => new(code, message, BaseErrorType.Forbidden);
}
```

## Result Pattern

```csharp
// src/{name}.Domain/Abstractions/Result.cs
namespace {name}.Domain.Abstractions;

public class Result
{
    protected internal Result(bool isSuccessful, BaseError error)
    {
        if (isSuccessful && error != BaseError.None)
        {
            throw new ArgumentException("Result is successful, but error is not None.", nameof(error));
        }

        if (!isSuccessful && error == BaseError.None)
        {
            throw new ArgumentException("Result is failed, but error is None.", nameof(error));
        }

        IsSuccessful = isSuccessful;
        Error = error;
    }

    public bool IsSuccessful { get; }

    public BaseError Error { get; }

    public static Result Ok() => new(true, BaseError.None);

    public static Result<TValue> Ok<TValue>(TValue value) => new(value, true, BaseError.None);

    public static Result Fail(BaseError error) => new(false, error);

    public static Result<TValue> Fail<TValue>(BaseError error) => new(default!, false, error);
}

public class Result<TValue> : Result
{
    private readonly TValue _value;

    protected internal Result(TValue value, bool isSuccessful, BaseError error)
        : base(isSuccessful, error)
        => _value = value;

    public TValue Value => IsSuccessful
        ? _value
        : throw new InvalidOperationException("There is no value for failed result.");

    public static implicit operator Result<TValue>(TValue value) => Ok(value);
}
```

---

## Related Skills

- `dotnet-clean-architecture` - Master C#/.NET solution following Clean Architecture principles
- `domain-entity` - Generate a domain entities, child entities, and domain events