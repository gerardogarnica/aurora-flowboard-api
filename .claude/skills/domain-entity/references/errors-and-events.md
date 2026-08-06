# Error catalogs and domain events

## Domain errors

The error type is `BaseError`. Build it with a static factory — the record's constructor takes three arguments `(Code, Message, ErrorType)`, so `new("Code", "Message")` does not compile.

**Pick the factory by semantics** — the category drives the HTTP status the endpoint returns:

| Factory | `BaseErrorType` | HTTP |
|---|---|---|
| `BaseError.Validation` | `Validation` | 400 |
| `BaseError.Forbidden` | `Forbidden` | 403 |
| `BaseError.NotFound` | `NotFound` | 404 |
| `BaseError.Conflict` | `Conflict` | 409 |
| `BaseError.Failure` | `Failure` | 500 |

Error codes are `"{Entity}.{Reason}"` — the entity name is **singular**: `"Project.NotFound"`, not `"Projects.NotFound"`.

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

## Domain events

One class per file, in the aggregate's `Events/` subfolder. Inherit `DomainEvent`, which supplies `Id` and `OccurredOnUtc` and implements `IDomainEvent`.

Events carry **ids and value types, never entity instances**.

```csharp
// src/{name}.Domain/{Aggregate}/Events/{Entity}CreatedDomainEvent.cs
namespace {name}.Domain.{Aggregate}.Events;

public sealed class {Entity}CreatedDomainEvent(Guid {entity}Id) : DomainEvent
{
    public Guid {Entity}Id { get; init; } = {entity}Id;
}
```

An event may carry extra context when a subscriber would otherwise have to re-query for it — enums and ids are fine, entities are not:

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

## How events get published

The aggregate only *records* the event with `AddDomainEvent(...)`. Persisting and dispatching is Infrastructure's job: `InsertOutboxMessagesInterceptor` extracts the events on `SaveChanges` and writes them as outbox messages inside the same transaction.

A command handler must never publish a domain event itself — it calls the aggregate method and saves.
