---
name: ef-core-configuration
description: Generates Entity Framework Core configurations using Fluent API to map domain entities to database tables with proper relationships, constraints, and conventions in a Clean Architecture solution.
language: C#
framework: .NET
pattern: Clean Architecture
---

# EF Core Configuration Skill

## Overview

This skill defines rules and patterns for creating **Entity Framework Core configurations** using the Fluent API.

The goal is to:
- Map Domain entities to database tables
- Configure relationships, constraints, and indexes
- Ensure high performance and maintainability
- Keep Domain layer persistence-ignorant

All configurations MUST live in the Infrastructure layer.

### Core Principles

1. Domain MUST remain persistence-ignorant
2. All mappings MUST be explicit (no implicit conventions reliance)
3. One configuration per entity
4. Fluent API MUST be used (no data annotations)
5. Configurations MUST be deterministic and version-safe

### Location

All configurations must be placed in:

```
src/{name}.Infrastructure/Configurations/
```

## Quick Reference

| Configuration | Use |
|---------------|-----|
| `ToTable()` | Table name |
| `HasKey()` | Primary key |
| `Property()` | Column configuration |
| `HasOne/HasMany()` | Relationships |
| `OwnsOne()` | Value objects |
| `HasIndex()` | Database indexes |

### Naming Conventions

| Element | Convention | Example |
|--------|------------|--------|
| Configuration class | `{EntityName}Configuration` | `WorkItemConfiguration` |
| Table name | snake_case plural | `work_items` |
| Column names | snake_case | `created_at` |

## Configuration Structure

Each configuration MUST:

- Implement `IEntityTypeConfiguration<TEntity>`
- Define table name
- Define primary key
- Configure all properties explicitly
- Configure relationships
- Configure indexes
- Configure constraints

Basic example:

```csharp
internal sealed class WorkItemConfiguration : IEntityTypeConfiguration<WorkItem>
{
    public void Configure(EntityTypeBuilder<WorkItem> builder)
    {
        builder.ToTable("work_items");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => x.Title);
    }
}
```

## Configuration Rules

### Properties
- All properties MUST be configured explicitly
- String properties MUST have `HasMaxLength()`
- Required properties MUST have `IsRequired()`
- Optional properties MUST NOT have `IsRequired()`
- Decimal properties MUST have `HasPrecision()`
- Value objects MUST be configured with `OwnsOne()`
- Foreign keys MUST be configured explicitly
- Collections MUST be configured with `HasMany()`
- Relationships MUST be configured with `HasOne()` or `HasMany()`

```csharp
builder.Property(x => x.Name)
    .IsRequired()
    .HasMaxLength(100);

builder.Property(x => x.Amount)
    .HasPrecision(9, 2);
```

### Primary keys
- Always explicitly defined
- Use single key unless required otherwise

```csharp
builder.HasKey(x => x.Id);
```

### Relationships
- Many-to-One

```csharp
builder
    .HasOne<Project>()
    .WithMany(p => p.WorkItems)
    .HasForeignKey(x => x.ProjectId)
    .OnDelete(DeleteBehavior.Restrict);
```

- One-to-One

```csharp
builder
    .HasOne(x => x.Profile)
    .WithOne()
    .HasForeignKey<UserProfile>("user_id");
```

- Many-to-Many (Explicit Join Entity Recommended)

```csharp
builder
    .HasMany(x => x.Tags)
    .WithMany();
```

### Value Objects
- Value Objects MUST be configured as owned entities
- No separate table unless necessary
- Flatten into parent entity when possible

```csharp
builder.OwnsOne(x => x.Email, email =>
{
    email.Property(e => e.Value)
        .HasColumnName("email")
        .IsRequired()
        .HasMaxLength(40);
});
```

### Enum Configuration
- Always convert enums to string for readability and stability

```csharp
builder.Property(x => x.Status)
    .HasConversion<string>()
    .IsRequired()
    .HasMaxLength(40);
```

### Indexes
- Define index to foreign keys if they are frequently queried
- Define indexes on frequently queried fields
- Avoid over-indexing (only index what is necessary)
- Add unique constraints indexes where applicable

```csharp
builder.HasIndex(x => x.ProjectId);

builder.HasIndex(x => x.Email)
    .IsUnique();
```

### Concurrency
- Use concurrency tokens when needed

```csharp
builder.Property<byte[]>("row_version")
    .IsRowVersion();
```

### Shadow Properties
- Use shadow properties for audit fields or concurrency tokens that should not be exposed in the domain model
- Foreign keys when not exposed

```csharp
builder.Property<DateTime>("created_at")
    .IsRequired();
```

### Ignoring Properties
- Use `Ignore()` for properties that should not be mapped to the database
- This is useful for computed properties or properties that are not relevant for persistence

```csharp
builder.Ignore(x => x.FullName);
```

### Complete example

```csharp
internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(x => x.CreatedOnUtc)
            .IsRequired();

        builder.HasMany(x => x.Members)
            .WithOne()
            .HasForeignKey("project_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.Name);
    }
}
```

## Critical Rules

1. Domain entities MUST NOT contain EF attributes
2. All configurations MUST be explicit
3. No business logic inside configurations
4. All relationships MUST be defined explicitly
5. Always define constraints and indexes
6. Avoid implicit cascade deletes unless intentional
7. Use Fluent API, not attributes in domain entities
8. Value objects as owned types

## Anti-Patterns to Avoid

❌ Using Data Annotations in Domain entities  
❌ Relying on EF Core conventions implicitly  
❌ Not configuring string lengths  
❌ Using enum as int (default)  
❌ Overusing cascade deletes  
❌ Ignoring indexes

## Related Skills

- `domain-entity` - Generate a domain entities, child entities, and domain events
- `infrastructure-layer-setup` - Setup and configure the infrastructure layer