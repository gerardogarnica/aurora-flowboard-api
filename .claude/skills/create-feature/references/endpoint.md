# Minimal API endpoint

One file per use case under `Endpoints/{Feature}/`. The class implements `IBaseEndpoint` and is discovered by assembly scanning — never register it by hand.

The endpoint binds the request, dispatches, and maps the `Result`. Nothing else.

## Naming and verbs

| Operation | Class | Verb | Success |
|---|---|---|---|
| Create | `Create{Entity}` | POST | 201 Created |
| Update | `Update{Entity}` | PUT | 202 Accepted |
| Delete | `Delete{Entity}` | DELETE | 202 Accepted |
| State change | `{Verb}{Entity}` | PATCH | 202 Accepted |
| Get by id | `Get{Entity}ById` | GET | 200 OK |
| Get all | `Get{Entities}` | GET | 200 OK |

Routes are lowercase plural with constraints — `projects/{id:guid}`. The endpoint declares only the resource segment; the `/api/v1/flowboard` prefix is applied centrally by the route group.

## Create — POST

```csharp
// src/{name}.Api/Endpoints/{Feature}/Create{Entity}.cs
using {name}.Application.{Feature}.Create;

namespace {name}.Api.Endpoints.{Feature};

public sealed class Create{Entity} : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "{feature}",
            async (
                [FromBody] Create{Entity}Request request,
                ICommandHandler<Create{Entity}Command, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new Create{Entity}Command(
                    request.Name,
                    request.Description,
                    request.SortOrder);

                Result<Guid> result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    () => Results.Created(string.Empty, result.Value),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("Create{Entity}")
            .WithTags(EndpointTags.{Feature})
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record Create{Entity}Request(
        string Name,
        string? Description,
        int SortOrder);
}
```

The request record is `internal sealed` and **nested inside the endpoint class** — it belongs to this route and nothing else should reference it.

## Update — PUT

```csharp
// src/{name}.Api/Endpoints/{Feature}/Update{Entity}.cs
using {name}.Application.{Feature}.Update;

namespace {name}.Api.Endpoints.{Feature};

public sealed class Update{Entity} : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(
            "{feature}/{id:guid}",
            async (
                Guid id,
                [FromBody] Update{Entity}Request request,
                ICommandHandler<Update{Entity}Command> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new Update{Entity}Command(
                    id,
                    request.Name,
                    request.Description);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    () => Results.Accepted(string.Empty),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("Update{Entity}")
            .WithTags(EndpointTags.{Feature})
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record Update{Entity}Request(
        string Name,
        string? Description);
}
```

The id comes from the route, never from the body — that removes any route/body mismatch.

## Delete — DELETE

```csharp
public sealed class Delete{Entity} : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete(
            "{feature}/{id:guid}",
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
            .WithTags(EndpointTags.{Feature})
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
```

## State change — PATCH

A transition (archive, activate, hold, complete) is **not** a PUT. It gets its own endpoint class and `MapPatch`:

```csharp
app.MapPatch(
    "{feature}/{id:guid}/archive",
    async (
        Guid id,
        ICommandHandler<Archive{Entity}Command> handler,
        CancellationToken cancellationToken) =>
    {
        Result result = await handler.Handle(new Archive{Entity}Command(id), cancellationToken);

        return result.Match(() => Results.Accepted(string.Empty), ApiResponses.Problem);
    })
```

## Get by id — GET

```csharp
public sealed class Get{Entity}ById : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "{feature}/{id:guid}",
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
            .WithTags(EndpointTags.{Feature})
            .Produces<{Entity}Response>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
```

For a `Result<T>` the single-argument `Match(Results.Ok, ApiResponses.Problem)` overload passes the value through directly.

## Get all — GET

```csharp
public sealed class Get{Entities} : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "{feature}",
            async (
                [FromQuery(Name = "inactive")] bool showInactive,
                IQueryHandler<GetAll{Entities}Query, IReadOnlyCollection<{Entity}ListResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetAll{Entities}Query(showInactive);

                Result<IReadOnlyCollection<{Entity}ListResponse>> result =
                    await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("Get{Entities}")
            .WithTags(EndpointTags.{Feature})
            .Produces<IReadOnlyCollection<{Entity}ListResponse>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
```

## Authorization

Every endpoint calls `.RequireAuthorization()`. Admin-only routes take a policy:

```csharp
.RequireAuthorization(policy => policy.RequireRole(Role.Administrator.Name))
```

Only `auth/login` is anonymous. Authorization that depends on the *data* (only a project admin may update it) is a domain rule and belongs in the aggregate, returning `BaseError.Forbidden(...)` — not in the endpoint.

## Rules

1. Never inject `DbContext` or a domain service into an endpoint — only the handler interface.
2. No business logic and no branching on business state.
3. Never return a domain entity.
4. Always pass `CancellationToken` to `handler.Handle(...)`.
5. No manual validation — the validator runs in the pipeline.
6. Declare every status code the route can produce with `.Produces(...)`, so the OpenAPI document matches reality.
7. Do not put `[JsonStringEnumConverter]` on collection properties — configure enum serialization at the serializer-options level instead.
