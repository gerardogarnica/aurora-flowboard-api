---
paths:
  - "src/Aurora.Flowboard.Domain/**/*.cs"
  - "src/Aurora.Flowboard.Infrastructure/Configurations/**/*.cs"
---

# Domain model conventions

## Enums live in their owning aggregate folder

Place enum types in `src/Aurora.Flowboard.Domain/{Aggregate}s/` next to the entity that owns them (`ProjectRole`, `WorkItemType`, `Priority`...), never in `Shared/`. `Shared/` is only for truly cross-aggregate value objects (`Email`, `Color`).

## Value objects

- Marked with `IValueObject` (`Domain/Abstractions/IValueObject.cs`, an empty marker interface).
- A value object is a `sealed record` with a private constructor and a `public static Create` returning `Result<T>`: `Email`, `Color` (`Shared/`), `ProjectCode`, `FlowStateDefinition` (`Projects/`), `Password` (`Users/`).
- The marker is what the architecture tests select on, so a new value object **must** declare it — `ValueObjects_Should_BeMarkedWithValueObjectInterface` fails otherwise.
- Two types deliberately stay out: `BaseError` (an `Abstractions` record, not a domain concept) and `Role`, a closed enumeration-style `sealed class` with static instances and `FromName` instead of `Create`.
- EF Core maps every persisted value object with `OwnsOne`, never `HasConversion`. `FlowStateDefinition` is not persisted: it is the input of `Project.Create`, which turns it into `FlowState` entities.

## Project status lifecycle depends on its kind

- `Product`/`Internal` are continuous: `Active ↔ Maintenance`, both `→ Archived`. `Client`/`Research` are timeboxed: `Active → Completed → Archived`, `Active → Archived`. `Maintenance` exists only in the continuous lifecycle and `Completed` only in the timeboxed one.
- `Project.ChangeKind` fails with `ProjectErrors.KindNotSupportedInCurrentStatus` when the current status is not part of the target kind's lifecycle (e.g. a `Product` in `Maintenance` cannot become `Client`), so a project can never be left with no valid status transition. Moving between two kinds of the same lifecycle is always allowed while the project is modifiable.
