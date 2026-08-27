# Query slice

A query is a read with no state change. Every query slice lives in its own folder:

```
/Application/{Feature}/{UseCase}/
├── Get{Entity}ByIdQuery.cs
├── Get{Entity}ByIdHandler.cs
├── Get{Entity}ByIdValidator.cs
└── {Entity}Response.cs
```

**Queries do get validated.** `ValidationBehavior.QueryHandler` is registered
(`Application/DependencyInjection.cs`), so a validator on a query runs exactly like one on a command. Write one whenever the query has an input worth checking.

| Shape | Returns |
|---|---|
| Get by Id | `Result<{Entity}Response>` |
| Get all / filtered | `Result<IReadOnlyCollection<{Entity}ListResponse>>` |
| Exists | `Result<bool>` |

## Get by Id

```csharp
// src/{name}.Application/{Feature}/GetById/{Entity}Response.cs
namespace {name}.Application.{Feature}.GetById;

public sealed record {Entity}Response(
    Guid Id,
    string Name,
    string? Description,
    {Entity}Type Type,
    int SortOrder,
    bool IsActive,
    DateTime CreatedOnUtc,
    DateTime? UpdatedOnUtc);
```

```csharp
// src/{name}.Application/{Feature}/GetById/Get{Entity}ByIdQuery.cs
namespace {name}.Application.{Feature}.GetById;

public sealed record Get{Entity}ByIdQuery(Guid Id) : IQuery<{Entity}Response>;
```

```csharp
// src/{name}.Application/{Feature}/GetById/Get{Entity}ByIdValidator.cs
namespace {name}.Application.{Feature}.GetById;

internal sealed class Get{Entity}ByIdValidator : AbstractValidator<Get{Entity}ByIdQuery>
{
    public Get{Entity}ByIdValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("{Entity} ID is required");
    }
}
```

```csharp
// src/{name}.Application/{Feature}/GetById/Get{Entity}ByIdHandler.cs
namespace {name}.Application.{Feature}.GetById;

internal sealed class Get{Entity}ByIdHandler(
    IApplicationDbContext dbContext) : IQueryHandler<Get{Entity}ByIdQuery, {Entity}Response>
{
    public async Task<Result<{Entity}Response>> Handle(
        Get{Entity}ByIdQuery query,
        CancellationToken cancellationToken)
    {
        {Entity}Response? response = await dbContext
            .{Entities}
            .AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new {Entity}Response(
                x.Id,
                x.Name,
                x.Description,
                x.Type,
                x.SortOrder,
                x.IsActive,
                x.CreatedOnUtc,
                x.UpdatedOnUtc))
            .SingleOrDefaultAsync(cancellationToken);

        if (response is null)
        {
            return Result.Fail<{Entity}Response>({Entity}Errors.NotFound);
        }

        return response;
    }
}
```

Project straight into the response inside the query — do not materialize the entity and map afterwards. The database returns only the columns the DTO needs.

## Get all, with a filter

```csharp
// src/{name}.Application/{Feature}/GetAll/{Entity}ListResponse.cs
namespace {name}.Application.{Feature}.GetAll;

public sealed record {Entity}ListResponse(
    Guid Id,
    string Name,
    string? Description,
    {Entity}Type Type,
    bool IsActive);
```

```csharp
// src/{name}.Application/{Feature}/GetAll/GetAll{Entities}Query.cs
namespace {name}.Application.{Feature}.GetAll;

public sealed record GetAll{Entities}Query(bool ShowInactive)
    : IQuery<IReadOnlyCollection<{Entity}ListResponse>>;
```

```csharp
// src/{name}.Application/{Feature}/GetAll/GetAll{Entities}Handler.cs
namespace {name}.Application.{Feature}.GetAll;

internal sealed class GetAll{Entities}Handler(
    IApplicationDbContext dbContext)
    : IQueryHandler<GetAll{Entities}Query, IReadOnlyCollection<{Entity}ListResponse>>
{
    public async Task<Result<IReadOnlyCollection<{Entity}ListResponse>>> Handle(
        GetAll{Entities}Query query,
        CancellationToken cancellationToken)
    {
        // Name the queryable something other than `query` — that name is taken by the parameter
        IQueryable<{Entity}> source = dbContext
            .{Entities}
            .AsNoTracking();

        if (!query.ShowInactive)
        {
            source = source.Where(x => x.IsActive);
        }

        List<{Entity}ListResponse> {entities} = await source
            .OrderBy(x => x.Name)
            .Select(x => new {Entity}ListResponse(
                x.Id,
                x.Name,
                x.Description,
                x.Type,
                x.IsActive))
            .ToListAsync(cancellationToken);

        return {entities};
    }
}
```

## Get by parent

```csharp
// src/{name}.Application/{Feature}/GetBy{Parent}/Get{Entities}By{Parent}Handler.cs
namespace {name}.Application.{Feature}.GetBy{Parent};

internal sealed class Get{Entities}By{Parent}Handler(
    IApplicationDbContext dbContext)
    : IQueryHandler<Get{Entities}By{Parent}Query, IReadOnlyCollection<{Entity}By{Parent}Response>>
{
    public async Task<Result<IReadOnlyCollection<{Entity}By{Parent}Response>>> Handle(
        Get{Entities}By{Parent}Query query,
        CancellationToken cancellationToken)
    {
        List<{Entity}By{Parent}Response> {entities} = await dbContext
            .{Entities}
            .AsNoTracking()
            .Where(x => x.{Parent}Id == query.{Parent}Id)
            .OrderBy(x => x.Name)
            .Select(x => new {Entity}By{Parent}Response(
                x.Id,
                x.Name,
                x.{Parent}Id,
                x.{Parent}.Name))
            .ToListAsync(cancellationToken);

        return {entities};
    }
}
```

A `.Select(...)` that reaches through a navigation (`x.{Parent}.Name`) generates the join by itself — no `.Include(...)` needed, and `.Include(...)` would be wasted work when projecting.

## Rules

- Queries are `sealed record`s implementing `IQuery<TResponse>`; one handler each.
- Handlers and validators are `internal sealed`.
- **Return DTOs, never domain entities.**
- Always `AsNoTracking()` — nothing in a query is meant to be saved.
- Always thread `CancellationToken`.
- No `Result.Ok(...)` needed: `Result<T>` converts implicitly from `T`.

## Anti-patterns

```csharp
// ❌ WRONG: exposing the domain model
public sealed record Get{Entity}Query(Guid Id) : IQuery<{Entity}>;

// ✅ CORRECT: a response DTO
public sealed record Get{Entity}Query(Guid Id) : IQuery<{Entity}Response>;

// ❌ WRONG: materialize then map in memory
List<{Entity}> entities = await dbContext.{Entities}.ToListAsync(cancellationToken);
return entities.Select(e => new {Entity}Response(...)).ToList();

// ✅ CORRECT: project in the database
return await dbContext.{Entities}
    .Select(e => new {Entity}Response(...))
    .ToListAsync(cancellationToken);

// ❌ WRONG: string interpolation into SQL
string sql = $"SELECT * FROM {table} WHERE name = '{query.Name}'";

// ✅ CORRECT: parameters
string sql = "SELECT * FROM flowboard.entities WHERE name = @Name";
```
