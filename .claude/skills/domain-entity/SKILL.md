---
name: domain-entity
description: Generates Domain layer types following DDD — aggregate roots, child entities, value objects, domain events, and error catalogs, using static factory methods, private setters, and Result. Use when the user asks to add or change an entity, aggregate, value object, domain event, or {Entity}Errors class; when a business rule or invariant needs a home; or when asked where domain logic belongs.
argument-hint: <entity and its fields, e.g. "WorkItemTemplate with a name, priority and project">
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
| Child Entity | Part of aggregate, no own identity outside | `ProjectMember`, `Comment` |
| Value Object | Immutable, no identity | `Email`, `Color`, `ProjectCode` |
| Domain Event | Signal state change | `ProjectCreatedDomainEvent` |

## Placeholder convention

`{Aggregate}` is the **plural PascalCase folder name** of the aggregate: `Projects`, `Users`,
`WorkItems`, `Flows`. `{Entity}` is the singular type name: `Project`, `WorkItem`.

## Entity Structure

```
/Domain/{Aggregate}/
├── {Entity}.cs                    # Aggregate root
├── {Entity}Errors.cs              # Typed errors
├── {ChildEntity}.cs               # Child entity (if applicable)
├── {Entity}Status.cs              # Enumerations owned by this aggregate
└── Events/
    ├── {Entity}CreatedDomainEvent.cs
    ├── {Entity}UpdatedDomainEvent.cs
    └── ...
```

Enums live in their owning aggregate folder, never in `Shared/`. Value objects shared across aggregates go in `Domain/Shared/` with their own `{ValueObject}Errors.cs`.

## Template: Aggregate Root Entity

```csharp
// src/{name}.Domain/{Aggregate}/{Entity}.cs
using {name}.Domain.{Aggregate}.Events;

namespace {name}.Domain.{Aggregate};

public sealed class {Entity} : BaseEntity
{
    // ═══════════════════════════════════════════════════════════════
    // CONSTANTS (no magic numbers — reused by the EF configuration)
    // ═══════════════════════════════════════════════════════════════
    public const int MaxNameLength = 100;
    public const int MaxDescriptionLength = 500;

    // ═══════════════════════════════════════════════════════════════
    // PRIVATE COLLECTIONS (encapsulated)
    // ═══════════════════════════════════════════════════════════════
    private readonly List<{ChildEntity}> _{childEntities} = [];

    // ═══════════════════════════════════════════════════════════════
    // PROPERTIES (private setters)
    // ═══════════════════════════════════════════════════════════════
    // NOTE: `Id` is inherited from BaseEntity — do NOT redeclare it here.
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    // Navigation property to the creator (if needed)
    public User Creator { get; init; } = null!;

    // Navigation property (read-only collection over the private field)
    public IReadOnlyCollection<{ChildEntity}> {ChildEntities} => _{childEntities}.AsReadOnly();

    // ═══════════════════════════════════════════════════════════════
    // CONSTRUCTORS
    // ═══════════════════════════════════════════════════════════════
    private {Entity}() : base(Guid.Empty) { } // EF Core

    private {Entity}(
        Guid id,
        string name,
        string? description,
        Guid createdBy,
        DateTime createdOnUtc) : base(id)
    {
        Name = name;
        Description = description;
        IsActive = true;
        CreatedBy = createdBy;
        CreatedOnUtc = createdOnUtc;
    }

    // ═══════════════════════════════════════════════════════════════
    // FACTORY METHODS
    // ═══════════════════════════════════════════════════════════════
    public static Result<{Entity}> Create(
        string name,
        string? description,
        User createdBy,
        DateTime createdOnUtc)
    {
        // Validate invariants first — return, don't throw
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Fail<{Entity}>({Entity}Errors.NameRequired);
        }

        if (name.Length > MaxNameLength)
        {
            return Result.Fail<{Entity}>({Entity}Errors.NameTooLong);
        }

        if (description?.Length > MaxDescriptionLength)
        {
            return Result.Fail<{Entity}>({Entity}Errors.DescriptionTooLong);
        }

        var {entity} = new {Entity}(
            Guid.NewGuid(),
            name.Trim(),
            description?.Trim(),
            createdBy.Id,
            createdOnUtc);

        // AddDomainEvent is a protected INSTANCE method — call it on the new instance,
        // never bare inside this static method (that does not compile).
        {entity}.AddDomainEvent(new {Entity}CreatedDomainEvent({entity}.Id));

        // Implicit conversion from TValue to Result<TValue> — no Result.Ok(...) needed
        return {entity};
    }

    // ═══════════════════════════════════════════════════════════════
    // DOMAIN METHODS
    // ═══════════════════════════════════════════════════════════════
    public Result Update(
        string name,
        string? description,
        DateTime updatedOnUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Fail({Entity}Errors.NameRequired);
        }

        if (name.Length > MaxNameLength)
        {
            return Result.Fail({Entity}Errors.NameTooLong);
        }

        Name = name.Trim();
        Description = description?.Trim();
        UpdatedOnUtc = updatedOnUtc;

        AddDomainEvent(new {Entity}UpdatedDomainEvent(Id));

        return Result.Ok();
    }

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
    // CHILD ENTITY MANAGEMENT (the root owns its children)
    // ═══════════════════════════════════════════════════════════════
    public Result Add{ChildEntity}(string name, int sortOrder, DateTime createdOnUtc)
    {
        // Invariants that need the aggregate's state are checked HERE, in the root
        if (_{childEntities}.Any(c => c.Name == name))
        {
            return Result.Fail({Entity}Errors.Duplicate{ChildEntity}Name);
        }

        {ChildEntity} {childEntity} = {ChildEntity}.Create(Id, name, sortOrder, createdOnUtc);
        _{childEntities}.Add({childEntity});

        AddDomainEvent(new {ChildEntity}AddedDomainEvent(Id, {childEntity}.Id));

        return Result.Ok();
    }

    public Result Remove{ChildEntity}(Guid {childEntity}Id)
    {
        {ChildEntity}? {childEntity} = _{childEntities}.FirstOrDefault(c => c.Id == {childEntity}Id);

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
    internal bool Has{ChildEntity}(Guid {childEntity}Id) =>
        _{childEntities}.Any(c => c.Id == {childEntity}Id);
}
```

## Template: Child Entity (Part of Aggregate)

A child entity does **not** inherit `BaseEntity` — it has no domain events of its own and is never loaded independently of its root. Its factory is `internal`, so only the aggregate root can create it.

```csharp
// src/{name}.Domain/{Aggregate}/{ChildEntity}.cs
namespace {name}.Domain.{Aggregate};

public sealed class {ChildEntity}
{
    public const int MaxNameLength = 100;

    public Guid Id { get; private set; }
    public Guid {Entity}Id { get; private set; }
    public string Name { get; private set; }
    public int SortOrder { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    // Navigation property (if the child needs to reach a related aggregate)
    public User User { get; init; } = null!;

    private {ChildEntity}() { } // EF Core

    private {ChildEntity}(
        Guid id,
        Guid {entity}Id,
        string name,
        int sortOrder,
        DateTime createdOnUtc)
    {
        Id = id;
        {Entity}Id = {entity}Id;
        Name = name;
        SortOrder = sortOrder;
        CreatedOnUtc = createdOnUtc;
    }

    // Takes the parent's Guid, not the parent object — see conventions below.
    internal static {ChildEntity} Create(
        Guid {entity}Id,
        string name,
        int sortOrder,
        DateTime createdOnUtc) =>
        new(Guid.NewGuid(), {entity}Id, name, sortOrder, createdOnUtc);

    internal void Update(string name, int sortOrder, DateTime updatedOnUtc)
    {
        Name = name;
        SortOrder = sortOrder;
        UpdatedOnUtc = updatedOnUtc;
    }
}
```

If the child has invariants of its own, make `Create` return `Result<{ChildEntity}>` and unwrap it in the root with `.Value` after checking `.IsSuccessful`.

## Template: Value Object

Value objects are `sealed record` (structural equality), immutable, and validate in `Create`. Their errors live in a **separate** `{ValueObject}Errors.cs` file.

```csharp
// src/{name}.Domain/Shared/Email.cs
namespace {name}.Domain.Shared;

public sealed record Email
{
    public const int MaxLength = 255;

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

        if (email.Length > MaxLength)
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
        int atIndex = email.IndexOf('@');
        int dotIndex = email.LastIndexOf('.');

        return atIndex > 0
            && dotIndex > atIndex + 1
            && dotIndex < email.Length - 1;
    }

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;
}
```

```csharp
// src/{name}.Domain/Shared/EmailErrors.cs
namespace {name}.Domain.Shared;

public static class EmailErrors
{
    public static readonly BaseError Empty = BaseError.Validation(
        "Email.Empty",
        "Email cannot be empty");

    public static readonly BaseError TooLong = BaseError.Validation(
        "Email.TooLong",
        "Email cannot exceed 255 characters");

    public static readonly BaseError InvalidFormat = BaseError.Validation(
        "Email.InvalidFormat",
        "Email format is invalid");
}
```

## Template: Domain Errors

The error type is `BaseError`. Build it with a static factory — the record's constructor takes three arguments `(Code, Message, ErrorType)`.

**Pick the factory by semantics** — the category drives the HTTP status the endpoint returns:

| Factory | `BaseErrorType` | HTTP |
|---|---|---|
| `BaseError.Validation` | `Validation` | 400 |
| `BaseError.Forbidden` | `Forbidden` | 403 |
| `BaseError.NotFound` | `NotFound` | 404 |
| `BaseError.Conflict` | `Conflict` | 409 |
| `BaseError.Failure` | `Failure` | 500 |

Error codes are `"{Entity}.{Reason}"` — the entity name is **singular**: `"Project.NotFound"`.

```csharp
// src/{name}.Domain/{Aggregate}/{Entity}Errors.cs
namespace {name}.Domain.{Aggregate};

public static class {Entity}Errors
{
    // Not found
    public static readonly BaseError NotFound = BaseError.NotFound(
        "{Entity}.NotFound",
        "The {entity} with the specified identifier was not found");

    // Validation
    public static readonly BaseError NameRequired = BaseError.Validation(
        "{Entity}.NameRequired",
        "{Entity} name is required");

    public static readonly BaseError NameTooLong = BaseError.Validation(
        "{Entity}.NameTooLong",
        "{Entity} name cannot exceed 100 characters");

    public static readonly BaseError DescriptionTooLong = BaseError.Validation(
        "{Entity}.DescriptionTooLong",
        "{Entity} description cannot exceed 500 characters");

    public static readonly BaseError AlreadyDeactivated = BaseError.Validation(
        "{Entity}.AlreadyDeactivated",
        "The {entity} is already deactivated");

    public static readonly BaseError AlreadyActive = BaseError.Validation(
        "{Entity}.AlreadyActive",
        "The {entity} is already active");

    // Conflict
    public static readonly BaseError AlreadyExists = BaseError.Conflict(
        "{Entity}.AlreadyExists",
        "A {entity} with this name already exists");

    public static readonly BaseError Duplicate{ChildEntity}Name = BaseError.Conflict(
        "{Entity}.Duplicate{ChildEntity}Name",
        "A {childEntity} with this name already exists");

    // Forbidden — authorization expressed as a domain rule
    public static readonly BaseError OnlyAdminCanUpdate = BaseError.Forbidden(
        "{Entity}.OnlyAdminCanUpdate",
        "Only an administrator can update this {entity}");

    // Child entity
    public static readonly BaseError {ChildEntity}NotFound = BaseError.NotFound(
        "{Entity}.{ChildEntity}NotFound",
        "The {childEntity} was not found in this {entity}");
}
```

## Template: Domain Events

One record per file, in the aggregate's `Events/` subfolder. Inherit `DomainEvent` (which supplies `Id` and `OccurredOnUtc` and implements `IDomainEvent`). Events carry **ids and value types, never entity instances**.

```csharp
// src/{name}.Domain/{Aggregate}/Events/{Entity}CreatedDomainEvent.cs
namespace {name}.Domain.{Aggregate}.Events;

public sealed class {Entity}CreatedDomainEvent(Guid {entity}Id) : DomainEvent
{
    public Guid {Entity}Id { get; init; } = {entity}Id;
}
```

```csharp
// src/{name}.Domain/{Aggregate}/Events/{ChildEntity}AddedDomainEvent.cs
namespace {name}.Domain.{Aggregate}.Events;

public sealed class {ChildEntity}AddedDomainEvent(
    Guid {entity}Id,
    Guid {childEntity}Id) : DomainEvent
{
    public Guid {Entity}Id { get; init; } = {entity}Id;
    public Guid {ChildEntity}Id { get; init; } = {childEntity}Id;
}
```

## Domain entities conventions

- Prefer `Guid` parameters over full entity objects when the method only needs the ID (e.g. `RemoveMember(Guid userId, ...)` instead of `RemoveMember(User user, ...)`). Take the full entity only when the method reads its state (e.g. `AddMember(User user, ...)` checks `user.IsActive`).
- Use entity IDs in domain events instead of passing the full entity.
- Timestamps are **parameters**, never `DateTime.UtcNow` inside the entity — that keeps domain tests deterministic without any substitute.
- Declare length limits as `public const int` on the entity or value object; the EF configuration reuses them (`HasMaxLength({Entity}.MaxNameLength)`).

## Wiring: from the entity to the database

A domain entity that is never mapped is never persisted. After writing the entity, complete every step:

1. **Entity** → `src/{name}.Domain/{Aggregate}/{Entity}.cs`
2. **Error catalog** → `src/{name}.Domain/{Aggregate}/{Entity}Errors.cs`
3. **Domain events** (one per file) → `src/{name}.Domain/{Aggregate}/Events/{Entity}{PastTenseVerb}DomainEvent.cs`
4. **EF configuration** → `src/{name}.Infrastructure/Configurations/{Entity}Configuration.cs` — see *Persistence consequences* below. Picked up automatically by `ApplyConfigurationsFromAssembly`; no manual registration.
5. **DbSet** — add `DbSet<{Entity}> {Plural}` to **both**:
   - `src/{name}.Application/Abstractions/Data/IApplicationDbContext.cs`
   - `src/{name}.Infrastructure/Database/ApplicationDbContext.cs`
6. **Migration** — from the repo root, name in plain PascalCase (`AddWorkItemTemplates`):

   ```
   dotnet ef migrations add {Name} --project src/{name}.Infrastructure --startup-project src/{name}.Api
   ```

7. **Verify** — launch the `dotnet-test-runner` agent (Agent tool, `subagent_type: dotnet-test-runner`). It runs `dotnet build` (warnings are errors here) and `dotnet test`, and reports only failures — a single ✅ line means everything is green. Do not run those commands yourself, and do not consider the entity done until that agent comes back clean.

> Application unit tests substitute `IApplicationDbContext` with NSubstitute and
> `MockDbSetHelper` — there is no `TestDbContext` to update. See the `unit-testing` skill.

## Persistence consequences

Each domain shape forces a specific mapping. Decide these while designing the entity:

| Shape in the domain | Required in the EF configuration |
|---|---|
| `private readonly List<T> _items` exposed as `IReadOnlyCollection<T> Items` | `builder.Navigation(x => x.Items).HasField("_items").UsePropertyAccessMode(PropertyAccessMode.Field)` — **without it EF cannot materialize the collection** |
| Value object (`sealed record` with `Value`) | `builder.OwnsOne(x => x.Vo, vo => vo.Property(v => v.Value).HasColumnName("vo").HasMaxLength(Vo.MaxLength))` |
| `public const int MaxLength` on the entity/VO | `HasMaxLength({Entity}.MaxLength)` — never the literal number |
| Enum property | `HasConversion<string>()` for new enums |
| Computed property (`FullName`) | `builder.Ignore(x => x.FullName)` |
| Navigation to the creator (`User Creator`) | `HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict)` |
| Child collection owned by the root | `HasMany(x => x.Children).WithOne().HasForeignKey(x => x.{Entity}Id).OnDelete(DeleteBehavior.Cascade)` |

For every other mapping rule, use the `ef-core-configuration` skill.

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

## Anti-Patterns to Avoid

```csharp
// ❌ WRONG: Public setters
public string Name { get; set; }

// ✅ CORRECT: Private setters
public string Name { get; private set; }

// ❌ WRONG: Redeclaring the identifier
public sealed class Project : BaseEntity
{
    public Guid ProjectId { get; private set; }
}

// ✅ CORRECT: Id is inherited from BaseEntity
public sealed class Project : BaseEntity
{
    private Project(Guid id, ...) : base(id) { }
}

// ❌ WRONG: Public constructor with all parameters
public User(Guid id, string name, string email, DateTime createdOnUtc, ...)

// ✅ CORRECT: Private constructor + static factory returning Result
public static Result<User> Create(string name, Email email, DateTime createdOnUtc)

// ❌ WRONG: Throwing exceptions
if (name == null) throw new ArgumentNullException(nameof(name));

// ✅ CORRECT: Return Result
if (string.IsNullOrWhiteSpace(name))
{
    return Result.Fail<User>(UserErrors.NameRequired);
}

// ❌ WRONG: Calling the instance method bare inside a static factory — does not compile
public static Result<User> Create(...)
{
    var user = new User(...);
    AddDomainEvent(new UserCreatedDomainEvent(user.Id));
    return user;
}

// ✅ CORRECT: Call it on the instance
public static Result<User> Create(...)
{
    var user = new User(...);
    user.AddDomainEvent(new UserCreatedDomainEvent(user.Id));
    return user;
}

// ❌ WRONG: Magic numbers
if (name.Length > 100) return Result.Fail<User>(UserErrors.NameTooLong);

// ✅ CORRECT: Named constant, reused by the EF configuration
public const int MaxNameLength = 100;
if (name.Length > MaxNameLength) return Result.Fail<User>(UserErrors.NameTooLong);

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

    public Result ChangeName(string newName, DateTime updatedOnUtc)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            return Result.Fail(UserErrors.NameRequired);
        }

        Name = newName;
        UpdatedOnUtc = updatedOnUtc;
        AddDomainEvent(new UserNameChangedDomainEvent(Id));
        return Result.Ok();
    }
}

// ❌ WRONG: Exposing internal collections
public List<OrderItem> Items { get; set; } = [];

// ✅ CORRECT: Encapsulated collections
private readonly List<OrderItem> _items = [];
public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
```

## Related Skills

- `domain-layer-setup` - Base abstractions for Domain layer
- `ef-core-configuration` - Map entities to database tables
- `cqrs-command-generator` - Expose the entity's behavior as a use case
- `unit-testing` - Test invariants and domain events
