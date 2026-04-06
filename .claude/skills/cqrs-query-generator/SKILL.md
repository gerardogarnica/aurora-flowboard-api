---
name: cqrs-query-generator
description: Generates CQRS Queries with Handlers, Request and Response DTOs following Clean Architecture patterns. Queries represent read operations that return data transfer objects.
language: C#
framework: .NET
dependencies: FluentValidation
---

# CQRS Query Generator

## Overview

This skill generates Queries following the CQRS (Command Query Responsibility Segregation) pattern. Queries represents read-only operations that return data without modifying state. Each query has:

- **Query Record** - Immutable data structure with request parameters
- **Validator** - FluentValidation rules for input validation
- **Handler** - Business logic implementation returning Response DTO

## Quick Reference

| Query Type | Returns | Use Case |
|--------------|---------|----------|
| GetById | Single entity by ID | `Result<ResponseDto>` |
| GetAll | All entities (with optional filtering) | `Result<IReadOnlyCollection<ResponseDto>>` |
| GetPaged | Paginated list | `Result<PagedResult<ResponseDto>>` |
| Search | Filtered/searched results | `Result<IReadOnlyCollection<ResponseDto>>` |
| Exists | Check if entity exists | `Result<bool>` |

---

## Command Structure

```
/Application/{Feature}/
├── GetById/
│   ├── Get{Entity}ByIdQuery.cs         # Query Record
│   ├── Get{Entity}ByIdHandler.cs       # Handler
│   ├── Get{Entity}ByIdValidator.cs     # Validator (optional)
│   └── {Entity}Response.cs             # Response DTO
├── GetAll/
│   ├── GetAll{Entities}Query.cs
│   ├── GetAll{Entities}Handler.cs
│   └── {Entities}ListResponse.cs
└── GetBy{Parent}/
│   ├── Get{Entities}By{Parent}Query.cs
│   ├── Get{Entities}By{Parent}Handler.cs
│   ├── Get{Entities}By{Parent}Validator.cs
│   └── {Entities}By{Parent}Response.cs
```

---

## Template: Get By ID Query

```csharp
// src/{name}.Application/{Feature}/GetById/{Entity}Response.cs
namespace {name}.Application.{Feature}.GetById;

public sealed record {Entity}Response(
    Guid {Entity}Id,
    string Name,
    string? Description,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    {Entity}Type Type,
    int SortOrder,
    bool IsActive,
    string? Notes,
    DateTime CreatedOnUtc,
    DateTime UpdatedOnUtc);
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
    I{name}DbContext dbContext) : IQueryHandler<Get{Entity}ByIdQuery, {Entity}Response>
{
    public async Task<Result<{Entity}Response>> Handle(
        Get{Entity}ByIdQuery query,
        CancellationToken cancellationToken)
    {
        // Get {entity}
        Entity? {entity} = await dbContext
            .Entities
            .SingleOrDefaultAsync(x => x.Id == query.Id, cancellationToken);

        if ({entity} is null)
        {
            return Result.Fail({Entity}Errors.NotFound);
        }

        // Return {entity} model
        var response = new {Entity}Response(
            {entity}.Id,
            {entity}.Name,
            {entity}.Description,
            {entity}.Type,
            {entity}.SortOrder,
            {entity}.IsActive,
            {entity}.Notes,
            {entity}.CreatedOnUtc,
            {entity}.UpdatedOnUtc);

        return response;
    }
}
```

---

## Template: Get All Query

```csharp
// src/{name}.Application/{Feature}/GetAll/{Entities}ListResponse.cs
namespace {name}.Application.{Feature}.GetAll;

public sealed record {Entities}ListResponse(
    Guid {Entity}Id,
    string Name,
    string? Description,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    {Entity}Type Type,
    int SortOrder,
    bool IsActive,
    string? Notes);
```

```csharp
// src/{name}.Application/{Feature}/GetAll/GetAll{Entities}Query.cs
namespace {name}.Application.{Feature}.GetAll;

public sealed record GetAll{Entities}Query(bool ShowInactive) : IQuery<IReadOnlyCollection<{Entities}ListResponse>>;
```

```csharp
// src/{name}.Application/{Feature}/GetAll/GetAll{Entities}Handler.cs
namespace {name}.Application.{Feature}.GetAll;

internal sealed class GetAll{Entities}Handler(
    I{name}DbContext dbContext) : IQueryHandler<GetAll{Entities}Query, IReadOnlyCollection<{Entities}ListResponse>>
{
    public async Task<Result<IReadOnlyCollection<{Entities}ListResponse>>> Handle(
        GetAll{Entities}Query query,
        CancellationToken cancellationToken)
    {
        // Get {entities}
        var query = dbContext
            .{Entities}
            .AsNoTracking()
            .AsQueryable();

        if (!query.ShowInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        // Return {entities} response
        List<{Entity}> {entities} = await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);

        List<{Entities}ListResponse> {entities} = await query
            .OrderBy(x => x.Name)
            .Select({entity} => new {Entities}ListResponse(
                {entity}.Id,
                {entity}.Name,
                {entity}.Description,
                {entity}.Type,
                {entity}.SortOrder,
                {entity}.IsActive,
                {entity}.Notes))
            .ToListAsync(cancellationToken);

        return {entities};
    }
}
```

---

## Template: Get By Parent Query

```csharp
// src/{name}.Application/{Feature}/GetBy{Parent}/{Entities}By{Parent}Response.cs
namespace {name}.Application.{Feature}.GetBy{Parent};

public sealed record {Entities}By{Parent}Response(
    Guid {Entity}Id,
    string Name,
    string? Description,
    Guid {Parent}Id,
    string {Parent}Name,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    {Entity}Type Type);
```

```csharp
// src/{name}.Application/{Feature}/GetBy{Parent}/Get{Entities}By{Parent}Query.cs
namespace {name}.Application.{Feature}.GetBy{Parent};

public sealed record Get{Entities}By{Parent}Query(Guid {Parent}Id) : IQuery<IReadOnlyCollection<{Entities}By{Parent}Response>>;
```

```csharp
// src/{name}.Application/{Feature}/GetBy{Parent}/Get{Entities}By{Parent}Validator.cs
namespace {name}.Application.{Feature}.GetBy{Parent};

internal sealed class Get{Entities}By{Parent}Validator : AbstractValidator<Get{Entities}By{Parent}Query>
{
    public Get{Entities}By{Parent}Validator()
    {
        RuleFor(x => x.{Parent}Id)
            .NotEmpty();
    }
}
```

```csharp
// src/{name}.Application/{Feature}/GetBy{Parent}/Get{Entities}By{Parent}Handler.cs
namespace {name}.Application.{Feature}.GetBy{Parent};

internal sealed class Get{Entities}By{Parent}Handler(
    I{name}DbContext dbContext) : IQueryHandler<Get{Entities}By{Parent}Query, IReadOnlyCollection<{Entities}By{Parent}Response>>
{
    public async Task<Result<IReadOnlyCollection<{Entities}By{Parent}Response>>> Handle(
        Get{Entities}By{Parent}Query query,
        CancellationToken cancellationToken)
    {
        // Get {entities}
        var query = dbContext
            .{Entities}
            .Where(x => x.{Parent}Id == query.{Parent}Id)
            .AsNoTracking()
            .AsQueryable();

        // Return {entities} response
        List<{Entities}By{Parent}Response> {entities} = await query
            .Include(x => x.{Parent})
            .OrderBy(x => x.Name)
            .Select({entity} => new {Entities}By{Parent}Response(
                {entity}.Id,
                {entity}.Name,
                {entity}.Description,
                {entity}.{Parent}Id,
                {entity}.{Parent}.Name,
                {entity}.Type))
            .ToListAsync(cancellationToken);

        return {entities};
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

// Custom validation
RuleFor(x => x.DateRange)
    .Must(BeValidDateRange).WithMessage("End date must be after start date");

private bool BeValidDateRange(DateRange range) => range.End > range.Start;
```

---

## Critical Rules

1. **Queries are records** - Immutable, value equality
2. **One handler per query** - No shared handlers
3. **Validators are internal** - Not exposed outside Application layer
4. **Return DTOs, not entities** - Don't expose domain models
5. **Use IDbContext** - Use the DbContext interface to query data
6. **Always use CancellationToken** - Pass through all async calls
7. **Keep handlers focused** - One responsibility per handler

---

## Anti-Patterns to Avoid

```csharp
// ❌ WRONG: Returning domain entities
public sealed record GetEntityQuery(Guid Id) : IQuery<Entity>; // Exposes domain

// ✅ CORRECT: Return DTOs
public sealed record GetEntityQuery(Guid Id) : IQuery<EntityResponse>;

// ❌ WRONG: String concatenation in SQL
var sql = $"SELECT * FROM entity WHERE name = '{request.Name}'"; // SQL injection!

// ✅ CORRECT: Parameterized queries
var sql = "SELECT * FROM entity WHERE name = @Name";
await connection.QueryAsync(sql, new { request.Name });
```

---

## Related Skills

- `application-layer-setup` - Base abstractions for Application layer
- `cqrs-command-generator` - Generate CQRS commands with handlers and validators
- `domain-entity` - Generate a domain entities, child entities, and domain events