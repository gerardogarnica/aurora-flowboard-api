# Aggregate root and child entity

## Aggregate root

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

## Child entity

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

    // Takes the parent's Guid, not the parent object.
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

## Anti-patterns

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
if (name.Length > 100)
{
    return Result.Fail<User>(UserErrors.NameTooLong);
}

// ✅ CORRECT: Named constant, reused by the EF configuration
public const int MaxNameLength = 100;
if (name.Length > MaxNameLength)
{
    return Result.Fail<User>(UserErrors.NameTooLong);
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
