# Command slice

A command is a state change. Every command slice lives in its own folder:

```
/Application/{Feature}/{UseCase}/
├── {Verb}{Entity}Command.cs
├── {Verb}{Entity}Handler.cs
└── {Verb}{Entity}Validator.cs
```

| Interface | Handler returns | Use for |
|---|---|---|
| `ICommand` | `Result` | Update, Delete, state changes |
| `ICommand<T>` | `Result<T>` | Create — returns the new `Guid` |

## Command returning a value

```csharp
// src/{name}.Application/{Feature}/Create/Create{Entity}Command.cs
namespace {name}.Application.{Feature}.Create;

public sealed record Create{Entity}Command(
    string Name,
    string? Description,
    int SortOrder) : ICommand<Guid>;
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
            .MaximumLength({Entity}.MaxNameLength);

        RuleFor(x => x.Description)
            .MaximumLength({Entity}.MaxDescriptionLength);

        RuleFor(x => x.SortOrder)
            .InclusiveBetween(0, 15);
    }
}
```

```csharp
// src/{name}.Application/{Feature}/Create/Create{Entity}Handler.cs
namespace {name}.Application.{Feature}.Create;

internal sealed class Create{Entity}Handler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ICommandHandler<Create{Entity}Command, Guid>
{
    public async Task<Result<Guid>> Handle(
        Create{Entity}Command command,
        CancellationToken cancellationToken)
    {
        User? createdBy = await dbContext
            .Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);

        if (createdBy is null)
        {
            return Result.Fail<Guid>(UserErrors.NotFound);
        }

        Result<{Entity}> result = {Entity}.Create(
            command.Name,
            command.Description,
            command.SortOrder,
            createdBy,
            dateTimeProvider.UtcNow);

        if (!result.IsSuccessful)
        {
            return Result.Fail<Guid>(result.Error);
        }

        {Entity} {entity} = result.Value;

        dbContext.{Entities}.Add({entity});

        await dbContext.SaveChangesAsync(cancellationToken);

        return {entity}.Id;
    }
}
```

## Command without a return value

```csharp
// src/{name}.Application/{Feature}/Update/Update{Entity}Command.cs
namespace {name}.Application.{Feature}.Update;

public sealed record Update{Entity}Command(
    Guid Id,
    string Name,
    string? Description) : ICommand;
```

```csharp
// src/{name}.Application/{Feature}/Update/Update{Entity}Handler.cs
namespace {name}.Application.{Feature}.Update;

internal sealed class Update{Entity}Handler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<Update{Entity}Command>
{
    public async Task<Result> Handle(
        Update{Entity}Command command,
        CancellationToken cancellationToken)
    {
        {Entity}? {entity} = await dbContext
            .{Entities}
            .SingleOrDefaultAsync(x => x.Id == command.Id, cancellationToken);

        if ({entity} is null)
        {
            return Result.Fail({Entity}Errors.NotFound);
        }

        Result updateResult = {entity}.Update(
            command.Name,
            command.Description,
            dateTimeProvider.UtcNow);

        if (!updateResult.IsSuccessful)
        {
            return Result.Fail(updateResult.Error);
        }

        // No dbContext.{Entities}.Update(...) — the entity is already tracked.
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
```

## Tracking rules

Primary keys are client-generated `Guid`s, which makes EF's change tracking unforgiving:

- **Never call `Update()` on an aggregate you loaded from the context.** It is already tracked; `SaveChangesAsync` picks up the changes. Calling `Update()` marks the whole graph as modified.
- **An entity you attach to a navigation property must be tracked.** If it is going to be assigned to a navigation, load it *without* `AsNoTracking()`. Use `AsNoTracking()` only when you read the entity purely to inspect its state or take its `Id`.

## Loading a related aggregate

```csharp
{Parent}? parent = await dbContext
    .{Parents}
    .SingleOrDefaultAsync(x => x.Id == command.{Parent}Id, cancellationToken);

if (parent is null)
{
    return Result.Fail<Guid>({Parent}Errors.NotFound);
}
```

## Validation rules reference

```csharp
// Strings — prefer the domain constant over a literal
RuleFor(x => x.Name)
    .NotEmpty()
    .MaximumLength({Entity}.MaxNameLength)
    .Matches("^[a-zA-Z]+$").WithMessage("Only letters allowed");

// Numbers
RuleFor(x => x.Amount)
    .GreaterThan(0)
    .InclusiveBetween(1, 100);

// Guids
RuleFor(x => x.Id)
    .NotEmpty().WithMessage("ID is required");

// Email
RuleFor(x => x.Email)
    .NotEmpty()
    .EmailAddress();

// Conditional
RuleFor(x => x.ParentId)
    .NotEmpty()
    .When(x => x.RequiresParent);

// Collections
RuleFor(x => x.Items)
    .NotEmpty()
    .Must(items => items.Count <= 10).WithMessage("Max 10 items");

// Custom
RuleFor(x => x.EndDate)
    .Must((command, endDate) => endDate > command.StartDate)
    .WithMessage("End date must be after start date");
```

Dates must be evaluated per call, not captured at validator construction:

```csharp
// ❌ WRONG — captures the value when the validator is built
.GreaterThanOrEqualTo(dateTimeProvider.Today)

// ✅ CORRECT — the lambda overload re-evaluates on every validation
.GreaterThanOrEqualTo(_ => dateTimeProvider.Today)
```

## Anti-patterns

```csharp
// ❌ WRONG: throwing for a business failure
if (entity is null)
    throw new NotFoundException("Entity not found");

// ✅ CORRECT: return a Result
if (entity is null)
{
    return Result.Fail<Guid>({Entity}Errors.NotFound);
}

// ❌ WRONG: business rule evaluated in the handler
if (command.Amount > 1000 && user.Level < 5)
    return Result.Fail(BaseError.Forbidden("Order.InsufficientLevel", "..."));

// ✅ CORRECT: the aggregate decides
Result result = entity.ProcessOrder(command.Amount, user);
if (!result.IsSuccessful)
{
    return Result.Fail(result.Error);
}

// ❌ WRONG: marking a tracked aggregate as modified
dbContext.{Entities}.Update({entity});

// ✅ CORRECT: it is already tracked
await dbContext.SaveChangesAsync(cancellationToken);
```
