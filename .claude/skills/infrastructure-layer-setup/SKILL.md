---
name: infrastructure-layer-setup
description: Infrastructure layer implementation rules and patterns for Clean Architecture in .NET applications. Defines database access, external integrations, dependency injection, and cross-cutting concerns implementation.
language: C#
framework: .NET
pattern: Clean Architecture
---

# Infrastructure Layer Setup

## Overview

This skill defines the rules and patterns for implementing the Infrastructure layer in a Clean Architecture application.

The Infrastructure layer is responsible for:
- **Database access and configurations** - EF Core, Dapper
- **External services integration** - APIs, messaging, storage
- **Authentication and authorization implementations** - Identity, JWT, OAuth, policy-based authorization, role-based authorization, password hashing
- **Cross-cutting concerns** - logging, caching, outbox pattern, background jobs

Infrastructure implements interfaces defined in the Application and Domain layers.

## Dependency Rule

- Infrastructure depends on Application
- Infrastructure NEVER depends on API
- Infrastructure implements contracts defined in:
  - Application layer (DbContext, services)
  - Domain layer (repositories, domain services if applicable)

## Infrastructure layer structure

```
src/Aurora.Flowboard.Infrastructure/
├── Authentication/
├── Authorization/
├── Caching/
├── Configurations/
│ ├── {EntityName}Configuration.cs
│ └── {EntityName}Configuration.cs
├── Cryptography/
├── Database/
│ ├── Migrations/
│ ├── ApplicationDbContext.cs
│ └── {name}DbContextFactory.cs
├── DomainEvents/
│ ├── IDomainEventsDispatcher.cs
│ └── DomainEventsDispatcher.cs
├── Interceptors/
│ └── InsertOutboxMessagesInterceptor.cs
├── Outbox/
├── Time/
├── DependencyInjection.cs
└── GlobalUsings.cs
```

## Package References

```xml
<!-- {name}.Infrastructure.csproj -->
<ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
</ItemGroup>
<ItemGroup>
    <PackageReference Include="EFCore.NamingConventions" />
    <PackageReference Include="MassTransit" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
    <PackageReference Include="Quartz.Extensions.Hosting" />
</ItemGroup>
```

## Core Responsibilities

### 1. Database Access

- Use **EF Core as primary ORM**
- Use **DbContext abstraction from Application layer**
- Apply configurations using `IEntityTypeConfiguration<T>`

Rules:
- Do NOT write business logic in DbContext
- Do NOT expose DbContext directly outside Infrastructure

### 2. DbContext Implementation

- Implements `IApplicationDbContext`
- Configures schema, mappings, and conventions

Rules:
- Use `HasDefaultSchema`
- Apply configurations via assembly scanning
- Keep DbContext thin (no business logic)

```csharp
// src/{name}.Infrastructure/Database/ApplicationDbContext.cs
using {name}.Application.Abstractions.Data;
using {name}.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace {name}.Infrastructure.Database;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options), I{name}DbContext
{
    internal const string DEFAULT_SCHEMA = "{name}";

    public DbSet<{EntityName}> {EntityNamePlural} { get; set; } = null!;
    public DbSet<{EntityName}> {EntityNamePlural} { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DEFAULT_SCHEMA);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
```

### 3. Entity Configurations

- One configuration per entity
- Use Fluent API

Rules:
- Define table names, keys, relationships
- Avoid annotations in Domain entities
- Keep Domain layer persistence-ignorant

## Domain Events Handling

### 1. Outbox Pattern

- Persist domain events as outbox messages
- Use EF Core interceptor

Rules:
- Extract domain events from entities
- Serialize events
- Store them before transaction commit

### 2. Domain Events Dispatcher

- Dispatch events AFTER persistence

Rules:
- Resolve handlers from DI
- Execute handlers asynchronously
- Do not block main transaction

## Interceptors

Used for cross-cutting persistence concerns.

Example:
- Outbox message generation
- Auditing
- Soft delete

Rules:
- Must be stateless
- Must not contain business logic
- Must not call external services

Example interceptor for Outbox pattern:

```csharp
// src/{name}.Infrastructure/Interceptors/InsertOutboxMessagesInterceptor.cs
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace {name}.Infrastructure.Interceptors;

public sealed class InsertOutboxMessagesInterceptor : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            InsertOutboxMessages(eventData.Context);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void InsertOutboxMessages(DbContext context)
    {
        var outboxMessages = context
            .ChangeTracker
            .Entries<BaseEntity>()
            .Select(x => x.Entity)
            .Where(x => x.DomainEvents.Count > 0)
            .SelectMany(x =>
            {
                var domainEvents = x.DomainEvents.ToList();
                x.ClearDomainEvents();

                return domainEvents;
            })
            .Select(domainEvent => new OutboxMessage
            {
                Id = domainEvent.Id,
                Type = domainEvent.GetType().Name,
                Content = JsonConvert.SerializeObject(domainEvent, SerializerSettings.Instance),
                OccurredOnUtc = domainEvent.OccurredOnUtc,
                IsProcessed = false
            })
            .ToList();

        context.Set<OutboxMessage>().AddRange(outboxMessages);
    }
}
```

## External Services Integration

Infrastructure is the ONLY layer allowed to integrate with:

- External APIs
- Email providers
- Payment gateways
- File storage systems

Rules:
- Always depend on abstractions (interfaces)
- Never call external services directly from Application
- Implement resiliency (retry, timeout, circuit breaker)

## Authentication & Authorization

- Implement interfaces from Application layer:
  - IUserContext
  - ITokenProvider

Rules:
- JWT generation handled here
- Claims extraction handled here
- Do not leak framework-specific types to Application

## Dependency Injection

All Infrastructure services are registered in:

`DependencyInjection.cs`

Rules:
- Use extension methods
- Group registrations by concern:
  - Database
  - Authentication
  - External services
  - Background jobs

```csharp
// src/{name}.Infrastructure/DependencyInjection.cs
using {name}.Application.Abstractions.Data;
using {name}.Infrastructure.Authentication;
using {name}.Infrastructure.DomainEvents;
using {name}.Infrastructure.Interceptors;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace {name}.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration) => services
            .AddAuthenticationServices(configuration)
            .AddAuthorizationServices()
            .AddDatabaseConfiguration(configuration)
            .AddDomainEventsDispatcher()
            .AddHealthChecks(configuration)
            .AddOutboxPatternImplementation()
            .AddDateTimeServices();

    private static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // See jwt-authentication skill
        return services;
    }

    private static IServiceCollection AddAuthorizationServices(this IServiceCollection services)
    {
        // See permission-authorization skill
        return services;
    }

    private static IServiceCollection AddDatabaseConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // Database connection
        var connectionString = configuration.GetConnectionString("Database");

        services.AddDbContextFactory<ApplicationDbContext>((sp, options) =>
            options
                .UseNpgsql(
                    connectionString,
                    x => x.MigrationsHistoryTable(HistoryRepository.DefaultTableName, ApplicationDbContext.DEFAULT_SCHEMA))
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(sp.GetRequiredService<InsertOutboxMessagesInterceptor>()));

        // DbContextFactory implementations
        services.AddScoped<IDbContextFactory, DbContextFactory>();

        // IUnitOfWork implementation
        services.AddScoped<IDbContext>(sp => sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());

        // Entity Framework Core interceptors
        services.TryAddSingleton<InsertOutboxMessagesInterceptor>();

        return services;
    }

    private static IServiceCollection AddDomainEventsDispatcher(this IServiceCollection services)
    {
        services.AddTransient<IDomainEventsDispatcher, DomainEventsDispatcher>();

        return services;
    }

    private static void AddHealthChecks(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString("Database")!);
    }

    private static IServiceCollection AddOutboxPatternImplementation(this IServiceCollection services)
    {
        services.AddOptions<OutboxOptions>().BindConfiguration("Outbox");
        services.ConfigureOptions<ConfigureProcessOutboxJob>();

        return services;
    }

    private static IServiceCollection AddDateTimeServices(this IServiceCollection services)
    {
        services.TryAddSingleton<IDateTimeService, DateTimeService>();
        return services;
    }
}
```

## Database Configuration Rules

- Use snake_case naming convention
- Use migrations for schema changes
- Configure connection via `appsettings.json`

Rules:
- Never hardcode connection strings
- Use environment-based configuration
- Ensure migrations are deterministic

## Performance Considerations

- Use projections (`Select`) instead of loading full entities
- Avoid `Include` when projecting
- Use indexes for frequent queries
- Use pagination for large datasets

## Resilience & Reliability

- Use retry policies for external calls
- Configure timeouts
- Implement circuit breakers when needed

## Caching (Optional)

- Apply only for read operations (queries)
- Never cache commands

## Time Handling

- Use `IDateTimeProvider` abstraction
- Do NOT use `DateTime.UtcNow` directly

## Critical Rules

1. Infrastructure MUST implement interfaces, never define business contracts
2. Infrastructure MUST NOT contain business logic
3. Infrastructure MUST NOT depend on API layer
4. External integrations MUST be isolated here
5. DbContext MUST be accessed via abstraction
6. Domain MUST remain persistence-ignorant
7. Use Outbox Pattern for domain events
8. All configurations MUST be explicit (no magic behavior)

## Anti-Patterns to Avoid

❌ Business logic inside DbContext  
❌ Direct dependency on EF Core in Application layer  
❌ Calling external APIs from Application  
❌ Using static services for infrastructure concerns  
❌ Skipping abstractions for "quick implementations"  
❌ Blocking async calls (`.Result`, `.Wait()`)


## Related Skills

- `dotnet-clean-architecture` - Master C#/.NET solution following Clean Architecture principles
- `application-layer-setup` - Base abstractions for Application layer
- `ef-core-configuration` - Generate EF configurations