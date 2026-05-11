---
name: minimal-api-endpoints
description: Generates Minimal API endpoints for operations (Create, Update, Delete, GetById, GetAll, etc) integrated with CQRS and Clean Architecture principles.
language: C#
framework: .NET
pattern: Minimal API + Clean Architecture
---

# Minimal API Endpoints Skill

## Overview

This skill defines rules and patterns for generating **Minimal API endpoints** in a .NET application using:

- Minimal APIs
- CQRS (Commands & Queries)
- Clean Architecture

Endpoints must be thin, delegating all logic to the Application layer.

## Core Principles

1. Endpoints MUST be inherits from `IBaseEndpoint`
2. Endpoints MUST be thin (no business logic)
3. Endpoints MUST delegate to command and query handlers in the Application layer
4. Endpoints MUST return standardized results
5. Endpoints MUST use proper HTTP semantics
6. Endpoints MUST support cancellation tokens
7. Endpoints MUST be grouped by feature

## Endpoint Structure

All endpoints must follow this structure:

- Route definition
- Request mapping
- Handler invocation
- Result mapping

## Naming Conventions

| Operation | Convention | Type | Example |
|------|------------|---------|---------|
| Create | Create{Feature} | POST | CreateWorkItem |
| Update | Update{Feature} | PUT | UpdateWorkItem |
| Delete | Delete{Feature} | DELETE | DeleteWorkItem |
| GetById | Get{Feature}ById | GET | GetWorkItemById |
| GetAll | Get{Feature}s | GET | GetWorkItems |
| Move | Move{Feature} | PATCH | MoveWorkItem |

## Create Endpoint

### Rules

* Use POST
* Accept request body
* Return 201 Created
* Use CreatedAtRoute when possible

### Example

```csharp
// src/{name}.Api/Endpoints/{Feature}s/Create{Entity}.cs
namespace {name}.Api.Endpoints.{Feature}s;

public sealed class Create{Entity} : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "{feature}s",
            async (
                [FromBody] Create{Entity}Request request,
                ICommandHandler<Create{Entity}Command, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new Create{Entity}Command(
                    request.Name,
                    request.Description,
                    request.Type,
                    request.SortOrder,
                    request.Notes);

                Result<Guid> result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    () => Results.Created(string.Empty, result.Value),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("Create{Entity}")
            .WithTags(EndpointTags.{Feature}s)
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record Create{Entity}Request(
        string Name,
        string? Description,
        [property: JsonConverter(typeof(JsonStringEnumConverter))]
        EntityType Type,
        int SortOrder,
        string? Notes);
}
```

## Update Endpoint

### Rules

* Use PUT
* Require ID in route
* Validate route vs body consistency

### Example

```csharp
// src/{name}.Api/Endpoints/{Feature}s/Update{Entity}.cs
namespace {name}.Api.Endpoints.{Feature}s;

public sealed class Update{Entity} : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(
            "{feature}s/{id}",
            async (
                Guid id,
                [FromBody] Update{Entity}Request request,
                ICommandHandler<Update{Entity}Command> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new Update{Entity}Command(
                    id,
                    request.Name,
                    request.Description,
                    request.Type,
                    request.SortOrder,
                    request.Notes);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    () => Results.Accepted(string.Empty),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("Update{Entity}")
            .WithTags(EndpointTags.{Feature}s)
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record Update{Entity}Request(
        string Name,
        string? Description,
        [property: JsonConverter(typeof(JsonStringEnumConverter))]
        EntityType Type,
        int SortOrder,
        string? Notes);
}
```

## Delete Endpoint

### Rules

* Use DELETE
* Require ID in route
* Return 202 Accepted

### Example

```csharp
// src/{name}.Api/Endpoints/{Feature}s/Delete{Entity}.cs
namespace {name}.Api.Endpoints.{Feature}s;

public sealed class Delete{Entity} : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete(
            "{feature}s/{id}",
            async (
                Guid id,
                ICommandHandler<Delete{Entity}Command> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new Delete{Entity}Command(id);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    () => Results.Accepted(string.Empty),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("Delete{Entity}")
            .WithTags(EndpointTags.{Feature}s)
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
```

## Get By Id Endpoint

### Rules

* Use GET
* Return 200 OK
* Return 404 if not found

### Example

```csharp
// src/{name}.Api/Endpoints/{Feature}s/Get{Entity}ById.cs
namespace {name}.Api.Endpoints.{Feature}s;

public sealed class Get{Entity}ById : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "{feature}s/{id}",
            async (
                Guid id,
                IQueryHandler<Get{Entity}ByIdQuery, {Entity}Response> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new Get{Entity}ByIdQuery(id);

                Result<{Entity}Response> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("Get{Entity}ById")
            .WithTags(EndpointTags.{Feature}s)
            .Produces<{Entity}Response>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
```

## Get All Endpoint

### Rules

* Use GET
* Return collection
* Support filtering/pagination if needed

### Example

```csharp
// src/{name}.Api/Endpoints/{Feature}s/Get{Entities}.cs
namespace {name}.Api.Endpoints.{Feature}s;

public sealed class Get{Entities} : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "{feature}s",
            async (
                [FromQuery(Name = "deleted")] bool showDeleted,
                IQueryHandler<GetAll{Entities}Query, IReadOnlyCollection<{Entity}ListResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetAll{Entities}Query(showDeleted);

                Result<IReadOnlyCollection<{Entity}ListResponse>> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("Get{Entities}")
            .WithTags(EndpointTags.{Feature}s)
            .Produces<IReadOnlyCollection<{Entity}ListResponse>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
```

## Request Models

* Use separate request DTOs
* Never expose Domain entities

Example:

```csharp
public sealed record Create{Entity}Request(string Name);
public sealed record Update{Entity}Request(string Name);
```

## Routing Conventions

* Use plural resource names
* Use kebab-case or lowercase
* Use route constraints

Examples:

* /api/{entity}s
* /api/{entity}s/{id:guid}

## Critical Rules


1. Do NOT inject DbContext or services directly into endpoints
2. Do NOT contain business logic in endpoints
3. Do NOT return domain entities from endpoints
4. Always include CancellationToken in handler calls
5. Do NOT validate manually inside endpoints - delegate to FluentValidation or similar
6. Logging handled via pipeline behaviors or middleware, not inside endpoints unless necessary
7. Prefer centralized handling via behaviors and middleware for cross-cutting concerns (validation, logging, error handling)

## Anti-Patterns

❌ Business logic inside endpoints  
❌ Direct DbContext usage  
❌ Returning Domain entities  
❌ Ignoring cancellation tokens  
❌ Manual validation inside endpoints

## Related Skills

- `application-layer-setup` - Base abstractions for Application layer
- `cqrs-command-generator` - Generate CQRS commands with handlers and validators
- `cqrs-query-generator` - Generate CQRS queries with handlers