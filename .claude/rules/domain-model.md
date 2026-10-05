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
- A value object is a `sealed record` with a private constructor and a `public static Create` returning `Result<T>`: `Email`, `Color` (`Shared/`), `ProjectCode` (`Projects/`), `Password` (`Users/`).
- The marker is what the architecture tests select on, so a new value object **must** declare it — `ValueObjects_Should_BeMarkedWithValueObjectInterface` fails otherwise.
- Two types deliberately stay out: `BaseError` (an `Abstractions` record, not a domain concept) and `Role`, a closed enumeration-style `sealed class` with static instances and `FromName` instead of `Create`.
- EF Core maps every value object with `OwnsOne`, never `HasConversion`.
