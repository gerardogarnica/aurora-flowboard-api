---
name: create-feature
description: Implements a vertical feature slice — classifies the use case as a command or a query, writes the Application slice (command/query, handler, validator, response DTO) and the Minimal API endpoint. Use when the user asks to implement a feature or use case from a spec, add an operation like "create/update/delete/assign/archive X", expose an endpoint or route, or fetch data with "get X by id" or "list all X". Also use when modifying an existing handler, validator, or endpoint.
argument-hint: <use case, e.g. "archive a project" or "list work items due this week">
---

# Create a feature

Classify the use case, write the Application slice, expose the endpoint.

The domain entity must already exist — if it does not, or a business rule has no home yet, start with the `domain-entity` skill.

## Workflow

1. **Classify.** A state change is a **command**; a read is a **query**. Name it verb + entity: `ArchiveProjectCommand`, `GetWorkItemsByProjectQuery`.
2. **Check the domain first.** If the operation needs a new aggregate method, a new `{Entity}Errors` entry, or a new domain event, add those before writing the handler. The rule belongs in the aggregate, never in the handler.
3. **Write the Application slice** in `src/{name}.Application/{Feature}/{UseCase}/`. Templates: [references/command-slice.md](references/command-slice.md) and [references/query-slice.md](references/query-slice.md).
4. **Write the endpoint** in `src/{name}.Api/Endpoints/{Feature}/{UseCase}.cs`. Template: [references/endpoint.md](references/endpoint.md).
5. **Write the tests** — handler tests and validator tests. Use the `unit-testing` skill.
6. **Verify** — launch the `dotnet-test-runner` agent (Agent tool, `subagent_type: dotnet-test-runner`). It runs `dotnet build` (warnings are errors here) and `dotnet test`, and reports only failures. Do not run those commands yourself, and do not consider the feature done until that agent comes back clean.

## Command or query

| | Command | Query |
|---|---|---|
| Interface | `ICommand` / `ICommand<T>` | `IQuery<TResponse>` |
| Handler | `ICommandHandler<TCommand>` / `<TCommand, TResponse>` | `IQueryHandler<TQuery, TResponse>` |
| Returns | `Result` / `Result<Guid>` | `Result<TResponse>` |
| Tracking | tracked load, then `SaveChangesAsync` | always `AsNoTracking()` |
| Validator | yes | **yes** — queries are validated too |

## Non-negotiable conventions

- **Folder = use case.** One folder per use case under `src/{name}.Application/{Feature}/` (e.g. `Projects/Archive/`) holding every file of that slice.
- **Handlers are `internal sealed`** with a primary constructor, and the method is `Handle` (not `HandleAsync`).
- **Handler names drop the `Command`/`Query` suffix** — `CreateProjectHandler`, not `CreateProjectCommandHandler`. Commands, queries and validators keep theirs.
- **Commands, queries and responses are `sealed record`s.** Commands and queries are `public`; handlers, validators and request DTOs are `internal`.
- **No manual DI registration.** `Scrutor` discovers handlers, validators and endpoints. Never
  touch `DependencyInjection.cs` for a new slice.
- **Return `Result`, never throw** for an expected failure. Errors come from `{Entity}Errors` in the Domain layer.
- **Validation lives in the validator**, not the handler. It runs automatically in the pipeline; the handler never checks input shape.
- **Data access through `IApplicationDbContext`.** Application must never reference Infrastructure or name an EF type in a handler signature. Raw, schema-qualified SQL inside a handler is an accepted choice here — there is no repository pattern.
- **Never call `Update()` on a tracked aggregate.** Primary keys are client-generated `Guid`s; load, mutate, `SaveChangesAsync`. An entity assigned to a navigation property must be tracked, so do not load that one `AsNoTracking()`.
- **Queries project straight to a DTO** with `.Select(...)` — never return a domain entity.
- **Endpoints implement `IBaseEndpoint`**, resolve the handler interface from DI, close with `result.Match(..., ApiResponses.Problem)`, tag with `EndpointTags`, and call `.RequireAuthorization()`.
- **Time comes from `IDateTimeProvider`**, never `DateTime.UtcNow` in a handler.
- **`CancellationToken` is threaded through every async call.**

## The behavior pipeline

Handlers are wrapped by decorators registered with `Scrutor`'s `Decorate(...)`. Execution order:

```
LoggingBehavior → PerformanceBehavior → ValidationBehavior → handler
```

Logging wraps validation so the log captures the full round-trip, including validation failures. All three decorate commands **and** queries.

## Naming

| Artifact | Pattern | Example |
|---|---|---|
| Command | `{Verb}{Entity}Command` | `ArchiveProjectCommand` |
| Query | `Get{X}Query` | `GetWorkItemsByProjectQuery` |
| Handler | `{Verb}{Entity}Handler` | `ArchiveProjectHandler` |
| Validator | `{Verb}{Entity}Validator` | `ArchiveProjectValidator` |
| Response | `{X}Response` | `WorkItemResponse` |
| Endpoint | `{UseCase}.cs` in `Endpoints/{Feature}/` | `Endpoints/Projects/ArchiveProject.cs` |
| Request | nested `internal sealed record {UseCase}Request` | inside the endpoint class |
