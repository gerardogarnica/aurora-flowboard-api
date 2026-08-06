---
name: domain-entity
description: Adds or changes a domain entity and maps it to the database — aggregate roots, child entities, value objects, domain events, error catalogs, and the EF Core configuration. Use when the user asks to add or change an entity, aggregate, value object, domain event, or {Entity}Errors class; when a business rule or invariant needs a home; when an entity needs a table, column, relationship, or index; or when asked where domain logic belongs.
argument-hint: <entity and its fields, e.g. "WorkItemTemplate with a name, priority and project">
---

# Domain entity

Write the domain type, then map it. An entity that is never mapped is never persisted, so both steps belong together.

Once the entity exists, continue with the `create-feature` skill for the use case and endpoint.

## Workflow

1. **Classify what you are writing.** Aggregate root, child entity, or value object. See the table below.
2. **Write the domain type** → `src/{name}.Domain/{Aggregate}/`. Templates: [references/aggregate-root.md](references/aggregate-root.md) and [references/value-objects.md](references/value-objects.md).
3. **Write the error catalog and domain events** → `{Entity}Errors.cs` and `Events/`. Template: [references/errors-and-events.md](references/errors-and-events.md).
4. **Write the EF configuration** → `src/{name}.Infrastructure/Configurations/{Entity}Configuration.cs`. Template: [references/ef-configuration.md](references/ef-configuration.md). Let *Persistence consequences* below drive what it must contain.
5. **Register the DbSet** — add `DbSet<{Entity}> {Plural}` to **both** `Application/Abstractions/Data/IApplicationDbContext.cs` and `Infrastructure/Database/ApplicationDbContext.cs`.
6. **Create the migration** — plain PascalCase name (`AddWorkItemTemplates`):

   ```
   dotnet ef migrations add {Name} --project src/{name}.Infrastructure --startup-project src/{name}.Api
   ```

7. **Verify** — launch the `dotnet-test-runner` agent (Agent tool, `subagent_type: dotnet-test-runner`). It runs `dotnet build` (warnings are errors here) and `dotnet test`, and reports only failures. Do not run those commands yourself, and do not consider the entity done until that agent comes back clean.

## What to write

| Concept | Purpose | Base type | Example |
|---------|---------|-----------|---------|
| Aggregate root | Entry point for the aggregate | `BaseEntity` | `Project`, `User` |
| Child entity | Part of an aggregate, never loaded alone | none | `ProjectMember`, `Comment` |
| Value object | Immutable, no identity | none (`sealed record`) | `Email`, `Color`, `ProjectCode` |
| Domain event | Signals a state change | `DomainEvent` | `ProjectCreatedDomainEvent` |

## Folder layout

`{Aggregate}` is the **plural PascalCase folder name** (`Projects`, `Users`, `WorkItems`). `{Entity}` is the singular type name (`Project`, `WorkItem`).

```
/Domain/{Aggregate}/
├── {Entity}.cs                    # Aggregate root
├── {Entity}Errors.cs              # Typed errors
├── {ChildEntity}.cs               # Child entity (if applicable)
├── {Entity}Status.cs              # Enumerations owned by this aggregate
└── Events/
    └── {Entity}CreatedDomainEvent.cs
```

Enums live in their owning aggregate folder, never in `Shared/`. Value objects shared across aggregates go in `Domain/Shared/` with their own `{ValueObject}Errors.cs`.

## Non-negotiable conventions

- **Private setters always.** `Id` is inherited from `BaseEntity` — never redeclare it.
- **Static factory `Create(...)` returning `Result<T>`.** Private constructors only. Never throw for an expected failure.
- **The aggregate root owns its children.** Child factories are `internal`; invariants that need the aggregate's state are enforced in the root, not the child.
- **Collections are private fields** exposed as `IReadOnlyCollection<T>`.
- **`AddDomainEvent` is a protected instance method.** Inside a static factory call it on the new instance (`entity.AddDomainEvent(...)`) — a bare call does not compile.
- **Timestamps are parameters**, never `DateTime.UtcNow` inside the entity. That keeps domain tests deterministic with no substitute.
- **Length limits are `public const int`** on the entity or value object; the EF configuration reuses them. No magic numbers — `.editorconfig` treats them as errors.
- **Prefer `Guid` parameters** over full entities when the method only needs the id. Take the entity only when the method reads its state (`AddMember(User user, ...)` checks `user.IsActive`).
- **Errors are `BaseError`**, built with `BaseError.Validation/NotFound/Conflict/Forbidden/Failure`. Codes are singular: `"Project.NotFound"`.
- **Entities stay persistence-ignorant** — no EF attributes in Domain.

## Persistence consequences

Each domain shape forces a specific mapping. Decide these while designing the entity:

| Shape in the domain | Required in the EF configuration |
|---|---|
| `private readonly List<T> _items` exposed as `IReadOnlyCollection<T> Items` | `builder.Navigation(x => x.Items).HasField("_items").UsePropertyAccessMode(PropertyAccessMode.Field)` — **without it EF cannot materialize the collection** |
| Value object (`sealed record` with `Value`) | `builder.OwnsOne(x => x.Vo, vo => vo.Property(v => v.Value).HasColumnName("vo").HasMaxLength(Vo.MaxLength))` |
| `public const int MaxLength` on the entity/VO | `HasMaxLength({Entity}.MaxLength)` — never the literal number |
| Enum property | `HasConversion<string>()` for new enums |
| Computed property (`FullName`) | `builder.Ignore(x => x.FullName)` |
| Navigation to the creator (`User Creator`) | `HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict)` |
| Child collection owned by the root | `HasMany(x => x.Children).WithOne().HasForeignKey(x => x.{Entity}Id).OnDelete(DeleteBehavior.Cascade)` |

## Notes

- Domain events are recorded by the aggregate and dispatched by `InsertOutboxMessagesInterceptor` on `SaveChanges`. A handler never publishes them itself.
- Application unit tests substitute `IApplicationDbContext` with NSubstitute and `MockDbSetHelper` — there is no `TestDbContext` to update. See the `unit-testing` skill.
