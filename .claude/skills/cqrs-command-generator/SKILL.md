---
name: cqrs-command-generator
description: Generates CQRS Commands with Handlers, Validators, and Request DTOs following Clean Architecture patterns. Commands represent actions that modify state and return Result types for proper error handling.
language: C#
framework: .NET
dependencies: FluentValidation
---

# CQRS Command Generator

## Overview

This skill generates Commands following the CQRS (Command Query Responsibility Segregation) pattern. Commands represent intentions to change system state. Each command has:

- **Command Record** - Immutable data structure with request parameters
- **Validator** - FluentValidation rules for input validation
- **Handler** - Business logic implementation returning Result

## Quick Reference

| Command Type | Returns | Use Case |
|--------------|---------|----------|
| `ICommand` | `Result` | Operations without return value (Update, Delete) |
| `ICommand<T>` | `Result<T>` | Operations returning data (Create returns Id) |

---

## Command Structure

```
/Application/{Feature}/
├── Create/
│   ├── Create{Entity}Command.cs        # Command Record
│   ├── Create{Entity}Handler.cs        # Handler
│   └── Create{Entity}Validator.cs      # Validator
├── Update/
│   ├── Update{Entity}Command.cs        # Command Record
│   ├── Update{Entity}Handler.cs        # Handler
│   └── Update{Entity}Validator.cs      # Validator
└── Delete/
│   ├── Delete{Entity}Command.cs
    └── Delete{Entity}Handler.cs
```

---

## Template: Command with Return Value

```csharp
// src/{name}.Application/{Feature}/Create/Create{Entity}Command.cs
namespace {name}.Application.{Feature}.Create;

public sealed record Create{Entity}Command(
    string Name,
    string? Description,
    int SortOrder,
    string? Notes) : ICommand<Guid>;
```

```csharp
// src/{name}.Application/{Feature}/Create/Create{Entity}Validator.cs
namespace {name}.Application.{Feature}.Create;

internal sealed class Create{Entity}Validator : AbstractValidator<Create{Entity}Command>
{
    public Create{Entity}Validator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(100);

        RuleFor(x => x.Description).MaximumLength(100);

        RuleFor(x => x.SortOrder)
            .NotEmpty()
            .InclusiveBetween(0, 15);

        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
```

```csharp
// src/{name}.Application/{Feature}/Create/Create{Entity}Handler.cs
namespace {name}.Application.{Feature}.Create;

internal sealed class Create{Entity}Handler(
    I{name}DbContext dbContext,
    IDateTimeService dateTimeService) : ICommandHandler<Create{Entity}Command, Guid>
{
    public async Task<Result<Guid>> Handle(
        Create{Entity}Command command,
        CancellationToken cancellationToken)
    {
        // Create {entity}
        var {entity} = {Entity}.Create(
            command.Name,
            command.Description,
            command.SortOrder,
            command.Notes,
            dateTimeService.UtcNow);

        dbContext.{Entities}.Add({entity});

        await dbContext.SaveChangesAsync(cancellationToken);

        return {entity}.Id;
    }
}
```

---

## Template: Command without Return Value

```csharp
// src/{name}.Application/{Feature}/Update/Update{Entity}Command.cs
namespace {name}.Application.{Feature}.Update;

public sealed record Update{Entity}Command(
    Guid Id,
    string Name,
    string? Description,
    int SortOrder,
    string? Notes) : ICommand;
```

```csharp
// src/{name}.Application/{Feature}/Update/Update{Entity}Validator.cs
namespace {name}.Application.{Feature}.Update;

internal sealed class Update{Entity}Validator : AbstractValidator<Update{Entity}Command>
{
    public Update{Entity}Validator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("{Entity} ID is required");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(100);

        RuleFor(x => x.Description).MaximumLength(100);

        RuleFor(x => x.SortOrder)
            .NotEmpty()
            .InclusiveBetween(0, 15);

        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
```

```csharp
// src/{name}.Application/{Feature}/Update/Update{Entity}Handler.cs
namespace {name}.Application.{Feature}.Update;

internal sealed class Update{Entity}Handler(
    I{name}DbContext dbContext,
    IDateTimeService dateTimeService) : ICommandHandler<Update{Entity}Command>
{
    public async Task<Result> Handle(
        Update{Entity}Command command,
        CancellationToken cancellationToken)
    {
        // Get {entity}
        Entity? {entity} = await dbContext
            .Entities
            .SingleOrDefaultAsync(x => x.Id == command.Id, cancellationToken);

        if ({entity} is null)
        {
            return Result.Fail({Entity}Errors.NotFound);
        }

        // Update {entity}
        var updateResult = {entity}.Update(
            command.Name,
            command.Description,
            command.SortOrder,
            command.Notes,
            dateTimeService.UtcNow);

        if (!updateResult.IsSuccessful)
        {
            return Result.Fail(updateResult.Error);
        }

        dbContext.{Entities}.Update({entity});

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
```

---

## Validation Rules Reference

```csharp
// String validations
RuleFor(x => x.Name)
    .NotEmpty()
    .NotNull()
    .MinimumLength(3)
    .MaximumLength(100)
    .Matches("^[a-zA-Z]+$").WithMessage("Only letters allowed");

// Numeric validations
RuleFor(x => x.Amount)
    .GreaterThan(0).WithMessage("Must be positive")
    .LessThanOrEqualTo(1000)
    .InclusiveBetween(1, 100).WithMessage("Must be 1-100");

// GUID validations
RuleFor(x => x.Id)
    .NotEmpty().WithMessage("ID is required")
    .NotEqual(Guid.Empty).WithMessage("Invalid ID");

// Email validation
RuleFor(x => x.Email)
    .NotEmpty()
    .EmailAddress().WithMessage("Invalid email format");

// Conditional validation
RuleFor(x => x.ParentId)
    .NotEmpty()
    .When(x => x.RequiresParent);

// Collection validation
RuleFor(x => x.Items)
    .NotEmpty().WithMessage("At least one item required")
    .Must(items => items.Count <= 10).WithMessage("Max 10 items");

// Custom validation
RuleFor(x => x.DateRange)
    .Must(BeValidDateRange).WithMessage("End date must be after start date");

private bool BeValidDateRange(DateRange range) => range.End > range.Start;
```

---

## Handler Patterns

### Pattern 1: Single Entity Operation

```csharp
public async Task<Result<Guid>> Handle(CreateCommand command, CancellationToken cancellationToken)
{
    // Create
    var entity = Entity.Create(command.Data);
    dbContext.Entity.Add(entity);
    await dbContext.SaveChangesAsync(cancellationToken);
    return entity.Id;
}
```

### Pattern 2: With Related Entities

```csharp
public async Task<Result<Guid>> Handle(CreateCommand command, CancellationToken cancellationToken)
{
    // Load related entity
    var parent = await dbContext.Parents.SingleOrDefaultAsync(x => x.Id == command.ParentId, cancellationToken);
    if (parent is null)
    {
        return Result.Fail<Guid>(ParentErrors.NotFound);
    }

    // Create with relationship
    var entity = Entity.Create(command.Data, parent);
    dbContext.Entities.Add(entity);
    await dbContext.SaveChangesAsync(cancellationToken);
    return entity.Id;
}
```

### Pattern 3: Batch Operations

```csharp
public async Task<r> Handle(CreateBatchCommand command, CancellationToken cancellationToken)
{
    var entities = new List<Entity>();

    foreach (var item in command.Items)
    {
        var result = Entity.Create(item);
        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }
        
        entities.Add(result.Value);
    }

    dbContext.Entities.AddRange(entities);
    await dbContext.SaveChangesAsync(cancellationToken);
    return Result.Ok();
}
```

---

## Critical Rules

1. **Commands are records** - Immutable, value equality
2. **One handler per command** - No shared handlers
3. **Validators are internal** - Not exposed outside Application layer
4. **Use Result pattern** - Never throw exceptions for business errors
5. **Use IDbContext** - Use the DbContext interface to add, update or remove entities
6. **Always use CancellationToken** - Pass through all async calls
7. **Domain logic in Domain** - Handler orchestrates, doesn't contain business rules
8. **Return IDs from Create** - Use `ICommand<Guid>` for creation
9. **Validate in order** - Check existence before creating, then validate business rules
10. **Keep handlers focused** - One responsibility per handler

---

## Anti-Patterns to Avoid

```csharp
// ❌ WRONG: Throwing exceptions for business errors
if (entity is null)
    throw new NotFoundException("Entity not found");

// ✅ CORRECT: Return Result
if (entity is null)
{
    return Result.Fail<Guid>(EntityErrors.NotFound);
}

// ❌ WRONG: Business logic in handler
if (command.Amount > 1000 && user.Level < 5)
    return Result.Fail(Error.InsufficientLevel);

// ✅ CORRECT: Business logic in domain
var result = entity.ProcessOrder(command.Amount, user);
if (!result.IsSuccessful)
{
    return Result.Fail(result.Error);
}

// ❌ WRONG: Validating dates at construction time
.GreaterThanOrEqualTo(_ => dateTimeProvider.Today)

// ✅ CORRECT: Use the lambda overload so the value is evaluated per-validation call
.GreaterThanOrEqualTo(dateTimeProvider.Today)
```

---

## Related Skills

- `application-layer-setup` - Base abstractions for Application layer
- `cqrs-query-generator` - Generate CQRS queries with handlers
- `domain-entity` - Generate a domain entities, child entities, and domain events