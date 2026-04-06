---
name: domain-entity
description: Generates Domain Entities following DDD principles with factory methods, private setters, domain events, and proper encapsulation. Supports aggregate roots, child entities, and value objects.
language: C#
framework: .NET
pattern: Domain-Driven Design
---

# Domain Entity patterns

## Overview

This skill generates Domain Entities following Domain-Driven Design (DDD) principles:

- **Encapsulation** - Private setters, controlled modification
- **Factory Methods** - Static `Create()` methods with validation
- **Domain Events** - State changes raise events
- **Rich Domain Model** - Behavior lives in the entity, not services
- **Invariant Protection** - Entity always in valid state

## Quick Reference

| Concept | Purpose | Example |
|---------|---------|---------|
| Aggregate Root | Entry point for aggregate | `Project`, `User` |
| Child Entity | Part of aggregate, no own identity outside | `OrderItem`, `AssessmentDetail` |
| Value Object | Immutable, no identity | `Email`, `Money`, `Address` |
| Domain Event | Signal state change | `UserCreatedDomainEvent` |

---

## Entity Structure

```
/Domain/{Aggregate}s/
├── {Entity}.cs                    # Main entity
├── {Entity}Errors.cs              # Typed errors
└── Events/
    ├── {Entity}CreatedDomainEvent.cs
    ├── {Entity}UpdatedDomainEvent.cs
    └── ...
```

---

## Template: Aggregate Root Entity

```csharp
// src/{name}.Domain/{Aggregate}s/{Entity}.cs
using {name}.Domain.{Aggregate}.Events;

namespace {name}.Domain.{Aggregate}s;

public sealed class {Entity} : BaseEntity
{
    // ═══════════════════════════════════════════════════════════════
    // PRIVATE COLLECTIONS (encapsulated)
    // ═══════════════════════════════════════════════════════════════
    private readonly List<{ChildEntity}> _{childEntities} = [];

    // ═══════════════════════════════════════════════════════════════
    // PROPERTIES (private setters)
    // ═══════════════════════════════════════════════════════════════
    public Guid {Entity}Id { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }
    
    // Navigation property (read-only collection)
    public IReadOnlyCollection<{ChildEntity}> {ChildEntities} => _{childEntities}.AsReadOnly();

    // ═══════════════════════════════════════════════════════════════
    // CONSTRUCTORS
    // ═══════════════════════════════════════════════════════════════
    // Private constructor for EF Core
    private {Entity}() : base(Guid.Empty) { }

    // Private constructor for factory method
    private {Entity}(
        Guid id,
        string name,
        string? description,
        DateTime createdOnUtc) : base(id)
    {
        Name = name;
        Description = description;
        IsActive = true;
        CreatedOnUtc = createdOnUtc;
    }

    // ═══════════════════════════════════════════════════════════════
    // FACTORY METHODS
    // ═══════════════════════════════════════════════════════════════
    public static Result<{Entity}> Create(
        string name,
        string? description,
        DateTime createdOnUtc)
    {
        // Validate invariants
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Fail<{Entity}>({Entity}Errors.NameIsRequired);
        }

        if (name.Length > 100)
        {
            return Result.Fail<{Entity}>({Entity}Errors.NameTooLong);
        }

        var {entity} = new {Entity}(
            Guid.NewGuid(),
            name.Trim(),
            description.Trim(),
            createdOnUtc);

        // Raise domain event
        AddDomainEvent(new {Entity}CreatedDomainEvent({entity}.Id));

        return {entity};
    }

    // ═══════════════════════════════════════════════════════════════
    // DOMAIN METHODS
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Updates the {Entity} properties
    /// </summary>
    public Result Update(
        string name,
        string? description,
        DateTime updatedOnUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Fail({Entity}Errors.NameIsRequired);
        }

        if (name.Length > 100)
        {
            return Result.Fail({Entity}Errors.NameTooLong);
        }

        Name = name.Trim();
        Description = description.Trim();
        UpdatedOnUtc = updatedOnUtc;

        AddDomainEvent(new {Entity}UpdatedDomainEvent(Id));

        return Result.Ok();
    }

    /// <summary>
    /// Deactivates the {Entity}
    /// </summary>
    public Result Deactivate(DateTime updatedOnUtc)
    {
        if (!IsActive)
        {
            return Result.Fail({Entity}Errors.AlreadyDeactivated);
        }

        IsActive = false;
        UpdatedOnUtc = updatedOnUtc;

        AddDomainEvent(new {Entity}DeactivatedDomainEvent(Id));

        return Result.Ok();
    }

    /// <summary>
    /// Reactivates the {Entity}
    /// </summary>
    public Result Activate(DateTime updatedOnUtc)
    {
        if (IsActive)
        {
            return Result.Fail({Entity}Errors.AlreadyActive);
        }

        IsActive = true;
        UpdatedOnUtc = updatedOnUtc;

        return Result.Ok();
    }

    // ═══════════════════════════════════════════════════════════════
    // CHILD ENTITY MANAGEMENT
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Adds a child entity to this aggregate
    /// </summary>
    public Result Add{ChildEntity}(string name, string? description, int sortOrder, DateTime createdOnUtc)
    {
        Result<ChildEntity> {childEntity} = {ChildEntity}.Create(this, name, description, sortOrder, createdOnUtc);
        if (!{childEntity}.IsSuccessful)
        {
            return Result.Fail({childEntity}.Error);
        }

        _{childEntities}.Add({childEntity}.Value);

        AddDomainEvent(new {ChildEntity}AddedDomainEvent(Id, {childEntity}.Id));

        return Result.Ok();
    }

    /// <summary>
    /// Removes a child entity from this aggregate
    /// </summary>
    public Result Remove{ChildEntity}(Guid {childEntity}Id)
    {
        var {childEntity} = _{childEntities}.FirstOrDefault(c => c.Id == {childEntity}Id);

        if ({childEntity} is null)
        {
            return Result.Fail({Entity}Errors.{ChildEntity}NotFound);
        }

        _{childEntities}.Remove({childEntity});

        return Result.Ok();
    }

    // ═══════════════════════════════════════════════════════════════
    // QUERY METHODS
    // ═══════════════════════════════════════════════════════════════
    public bool HasActiveChildren() => _{childEntities}.Any(c => c.IsActive);

    public {ChildEntity}? GetChildById(Guid childId) => 
        _{childEntities}.FirstOrDefault(c => c.Id == childId);
}
```

---

## Template: Child Entity (Part of Aggregate)

```csharp
// src/{name}.Domain/{Aggregate}s/{ChildEntity}.cs
namespace {Name}.Domain.{Aggregate}s;

public sealed class {ChildEntity}
{
    // ═══════════════════════════════════════════════════════════════
    // PROPERTIES
    // ═══════════════════════════════════════════════════════════════
    public Guid Id { get; private set; }
    public Guid {Parent}Id { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime UpdatedOnUtc { get; private set; }

    // Navigation property
    public {Parent} {Parent} { get; private set; } = null!;

    // ═══════════════════════════════════════════════════════════════
    // CONSTRUCTORS
    // ═══════════════════════════════════════════════════════════════
    private {ChildEntity}() { } // EF Core

    private {ChildEntity}(
        Guid id,
        Guid {parent}Id,
        string name,
        string? description,
        int sortOrder,
        DateTime createdOnUtc)
    {
        Id = id;
        {Parent}Id = {parent}Id;
        Name = name;
        Description = description;
        SortOrder = sortOrder;
        IsActive = true;
        CreatedOnUtc = createdOnUtc;
    }

    // ═══════════════════════════════════════════════════════════════
    // FACTORY METHOD
    // ═══════════════════════════════════════════════════════════════
    internal static {ChildEntity} Create(
        {Parent} {parent},
        string name,
        string? description,
        int sortOrder,
        DateTime createdOnUtc)
    {
        return new {ChildEntity}(
            Guid.NewGuid(),
            {parent}.Id,
            name,
            description,
            sortOrder,
            createdOnUtc);
    }

    // ═══════════════════════════════════════════════════════════════
    // DOMAIN METHODS
    // ═══════════════════════════════════════════════════════════════
    public void Update(
        string name,
        string? description,
        int sortOrder,
        DateTime updatedOnUtc)
    {
        Name = name;
        Description = description;
        SortOrder = sortOrder;
        UpdatedOnUtc = updatedOnUtc;
    }

    public void Deactivate(DateTime updatedOnUtc)
    {
        IsActive = false;
        UpdatedOnUtc = updatedOnUtc;
    }
}
```

---

## Template: Value Object

```csharp
// src/{name}.Domain/Shared/Email.cs
namespace {name}.Domain.Shared;

public sealed record Email
{
    public string Value { get; init; }

    private Email(string value)
    {
        Value = value;
    }

    public static Result<Email> Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Result.Fail<Email>(EmailErrors.Empty);
        }

        email = email.Trim().ToLowerInvariant();

        if (email.Length > 255)
        {
            return Result.Fail<Email>(EmailErrors.TooLong);
        }

        if (!IsValidFormat(email))
        {
            return Result.Fail<Email>(EmailErrors.InvalidFormat);
        }

        return new Email(email);
    }

    private static bool IsValidFormat(string email)
    {
        // Simple email validation
        var atIndex = email.IndexOf('@');
        var dotIndex = email.LastIndexOf('.');
        
        return atIndex > 0 
            && dotIndex > atIndex + 1 
            && dotIndex < email.Length - 1;
    }

    public override string ToString() => Value;

    // Implicit conversion for convenience
    public static implicit operator string(Email email) => email.Value;
}

public static class EmailErrors
{
    public static readonly Error Empty = new("Email.Empty", "Email cannot be empty");
    public static readonly Error TooLong = new("Email.TooLong", "Email cannot exceed 255 characters");
    public static readonly Error InvalidFormat = new("Email.InvalidFormat", "Email format is invalid");
}
```

### More Value Object Examples

```csharp
// Money Value Object
public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Result<Money> Create(decimal amount, string currency = "USD")
    {
        if (amount < 0)
            return Result.Failure<Money>(MoneyErrors.NegativeAmount);

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            return Result.Failure<Money>(MoneyErrors.InvalidCurrency);

        return new Money(Math.Round(amount, 2), currency.ToUpperInvariant());
    }

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add different currencies");

        return new Money(Amount + other.Amount, Currency);
    }

    public static Money Zero(string currency = "USD") => new(0, currency);
}

// DateRange Value Object
public sealed record DateRange
{
    public DateTime Start { get; }
    public DateTime End { get; }

    private DateRange(DateTime start, DateTime end)
    {
        Start = start;
        End = end;
    }

    public static Result<DateRange> Create(DateTime start, DateTime end)
    {
        if (end <= start)
            return Result.Failure<DateRange>(DateRangeErrors.EndMustBeAfterStart);

        return new DateRange(start, end);
    }

    public bool Contains(DateTime date) => date >= Start && date <= End;
    
    public bool Overlaps(DateRange other) => 
        Start < other.End && End > other.Start;

    public int DurationInDays => (End - Start).Days;
}
```

---

## Template: Domain Errors

```csharp
// src/{name}.Domain/{Aggregate}/{Entity}Errors.cs
namespace {name}.Domain.{aggregate};

public static class {Entity}Errors
{
    // Not found errors
    public static readonly Error NotFound = BaseError.NotFound(
        "{Entity}.NotFound",
        "The {entity} with the specified identifier was not found");

    // Validation errors
    public static readonly Error NameIsRequired = BaseError.Validation(
        "{Entity}.NameRequired",
        "{Entity} name is required");

    public static readonly Error NameTooLong = BaseError.Validation(
        "{Entity}.NameTooLong",
        "{Entity} name cannot exceed 100 characters");

    // Business rule errors
    public static readonly Error AlreadyExists = BaseError.Conflict(
        "{Entity}.AlreadyExists",
        "A {entity} with this name already exists");

    public static readonly Error AlreadyDeactivated = BaseError.Validation(
        "{Entity}.AlreadyDeactivated",
        "The {entity} is already deactivated");

    public static readonly Error AlreadyActive = BaseError.Validation(
        "{Entity}.AlreadyActive",
        "The {entity} is already active");

    public static readonly Error CannotDeleteWithActiveRelationships = BaseError.Validation(
        "{Entity}.CannotDeleteWithActiveRelationships",
        "Cannot delete {entity} with active relationships");

    // Child entity errors
    public static readonly Error {ChildEntity}NotFound = BaseError.NotFound(
        "{Entity}.{ChildEntity}NotFound",
        "The {childEntity} was not found in this {entity}");

    public static readonly Error Duplicate{ChildEntity}Name = BaseError.Conflict(
        "{Entity}.Duplicate{ChildEntity}Name",
        "A {childEntity} with this name already exists");

    public static readonly Error Child{ChildEntity}Required = BaseError.Validation(
        "{Entity}.Child{ChildEntity}Required",
        "{ChildEntity} cannot be null");
}
```

---

## Template: Domain Events

```csharp
// src/{name}.Domain/{Aggregate}/Events/{Entity}CreatedDomainEvent.cs
namespace {name}.Domain.{aggregate}.Events;

public sealed class {Entity}CreatedDomainEvent(Guid {entity}Id) : DomainEvent
{
    public Guid {Entity}Id { get; init; } = {entity}Id;
}

// src/{name}.Domain/{Aggregate}/Events/{Entity}UpdatedDomainEvent.cs
public sealed class {Entity}UpdatedDomainEvent(Guid {entity}Id) : DomainEvent
{
    public Guid {Entity}Id { get; init; } = {entity}Id;
}

// src/{name}.Domain/{Aggregate}/Events/{Entity}DeactivatedDomainEvent.cs
public sealed class {Entity}DeactivatedDomainEvent(Guid {entity}Id) : DomainEvent
{
    public Guid {Entity}Id { get; init; } = {entity}Id;
}

// src/{name}.Domain/{Aggregate}/Events/{ChildEntity}AddedDomainEvent.cs
public sealed class {ChildEntity}AddedDomainEvent(
    Guid {entity}Id,
    Guid {childEntity}Id) : DomainEvent
{
    public Guid {Entity}Id { get; init; } = {entity}Id;
    public Guid {ChildEntity}Id { get; init; } = {childEntity}Id;
}
```

---

## Domain entities conventions

- Prefer `Guid` parameters over full entity objects when the method only needs the ID (e.g. `RemoveMember(Guid userId, ...)` instead of `RemoveMember(User user, ...)`).

---

## Critical DDD Rules

- **Private setters always** - No direct property modification from outside
- **Factory methods for creation** - `Create()` static methods with validations
- **Domain events for state changes** - Signal significant changes
- **Entities are always valid** - Invariants protected in constructors and methods
- **Aggregate root controls children** - Child entities managed through root
- **Value objects are immutable** - Use `record` types
- **No logic in setters** - Use named methods
- **Use Result pattern** - Return errors, don't throw
- **Keep entities persistence-ignorant** - No EF Core attributes on domain

---

## Anti-Patterns to Avoid

```csharp
// ❌ WRONG: Public setters
public string Name { get; set; }

// ✅ CORRECT: Private setters
public string Name { get; private set; }

// ❌ WRONG: Constructor with all parameters
public User(Guid id, string name, string email, DateTime createdOnUtc, ...)

// ✅ CORRECT: Factory method
public static Result<User> Create(string name, string email, DateTime createdOnUtc)

// ❌ WRONG: Throwing exceptions
if (name == null) throw new ArgumentNullException(nameof(name));

// ✅ CORRECT: Return Result
if (string.IsNullOrWhiteSpace(name))
{
    return Result.Failure<Entity>(EntityErrors.NameRequired);
}

// ❌ WRONG: Anemic domain model
public class User
{
    public string Name { get; set; }
    public void SetName(string name) => Name = name; // Just a setter!
}

// ✅ CORRECT: Rich domain model with behavior
public class User
{
    public string Name { get; private set; }
    
    public Result ChangeName(string newName, DateTime updatedAt)
    {
        if (string.IsNullOrWhiteSpace(newName))
            return Result.Failure(UserErrors.NameRequired);
        
        Name = newName;
        UpdatedAt = updatedAt;
        RaiseDomainEvent(new UserNameChangedDomainEvent(Id, newName));
        return Result.Success();
    }
}

// ❌ WRONG: Exposing internal collections
public List<OrderItem> Items { get; set; } = [];

// ✅ CORRECT: Encapsulated collections
private readonly List<OrderItem> _items = [];
public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
```

---

## Related Skills

- `domain-layer-setup` - Base abstractions for Domain layer
- `ef-core-configuration` - Map entities to database
- `domain-events-generator` - Handle domain events
- `result-pattern` - Error handling