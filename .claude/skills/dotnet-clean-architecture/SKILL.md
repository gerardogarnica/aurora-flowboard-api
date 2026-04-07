---
name: dotnet-clean-architecture
description: Master C#/.NET solution following Clean Architecture principles with proper layer separation (API, Application, Domain, Infrastructure) and patterns for building robust APIs, and enterprise applications. Creates project structure, dependency injection setup, and cross-cutting concerns configuration.
---

# .NET Clean Architecture Project Scaffolder

## Overview

This skill generates a complete .NET solution following Clean Architecture (also known as Onion Architecture or Hexagonal Architecture). The architecture enforces separation of concerns through distinct layers with unidirectional dependencies pointing inward.

## When to Use This Skill

- Developing new .NET Web APIs or MCP servers
- Reviewing C# code for quality and performance
- Designing service architectures with dependency injection
- Implementing caching strategies
- Writing unit and integration tests
- Optimizing database access with EF Core or Dapper
- Configuring applications with IOptions pattern
- Handling errors and implementing resilience patterns

## Architecture Layers

| Layer | Scope |
|------|----------------|
| Domain | Entities, Value Objects, Domain Events, Interfaces |
| Application | Commands, Queries, Handlers, Validators, DTOs, Behaviors |
| Infrastructure | EF Core, Authentication, External Services, Cross-Cutting Concerns |
| API | Minimal APIs, Middlewares, Response formatting |

**Dependency Rule**: Dependencies point inward. Domain has no dependencies. Application depends only on Domain. Infrastructure implements interfaces from Application. API depends on Infrastructure.

## Quick Reference

| Task | Command/Action |
|------|----------------|
| Create solution | `dotnet new sln -n {SolutionName}.slnx` |
| Create Domain project | `dotnet new classlib -n {name}.Domain` |
| Create Application project | `dotnet new classlib -n {name}.Application` |
| Create Infrastructure project | `dotnet new classlib -n {name}.Infrastructure` |
| Create API project | `dotnet new webapi -n {name}.Api` |
| Add project to solution | `dotnet sln add src/{project}/{project}.csproj` |
| Add project reference | `dotnet add reference ../other/other.csproj` |

---

## Project Structure

```
{SolutionName}/
├── src/
│   ├── {name}.Domain/
│   │   ├── Abstractions/
│   │   │   ├── BaseEntity.cs
│   │   │   ├── BaseError.cs
│   │   │   ├── IDomainEvent.cs
│   │   │   └── Result.cs
│   │   ├── {Aggregate}/
│   │   │   ├── {Entity}.cs
│   │   │   ├── {Entity}Errors.cs
│   │   │   ├── Events/
│   │   ├── {Shared}/
│   │   │   ├── {ValueObject1}.cs
│   │   │   ├── {ValueObject2}.cs
│   │   ├── GlobalUsings.cs
│   │   └── {name}.Domain.csproj
│   │
│   ├── {name}.Application/
│   │   ├── Abstractions/
│   │   │   ├── Behaviors/
│   │   │   │   ├── LoggingBehavior.cs
│   │   │   │   └── ValidationBehavior.cs
│   │   │   ├── Messaging/
│   │   │   │   ├── ICommand.cs
│   │   │   │   ├── ICommandHandler.cs
│   │   │   │   ├── IDomainEventHandler.cs
│   │   │   │   ├── IQuery.cs
│   │   │   │   └── IQueryHandler.cs
│   │   │   ├── Time/
│   │   │   │   ├── IDateTimeService.cs
│   │   │   ├── Validations/
│   │   │   │   ├── RuleBuilderOptionsExtensions.cs
│   │   │   │   └── ValidationError.cs
│   │   │   ├── Authentication/
│   │   │   ├── Data/
│   │   │   └── Exceptions/
│   │   ├── {Feature}/
│   │   │   ├── Create/
│   │   │   ├── Update/
│   │   │   ├── Delete/
│   │   │   ├── GetById/
│   │   │   └── GetAll/
│   │   ├── DependencyInjection.cs
│   │   ├── GlobalUsings.cs
│   │   └── {name}.Application.csproj
│   │
│   ├── {name}.Infrastructure/
│   │   ├── Authentication/
│   │   ├── Authorization/
│   │   ├── Configurations/
│   │   │   ├── {Entity1}Configuration.cs
│   │   │   └── {Entity2}Configuration.cs
│   │   ├── Database/
│   │   │   ├── Migrations/
│   │   │   └── ApplicationDbContext.cs
│   │   ├── DomainEvents/
│   │   │   ├── IDomainEventsDispatcher.cs
│   │   │   └── DomainEventsDispatcher.cs
│   │   ├── Interceptors/
│   │   │   └── InsertOutboxMessagesInterceptor.cs
│   │   ├── Outbox/
│   │   ├── Time/
│   │   ├── DependencyInjection.cs
│   │   ├── GlobalUsings.cs
│   │   └── {name}.Infrastructure.csproj
│   │
│   └── {name}.Api/
│       ├── Endpoints/
│       │   └── {Feature}/
│       ├── Middlewares/
│       ├── Extensions/
│       ├── Responses/
│       ├── Program.cs
│       ├── DependencyInjection.cs
│       ├── GlobalUsings.cs
│       ├── appsettings.json
│       └── {name}.Api.csproj
│
├── tests/
│   ├── {name}.ArchitectureTests/
│   ├── {name}.Domain.UnitTests/
│   ├── {name}.Application.UnitTests/
│   └── {name}.Api.UnitTests/
│
└── {SolutionName}.slnx
```

---

## 1. Create Solution and Projects

```bash
# Create solution
dotnet new sln -n {SolutionName}

# Create projects
dotnet new classlib -n {name}.Domain -o src/{name}.Domain
dotnet new classlib -n {name}.Application -o src/{name}.Application
dotnet new classlib -n {name}.Infrastructure -o src/{name}.Infrastructure
dotnet new webapi -n {name}.Api -o src/{name}.Api

# Add projects to solution
dotnet sln add src/{name}.Domain/{name}.Domain.csproj
dotnet sln add src/{name}.Application/{name}.Application.csproj
dotnet sln add src/{name}.Infrastructure/{name}.Infrastructure.csproj
dotnet sln add src/{name}.Api/{name}.Api.csproj

# Add project references
cd src/{name}.Application
dotnet add reference ../{name}.Domain/{name}.Domain.csproj

cd ../{name}.Infrastructure
dotnet add reference ../{name}.Application/{name}.Application.csproj

cd ../{name}.Api
dotnet add reference ../{name}.Infrastructure/{name}.Infrastructure.csproj
```

---

## 2. Infrastructure Layer Setup

### Package References

```xml
<!-- {name}.Infrastructure.csproj -->
<ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
</ItemGroup>
<ItemGroup>
    <PackageReference Include="Dapper" />
    <PackageReference Include="EFCore.NamingConventions" />
    <PackageReference Include="MassTransit" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
    <PackageReference Include="Quartz.Extensions.Hosting" />
</ItemGroup>
```

### Application DbContext

```csharp
// src/{name}.Infrastructure/Database/ApplicationDbContext.cs
using {name}.Application.Abstractions.Data;
using {name}.Application.Abstractions.Time;
using {name}.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace {name}.Infrastructure.Database;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext, I{name}DbContext
{
    internal const string DEFAULT_SCHEMA = "{name}";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DEFAULT_SCHEMA);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
```

### Outbox Messages interceptor

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

### Dependency Injection

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

---

## 5. API Layer Setup

### Program.cs

```csharp
// src/{name}.Api/Program.cs
using {name}.Api;
using {name}.Api.Endpoints;
using {name}.Api.Extensions;
using {name}.Application;
using {name}.Infrastructure;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder
    .AddApiServices()
    .AddErrorHandling()
    .AddObservability();

builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration)
    .AddEndpoints();

var app = builder.Build();

RouteGroupBuilder routeGroup = app.MapGroup("{companyname}/{productname}/");

app.MapEndpoints(routeGroup);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    await app.ApplyMigrationsAsync();
}

app.UseHttpsRedirection();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

await app.RunAsync();

// REMARK: Required for functional and integration tests to work.
namespace {name}.Api
{
    public partial class Program;
}
```

---

## Naming Conventions

| Item | Convention | Example |
|------|------------|---------|
| Solution | PascalCase with withespaces | `Aurora Flowboard` |
| Projects | PascalCase with dots | `Aurora.Flowboard.Domain` |
| Namespaces | PascalCase | `Aurora.Flowboard.Domain.Users` |
| Classes | PascalCase | `WorkItem` |
| Interfaces | IPascalCase | `IDomainEvent` |
| Commands | {Action}{Entity}Command | `CreateUserCommand` |
| Queries | Get{Entity}Query | `GetUserByIdQuery` |
| Handlers | {Action}{Entity}Handler / Get{Entity}Handler | `CreateUserHandler` |
| Validators | {Action}{Entity}Validator / Get{Entity}Validator | `CreateUserValidator` |
| Responses | {Entity}Response | `UserResponse` |
| Domain Events | {Entity}{Action}DomainEvent | `UserCreatedDomainEvent` |
| Errors | {Entity}Errors | `UserErrors` |

---

## Critical Rules

1. **Domain has ZERO dependencies** on other layers or external packages
2. **Application depends only on Domain** - no infrastructure concerns
3. **Infrastructure implements interfaces** defined in Domain/Application
4. **API only references Infrastructure** - never Domain directly for services
5. **Inject dependencies** through constructor injection
6. **Use primary constructor** for declaring constructor parameters in the class, record or struct definition
7. **Use Result pattern** instead of exceptions for business logic errors
8. **Commands modify state**, Queries read state (CQRS)
9. **One handler per Command/Query** - no shared handlers
10. **Don't use repository pattern**, use of DbContext interface in command and query handlers is allowed
11. **Domain events are raised in domain**, handled in application layer
12. **Always use CancellationToken** in async methods
13. **Use record types** for DTOs and immutable data
14. **Use constants**, avoid magic numbers or strings

---

## Common Pitfalls

- **N+1 Queries**: Use `.Include()` or explicit joins
- **Memory Leaks**: Dispose IDisposable resources, use `using`
- **Deadlocks**: Don't mix sync and async, use ConfigureAwait(false) in libraries
- **Over-fetching**: Select only needed columns, use projections
- **Missing Indexes**: Check query plans, add indexes for common filters
- **Timeout Issues**: Configure appropriate timeouts for HTTP clients
- **Cache Stampede**: Use distributed locks for cache population

---

## Related Skills

- `domain-layer-setup` - Base abstractions for Domain layer
- `domain-entity` - Generate a domain entities, child entities, and domain events
- `application-layer-setup` - Base abstractions for Application layer
- `cqrs-command-generator` - Generate CQRS commands with handlers and validators
- `cqrs-query-generator` - Generate CQRS queries with handlers
- `ef-core-configuration` - Generate EF configurations