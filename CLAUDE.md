# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Aurora Flowboard is a .NET 10 internal REST API for software project management. It follows **Clean Architecture + DDD** with a modular monolith approach. The stack is .NET 10, Entity Framework Core, and PostgreSQL with JWT authentication and RBAC.

## Tech stack
- .NET 10 / C#
- ASP.NET Core (Minimal APIs)
- Entity Framework Core + Npgsql (PostgreSQL) + EFCore.NamingConventions (snake_case)
- FluentValidation
- Redis for caching
- Scrutor for DI assembly scanning
- Serilog for logging
- Swashbuckle (Swagger/OpenAPI)

## Architecture

```
Aurora.Flowboard.Api            → Minimal API endpoints, middleware, DI composition root
Aurora.Flowboard.Application    → CQRS handlers, validators, behavior pipeline
Aurora.Flowboard.Domain         → Entities, value objects, domain events, Result type
Aurora.Flowboard.Infrastructure → EF Core, PostgreSQL, repositories, auth, time
```

Tests live under `test/`:
- `Aurora.Flowboard.Domain.UnitTests`
- `Aurora.Flowboard.Application.UnitTests`

## Domain aggregates

| Aggregate    | Key entities                                                              |
|--------------|---------------------------------------------------------------------------|
| Projects     | `Project`, `ProjectMember`, `ProjectChangeLog`, `ProjectCode` (VO)       |
| Flows        | `Flow`, `FlowState`, `FlowTransition`                                     |
| WorkItems    | `WorkItem`, `Comment`, `TimeEntry`, `WorkItemTag`, `StateTransitionHistory`, `WorkItemChangeLog` |
| Users        | `User`                                                                    |
| Shared       | `Email` (VO)                                                              |

## Key patterns

**CQRS** — every operation is a `ICommand`/`IQuery` + handler. Handlers return `Result` or `Result<T>`. Validators are auto-wired via `ValidationBehavior`. Behavior pipeline: `LoggingBehavior → PerformanceBehavior → ValidationBehavior → Handler`.

**Result type** — railway-oriented `Result`/`Result<TValue>` with `BaseError` categories: `Failure`, `Validation`, `NotFound`, `Conflict`, `Forbidden`. Endpoints use `ResultExtensions.Match(...)` to map to HTTP responses.

**Minimal APIs** — endpoints implement `IBaseEndpoint`, auto-registered via Scrutor. All routes grouped under `/api/v1/flowboard`.

**EF Core** — one `IEntityTypeConfiguration<T>` per entity in `Infrastructure/Configurations/`. Schema `flowboard`, snake_case naming. Private field navigation (`_members`, `_changeLogs`, etc.) mapped explicitly.

**Domain entities** — private setters, static factory methods, domain events via `BaseEntity`. Enum types belong in their owning aggregate folder.

## Workflow
1. Ask clarifying questions if requirements are unclear.
2. Propose a plan and list files to change.
3. Implement the smallest viable change.
4. Add or update tests when appropriate.
5. Provide commands to verify changes.

## Hard rules
- Do not introduce new architectural layers.
- Do not add frameworks we do not already use.
- Always pass `CancellationToken` through async calls.
- No sync over async.
- No `Task.Run` in request handlers.
- Outbound HTTP calls must have timeouts and cancellation.
- Caching must consider time budgets and stampede protection.

## Build & Test Verification
- After every code change, run `dotnet build` to verify a clean build.
- After modifying domain/application logic or tests, run `dotnet test` and report pass/fail counts.
- Do not consider a task complete until build and tests pass

## Commands

```bash
dotnet build
dotnet test
dotnet test --filter "FullyQualifiedName~TestClassName"
dotnet ef migrations add <Name> --project src/Aurora.Flowboard.Infrastructure
dotnet ef database update --project src/Aurora.Flowboard.Infrastructure
```

Solution file: `Aurora Flowboard.slnx`

## Code style

`Directory.Build.props` treats warnings as errors and enables SonarAnalyzer. `.editorconfig` enforces:

- File-scoped namespaces
- No `var` for built-in types; `var` allowed when type is apparent
- No `this.` qualification
- Namespace must match folder structure
- Expression-bodied members for properties/lambdas where applicable
- Null propagation (`?.`) over explicit null checks
- No magic numbers or strings — use constants

`Directory.Packages.props` manages all NuGet versions centrally — never add version attributes to individual `.csproj` files.

## Testing conventions

- **Domain tests**: assert entity behavior and domain events. Use `BaseTest` helpers. Pattern: `*Data.cs` builders + `*Tests.cs` xUnit facts.
- **Application tests**: test CQRS handler logic with NSubstitute mocks and `MockDbSetHelper`. Use FluentAssertions.
- Stack: xUnit v3 + NSubstitute + FluentAssertions.
