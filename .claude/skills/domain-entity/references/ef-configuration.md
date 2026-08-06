# EF Core configuration

Maps domain entities to tables with the Fluent API, keeping the Domain layer persistence-ignorant.

## Core principles

1. Domain MUST remain persistence-ignorant
2. All mappings MUST be explicit (no reliance on implicit conventions)
3. One configuration per entity
4. Fluent API only — no data annotations
5. Configurations MUST be deterministic and version-safe

## Location

```
src/{name}.Infrastructure/Configurations/
```

Discovered automatically by `ApplyConfigurationsFromAssembly` — no manual registration.

## Global conventions already in effect

Two settings apply to every configuration, so do not restate them per entity:

- **Default schema `flowboard`** — `modelBuilder.HasDefaultSchema(DefaultSchema)` (`Database/ApplicationDbContext.cs`). Never pass a schema to `ToTable()`.
- **snake_case naming** — `.UseSnakeCaseNamingConvention()` (`DependencyInjection.cs`) rewrites every table and column name. This is why `HasColumnName` is unnecessary except on owned types, where the convention cannot infer the flattened column name.

## Naming

| Element | Convention | Example |
|--------|------------|--------|
| Configuration class | `{EntityName}Configuration` | `WorkItemConfiguration` |
| Table name | snake_case plural | `work_items` |
| Column names | snake_case (automatic) | `created_at` |

## Properties

- All properties MUST be configured explicitly
- String properties MUST have `HasMaxLength()`
- Required properties MUST have `IsRequired()`; optional ones MUST NOT
- Decimal properties MUST have `HasPrecision()`
- Foreign keys and relationships MUST be configured explicitly

Reuse the domain constant for lengths — never a literal. The entity or value object declares `public const int MaxNameLength`, and the configuration points at it, so the invariant and the column can never drift apart:

```csharp
builder.Property(x => x.Name)
    .IsRequired()
    .HasMaxLength(Project.MaxNameLength);

builder.Property(x => x.Amount)
    .HasPrecision(9, 2);
```

## Primary keys

```csharp
builder.HasKey(x => x.Id);
```

Always explicit. Single key unless genuinely composite.

## Relationships

Many-to-one:

```csharp
builder
    .HasOne<Project>()
    .WithMany(p => p.WorkItems)
    .HasForeignKey(x => x.ProjectId)
    .OnDelete(DeleteBehavior.Restrict);
```

One-to-one:

```csharp
builder
    .HasOne(x => x.Profile)
    .WithOne()
    .HasForeignKey<UserProfile>("user_id");
```

Many-to-many — prefer an explicit join entity:

```csharp
builder
    .HasMany(x => x.Tags)
    .WithMany();
```

## Private collection navigations (mandatory)

Domain aggregates encapsulate their children behind a private field exposed as a read-only collection:

```csharp
private readonly List<ProjectMember> _members = [];
public IReadOnlyCollection<ProjectMember> Members => _members.AsReadOnly();
```

EF Core cannot write through `AsReadOnly()`, so **every** such navigation MUST declare its backing field. Omitting this is the single most common mapping failure in this codebase — the collection silently fails to materialize:

```csharp
builder.HasMany(x => x.Members)
    .WithOne()
    .HasForeignKey(x => x.ProjectId)
    .OnDelete(DeleteBehavior.Cascade);

builder.Navigation(x => x.Members)
    .HasField("_members")
    .UsePropertyAccessMode(PropertyAccessMode.Field);
```

A navigation whose relationship is configured from the *other* side still needs its own `Navigation(...).HasField(...)` call:

```csharp
builder.Navigation(x => x.Flows)
    .HasField("_flows")
    .UsePropertyAccessMode(PropertyAccessMode.Field);
```

## Value objects

Configured as owned types, flattened into the parent table:

```csharp
builder.OwnsOne(x => x.Email, email =>
{
    email.Property(e => e.Value)
        .HasColumnName("email")
        .IsRequired()
        .HasMaxLength(Email.MaxLength);

    email.HasIndex(e => e.Value)
        .IsUnique();
});
```

A value object held in a private backing field (no public property) is mapped by name:

```csharp
builder.OwnsOne<Password>("Password", password =>
{
    password.Property(p => p.Hash)
        .HasColumnName("password_hash")
        .IsRequired()
        .HasMaxLength(Password.MaxHashLength);
});
```

## Owned collections

A collection of value objects uses `OwnsMany` with its own table, an explicit owner foreign key, and a composite key — plus the usual backing-field declaration:

```csharp
builder.OwnsMany(x => x.Roles, role =>
{
    role.ToTable("user_roles");

    role.WithOwner().HasForeignKey("UserId");

    role.HasKey("UserId", nameof(Role.Name));

    role.Property(r => r.Name)
        .IsRequired()
        .HasMaxLength(50);
});

builder.Navigation(x => x.Roles)
    .HasField("_roles")
    .UsePropertyAccessMode(PropertyAccessMode.Field);
```

## Enums

- Prefer `HasConversion<string>()` for **new** enums — readable in the database and stable against reordering of enum members
- **Do not change existing `HasConversion<int>()` mappings.** `Project.Status` and `ProjectChangeLog.NewStatus` are mapped to `int`. Converting them to `string` produces a destructive migration against existing data — leave them as they are

```csharp
builder.Property(x => x.Status)
    .IsRequired()
    .HasConversion<string>()
    .HasMaxLength(40);
```

## Indexes

- Index foreign keys that are frequently queried
- Index frequently filtered fields
- Add unique indexes where the domain implies uniqueness
- Avoid over-indexing

```csharp
builder.HasIndex(x => x.ProjectId);

builder.HasIndex(x => x.Email)
    .IsUnique();
```

## Concurrency, shadow properties, ignoring

```csharp
builder.Property<byte[]>("row_version")
    .IsRowVersion();

// Shadow property — audit or FK not exposed on the domain model
builder.Property<DateTime>("created_at")
    .IsRequired();

// Computed property that must not be persisted
builder.Ignore(x => x.FullName);
```

## Complete example

```csharp
internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(Project.MaxNameLength);

        builder.Property(x => x.Description)
            .HasMaxLength(Project.MaxDescriptionLength);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(ProjectCode.MaxLength);

        builder.OwnsOne(x => x.Color, color =>
        {
            color.Property(c => c.Value)
                .HasColumnName("color")
                .IsRequired()
                .HasMaxLength(Color.MaxLength);
        });

        builder.Property(x => x.CreatedOnUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedOnUtc);

        builder.HasOne<User>(x => x.Creator)
            .WithMany()
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Members)
            .WithOne()
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Members)
            .HasField("_members")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => x.Code)
            .IsUnique();
    }
}
```

Note the shape: domain constants for every length, the creator FK as `Restrict` and the child collection as `Cascade`, and a `Navigation(...).HasField(...)` for each private collection.

## Critical rules

1. Domain entities MUST NOT contain EF attributes
2. All configurations MUST be explicit
3. No business logic inside configurations
4. All relationships MUST be defined explicitly
5. Every private collection navigation MUST declare `HasField` + `PropertyAccessMode.Field`
6. String lengths MUST come from a domain constant, not a literal
7. Always define constraints and indexes
8. Avoid implicit cascade deletes unless intentional
9. Value objects as owned types

## Anti-patterns

❌ Using data annotations in domain entities
❌ Relying on EF Core conventions implicitly
❌ Mapping a private collection without `Navigation(...).HasField(...)`
❌ Hardcoding string lengths instead of reusing the domain constant
❌ Adding `HasColumnName` outside owned types (snake_case is already global)
❌ Passing a schema to `ToTable()` (the default schema is already set)
❌ Rewriting an existing `HasConversion<int>()` enum to `string`
❌ Overusing cascade deletes
❌ Ignoring indexes
