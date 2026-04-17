---
name: application-layer-setup
description: Application layer principles, CQRS patterns, orchestration rules, and production-ready abstractions for Clean Architecture in .NET applications.
language: C#
framework: .NET
pattern: Domain-Driven Design
---

# Application Layer Setup

## Overview

This skill provides abstractions for commands and queries, implementations for cross-cutting concerns, and guidelines for structuring the Application layer in a Clean Architecture application. It includes:

- **Base command and query** - Marker interfaces for commands and queries
- **Command and query handlers** - Generic interfaces for handling commands and queries
- **Pipeline behaviors** - Interfaces for cross-cutting concerns like validation, performance, logging, and transaction management
- **DbContext interface** - Abstraction for the database context to be used in handlers and behaviors
- **Rule builder validation** - Base class for implementing business rules validation in handlers
- **Authentication and authorization** - Interfaces for accessing the current user and checking permissions in handlers and behaviors

## Dependency Rules

- Application depends ONLY on Domain
- Application MUST NOT depend on Infrastructure
- Application defines contracts implemented by Infrastructure

## Application layer structure and conventions

```
Aurora.Flowboard.Application/
├── Abstractions/
│   ├── Authentication/   → IUserContext, ITokenProvider, TokenRequest, IdentityToken
│   ├── Behaviors/        → LoggingBehavior, PerformanceBehavior, ValidationBehavior (Scrutor decorators)
│   ├── Data/             → IApplicationDbContext, IApplicationDbContextFactory
│   ├── Exceptions/       → AuroraFlowboardException
│   ├── Messaging/        → ICommand, ICommandHandler, IQuery, IQueryHandler, IDomainEventHandler
│   ├── Time/             → IDateTimeProvider
│   └── Validations/      → ValidationError, RuleBuilderOptionsExtensions
├── DependencyInjection.cs
└── GlobalUsings.cs
```

### CQRS patterns

- Commands with no return value implement `ICommand` → handled by `ICommandHandler<TCommand>`
- Commands with a return value implement `ICommand<TResponse>` → handled by `ICommandHandler<TCommand, TResponse>`
- Queries implement `IQuery<TResponse>` → handled by `IQueryHandler<TQuery, TResponse>`
- All handlers return `Result` or `Result<TResponse>` — never throw for business errors
- Domain event handlers implement `IDomainEventHandler<TDomainEvent>`

### Folder naming

Use verb-only folder names inside a feature — not verb+entity:
- `{Feature}/Create/` not `{Feature}/Create{Feature}/`
- `{Feature}/AddMember/` not `{Feature}/Add{Feature}Member/`
- `{Feature}/GetById/` not `{Feature}/Get{Feature}ById/`

### Commands

- When a handler only needs to verify existence before calling a domain method, use `AnyAsync` instead of loading the full entity.

### Queries

- `GetById` returns a full response DTO including nested collections (e.g. `ProjectResponse` with `ProjectMemberResponse` list).
- `GetAll` returns a lightweight summary DTO — omit audit timestamps and nested collections; expose counts instead (e.g. `MemberCount`).
- Use projections in queries (avoid over-fetching).
- Never use `Include` with a `Select` projection — EF Core ignores includes when a projection is present; navigation properties accessed inside `Select` are translated to subqueries automatically.

### Behavior decorator chain

Behaviors are registered via Scrutor's `Decorate` in this order (outermost → innermost):

```
LoggingBehavior → PerformanceBehavior → ValidationBehavior → actual handler
```

Logging wraps validation so the log captures the full round-trip including validation failures.
Queries are **not** decorated with `ValidationBehavior` — only commands.

## Package References

```xml
<!-- {name}.Application.csproj -->
<ItemGroup>
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Relational" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
    <PackageReference Include="Scrutor" />
</ItemGroup>
```

## CQRS Abstractions

```csharp
// src/{name}.Application/Abstractions/Messaging/ICommand.cs
namespace {name}.Application.Abstractions.Messaging;

public interface ICommand;

#pragma warning disable S2326 // Unused type parameters should be removed
public interface ICommand<TResponse>;
#pragma warning restore S2326 // Unused type parameters should be removed

public interface IBaseCommand;
```

```csharp
// src/{name}.Application/Abstractions/Messaging/ICommandHandler.cs
namespace {name}.Application.Abstractions.Messaging;

public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    Task<Result> Handle(TCommand command, CancellationToken cancellationToken);
}

public interface ICommandHandler<in TCommand, TResponse> where TCommand : ICommand<TResponse>
{
    Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken);
}
```

```csharp
// src/{name}.Application/Abstractions/Messaging/IQuery.cs
namespace {name}.Application.Abstractions.Messaging;

#pragma warning disable S2326 // Unused type parameters should be removed
public interface IQuery<TResponse>;
#pragma warning restore S2326 // Unused type parameters should be removed
```

```csharp
// src/{name}.Application/Abstractions/Messaging/IQueryHandler.cs
namespace {name}.Application.Abstractions.Messaging;

public interface IQueryHandler<in TQuery, TResponse> where TQuery : IQuery<TResponse>
{
    Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken);
}
```

```csharp
// src/{name}.Application/Abstractions/Messaging/IDomainEventHandler.cs
namespace {name}.Application.Abstractions.Messaging;

public interface IDomainEventHandler<in TDomainEvent> where TDomainEvent : IDomainEvent
{
    Task Handle(TDomainEvent domainEvent, CancellationToken cancellationToken);
}
```

## Application common exceptions

```csharp
// src/{name}.Application/Abstractions/Exceptions/{name}Exception.cs
namespace {name}.Application.Abstractions.Exceptions;

public sealed class {name}Exception : Exception
{
    public {name}Exception(
        string requestName,
        BaseError? error = default,
        Exception? innerException = default) : base($"Error processing request {requestName}.", innerException)
    {
        RequestName = requestName;
        Error = error;
    }

    public string RequestName { get; }

    public BaseError? Error { get; }
}
```

## Validation abstractions

```csharp
// src/{name}.Application/Abstractions/Validations/RuleBuilderOptionsExtensions.cs
namespace {name}.Application.Abstractions.Validations;

public static class RuleBuilderOptionsExtensions
{
    public static IRuleBuilderOptions<T, TProperty> WithBaseError<T, TProperty>(
        this IRuleBuilderOptions<T, TProperty> rule,
        BaseError baseError) =>
        rule.WithErrorCode(baseError.Code).WithMessage(baseError.Message);
}
```

```csharp
// src/{name}.Application/Abstractions/Validations/ValidationError.cs
namespace {name}.Application.Abstractions.Validations;

public sealed record ValidationError : BaseError
{
    public BaseError[] Errors { get; }

    public ValidationError(BaseError[] errors)
        : base("Validation", "One or more validation errors occurred", BaseErrorType.Validation)
    {
        Errors = errors;
    }

    public static ValidationError FromResults(IEnumerable<Result> results) =>
        new([.. results.Where(r => !r.IsSuccessful).Select(r => r.Error)]);
}
```

## DbContext abstraction

```csharp
// src/{name}.Application/Abstractions/Data/I{name}DbContext.cs
namespace {name}.Application.Abstractions.Data;

public interface I{name}DbContext : IAsyncDisposable
{
    DbSet<{DomainEntity1}> {DomainEntity1}s { get; }
    DbSet<{DomainEntity2}> {DomainEntity2}s { get; }
    DbSet<{DomainEntity3}> {DomainEntity3}s { get; }
    DbSet<{DomainEntity4}> {DomainEntity4}s { get; }
    DbSet<{DomainEntity5}> {DomainEntity5}s { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

```csharp
// src/{name}.Application/Abstractions/Data/I{name}DbContextFactory.cs
namespace {name}.Application.Abstractions.Data;

public interface I{name}DbContextFactory
{
    Task<I{name}DbContext> CreateDbContextAsync(CancellationToken cancellationToken = default);
}
```

## Authentication and authorization abstractions

```csharp
// src/{name}.Application/Abstractions/Authentication/IUserContext.cs
namespace {name}.Application.Abstractions.Authentication;

public interface IUserContext
{
    Guid UserId { get; }
}
```

```csharp
// src/{name}.Application/Abstractions/Authentication/TokenRequest.cs
namespace {name}.Application.Abstractions.Authentication;

public sealed record TokenRequest(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    IEnumerable<string> Roles);
```

```csharp
// src/{name}.Application/Abstractions/Authentication/IdentityToken.cs
namespace {name}.Application.Abstractions.Authentication;

public sealed record IdentityToken(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresOn,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresOn);
```

```csharp
// src/{name}.Application/Abstractions/Authentication/ITokenProvider.cs
namespace {name}.Application.Abstractions.Authentication;

public interface ITokenProvider
{
    IdentityToken CreateToken(TokenRequest tokenRequest);
}
```

## Pipeline behaviors

```csharp
// src/{name}.Application/Abstractions/Behaviors/LoggingBehavior.cs
using Microsoft.Extensions.Logging;

namespace {name}.Application.Abstractions.Behaviors;

internal static class LoggingBehavior
{
    internal sealed class CommandHandler<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> innerHandler,
        ILogger<CommandHandler<TCommand, TResponse>> logger) : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        public async Task<Result<TResponse>> Handle(
            TCommand command,
            CancellationToken cancellationToken)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Processing request: {Name} {@Request}", typeof(TCommand).Name, command);
            }

            Result<TResponse> result = await innerHandler.Handle(command, cancellationToken);

            if (result.IsSuccessful)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Request processed successfully: {Name} {@Response}", typeof(TCommand).Name, result);
                }
            }
            else
            {
                logger.LogError("Request processed with errors: {Name} {@Response}", typeof(TCommand).Name, result);
            }

            return result;
        }
    }

    internal sealed class CommandBaseHandler<TCommand>(
        ICommandHandler<TCommand> innerHandler,
        ILogger<CommandBaseHandler<TCommand>> logger) : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        public async Task<Result> Handle(
            TCommand command,
            CancellationToken cancellationToken)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Processing request: {Name} {@Request}", typeof(TCommand).Name, command);
            }

            Result result = await innerHandler.Handle(command, cancellationToken);

            if (result.IsSuccessful)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Request processed successfully: {Name} {@Response}", typeof(TCommand).Name, result);
                }
            }
            else
            {
                logger.LogError("Request processed with errors: {Name} {@Response}", typeof(TCommand).Name, result);
            }

            return result;
        }
    }

    internal sealed class QueryHandler<TQuery, TResponse>(
        IQueryHandler<TQuery, TResponse> innerHandler,
        ILogger<QueryHandler<TQuery, TResponse>> logger) : IQueryHandler<TQuery, TResponse>
        where TQuery : IQuery<TResponse>
    {
        public async Task<Result<TResponse>> Handle(
            TQuery query,
            CancellationToken cancellationToken)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Processing request: {Name} {@Request}", typeof(TQuery).Name, query);
            }

            Result<TResponse> result = await innerHandler.Handle(query, cancellationToken);

            if (result.IsSuccessful)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Request processed successfully: {Name} {@Response}", typeof(TResponse).Name, result);
                }
            }
            else
            {
                logger.LogError("Request processed with errors: {Name} {@Response}", typeof(TResponse).Name, result);
            }

            return result;
        }
    }
}
```

```csharp
// src/{name}.Application/Abstractions/Behaviors/PerformanceBehavior.cs
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace {name}.Application.Abstractions.Behaviors;

internal static class PerformanceBehavior
{
    private const int MaximumAllowedMilliseconds = 500;
    private const string LongRunningMessage = "Long-running request: {Name} ({ElapsedMilliseconds} milliseconds) {@Request}";

    internal sealed class CommandHandler<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> innerHandler,
        ILogger<CommandHandler<TCommand, TResponse>> logger) : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        private readonly Stopwatch stopWatch = new();

        public async Task<Result<TResponse>> Handle(
            TCommand command,
            CancellationToken cancellationToken)
        {
            stopWatch.Start();

            Result<TResponse> result = await innerHandler.Handle(command, cancellationToken);

            stopWatch.Stop();

            if (stopWatch.ElapsedMilliseconds > MaximumAllowedMilliseconds)
            {
                logger.LogWarning(LongRunningMessage, typeof(TCommand).Name, stopWatch.ElapsedMilliseconds, command);
            }

            return result;
        }
    }

    internal sealed class CommandBaseHandler<TCommand>(
        ICommandHandler<TCommand> innerHandler,
        ILogger<CommandBaseHandler<TCommand>> logger) : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        private readonly Stopwatch stopWatch = new();

        public async Task<Result> Handle(
            TCommand command,
            CancellationToken cancellationToken)
        {
            stopWatch.Start();

            Result result = await innerHandler.Handle(command, cancellationToken);

            stopWatch.Stop();

            if (stopWatch.ElapsedMilliseconds > MaximumAllowedMilliseconds)
            {
                logger.LogWarning(LongRunningMessage, typeof(TCommand).Name, stopWatch.ElapsedMilliseconds, command);
            }

            return result;
        }
    }

    internal sealed class QueryHandler<TQuery, TResponse>(
        IQueryHandler<TQuery, TResponse> innerHandler,
        ILogger<QueryHandler<TQuery, TResponse>> logger) : IQueryHandler<TQuery, TResponse>
        where TQuery : IQuery<TResponse>
    {
        private readonly Stopwatch stopWatch = new();

        public async Task<Result<TResponse>> Handle(
            TQuery query,
            CancellationToken cancellationToken)
        {
            stopWatch.Start();

            Result<TResponse> result = await innerHandler.Handle(query, cancellationToken);

            stopWatch.Stop();

            if (stopWatch.ElapsedMilliseconds > MaximumAllowedMilliseconds)
            {
                logger.LogWarning(LongRunningMessage, typeof(TQuery).Name, stopWatch.ElapsedMilliseconds, query);
            }

            return result;
        }
    }
}
```

```csharp
// src/{name}.Application/Abstractions/Behaviors/ValidationBehavior.cs
using {name}.Application.Abstractions.Validations;
using FluentValidation.Results;

namespace {name}.Application.Abstractions.Behaviors;

internal static class ValidationBehavior
{
    internal sealed class CommandHandler<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> innerHandler,
        IEnumerable<IValidator<TCommand>> validators) : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        public async Task<Result<TResponse>> Handle(
            TCommand command,
            CancellationToken cancellationToken)
        {
            ValidationFailure[] failures = await ValidateAsync(command, validators);

            if (failures.Length == 0)
            {
                return await innerHandler.Handle(command, cancellationToken);
            }

            return Result.Fail<TResponse>(CreateValidationError(failures));
        }
    }

    internal sealed class CommandBaseHandler<TCommand>(
        ICommandHandler<TCommand> innerHandler,
        IEnumerable<IValidator<TCommand>> validators) : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
        {
            ValidationFailure[] failures = await ValidateAsync(command, validators);

            if (failures.Length == 0)
            {
                return await innerHandler.Handle(command, cancellationToken);
            }

            return Result.Fail(CreateValidationError(failures));
        }
    }

    private static async Task<ValidationFailure[]> ValidateAsync<TCommand>(
        TCommand command,
        IEnumerable<IValidator<TCommand>> validators)
    {
        IValidator<TCommand>[] validatorArray = [.. validators];

        if (validatorArray.Length == 0)
        {
            return [];
        }

        var context = new ValidationContext<TCommand>(command);

        ValidationResult[] results = await Task.WhenAll(
            validatorArray.Select(v => v.ValidateAsync(context)));

        ValidationFailure[] failures = [.. results
            .Where(x => !x.IsValid)
            .SelectMany(x => x.Errors)
            .Distinct()];

        return failures;
    }

    private static ValidationError CreateValidationError(ValidationFailure[] validationFailures) =>
        new([.. validationFailures.Select(f => BaseError.Validation(f.ErrorCode, f.ErrorMessage))]);
}
```

## Dependency Injection

```csharp
// src/{name}.Application/DependencyInjection.cs
using {name}.Application.Abstractions.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace {name}.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services) => services
        .AddMessagingHandlers()
        .AddBehaviors()
        .AddDomainHandlers()
        .AddDomainServices()
        .AddValidatorsFromAssembly();

    private static IServiceCollection AddMessagingHandlers(this IServiceCollection services)
    {
        services
            .Scan(scan => scan.FromAssembliesOf(typeof(DependencyInjection))
            .AddClasses(classes => classes.AssignableTo(typeof(IQueryHandler<,>)), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services
            .Scan(scan => scan.FromAssembliesOf(typeof(DependencyInjection))
            .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<>)), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services
            .Scan(scan => scan.FromAssembliesOf(typeof(DependencyInjection))
            .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<,>)), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        return services;
    }

    private static IServiceCollection AddBehaviors(this IServiceCollection services)
    {
        services.Decorate(typeof(ICommandHandler<,>), typeof(ValidationBehavior.CommandHandler<,>));
        services.Decorate(typeof(ICommandHandler<>), typeof(ValidationBehavior.CommandBaseHandler<>));

        services.Decorate(typeof(IQueryHandler<,>), typeof(PerformanceBehavior.QueryHandler<,>));
        services.Decorate(typeof(ICommandHandler<,>), typeof(PerformanceBehavior.CommandHandler<,>));
        services.Decorate(typeof(ICommandHandler<>), typeof(PerformanceBehavior.CommandBaseHandler<>));

        services.Decorate(typeof(IQueryHandler<,>), typeof(LoggingBehavior.QueryHandler<,>));
        services.Decorate(typeof(ICommandHandler<,>), typeof(LoggingBehavior.CommandHandler<,>));
        services.Decorate(typeof(ICommandHandler<>), typeof(LoggingBehavior.CommandBaseHandler<>));

        return services;
    }

    private static IServiceCollection AddDomainHandlers(this IServiceCollection services)
    {
        services
            .Scan(scan => scan.FromAssembliesOf(typeof(DependencyInjection))
            .AddClasses(classes => classes.AssignableTo(typeof(IDomainEventHandler<>)), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        return services;
    }

    private static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        return services;
    }

    private static IServiceCollection AddValidatorsFromAssembly(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        return services;
    }
}
```

## Anti-Patterns to Avoid

❌ Business logic in handlers  
❌ Returning domain entities  
❌ Using Include with Select  
❌ Ignoring cancellation tokens  
❌ Throwing exceptions for business rules  
❌ Blocking async code (.Result, .Wait())  
❌ Overloading handlers with logic that belongs in the domain

```csharp
// ❌ WRONG: Behavior that modifies request
public async Task<TResponse> Handle(...)
{
    request.ModifiedAt = DateTime.UtcNow;  // Don't modify!
    return await next();
}

// ✅ CORRECT: Behaviors observe, don't modify
public async Task<TResponse> Handle(...)
{
    _logger.LogInformation("Processing at {Time}", DateTime.UtcNow);
    return await next();
}

// ❌ WRONG: Swallowing exceptions silently
try { return await next(); }
catch { return default!; }  // Silent failure!

// ✅ CORRECT: Log and convert or rethrow
try { return await next(); }
catch (Exception ex)
{
    _logger.LogError(ex, "Error in handler");
    return CreateFailureResult(ex);
}

// ❌ WRONG: Blocking async code
var result = next().Result;  // Deadlock risk!

// ✅ CORRECT: Await properly
var result = await next();

// ❌ WRONG: Caching commands
public sealed class CachingBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>  // Commands shouldn't be cached!

// ✅ CORRECT: Cache only queries
public sealed class CachingBehavior<TRequest, TResponse>
    where TRequest : ICachedQuery<TResponse>
```

## Related Skills

- `dotnet-clean-architecture` - Master C#/.NET solution following Clean Architecture principles
- `domain-entity` - Generate a domain entities, child entities, and domain events
- `cqrs-command-generator` - Generate CQRS commands with handlers and validators
- `cqrs-query-generator` - Generate CQRS queries with handlers