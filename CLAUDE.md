# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Aurora Flowboard is a .NET 10 internal REST API for software project management. It follows **Clean Architecture + DDD** with a modular monolith approach. The stack is .NET 10, Entity Framework Core, and PostgreSQL with JWT authentication and RBAC.

## Tech stack
- .NET 10 / C#
- ASP.NET Core (Minimal APIs)
- .NET Aspire for local orchestration (Postgres container) and service defaults
- Entity Framework Core + Npgsql (PostgreSQL) + EFCore.NamingConventions (snake_case)
- FluentValidation
- Scrutor for DI assembly scanning
- OpenTelemetry (tracing, metrics, logging) — no Serilog
- Swashbuckle (Swagger/OpenAPI)
- JWT bearer authentication (custom `ITokenProvider`) + RBAC (`Administrator`, `Member`)
- xUnit v3 + NetArchTest + Shouldly (architecture tests); xUnit v3 + NSubstitute + FluentAssertions (unit tests)

## Architecture

```
Aurora.Flowboard.AppHost         → .NET Aspire orchestration (Postgres + Api resource wiring)
Aurora.Flowboard.ServiceDefaults → Shared Aspire defaults (OpenTelemetry, health checks, resilience)
Aurora.Flowboard.Api             → Minimal API endpoints, middleware, DI composition root
Aurora.Flowboard.Application     → CQRS handlers, validators, behavior pipeline
Aurora.Flowboard.Domain          → Entities, value objects, domain events, Result type
Aurora.Flowboard.Infrastructure  → EF Core, PostgreSQL, migrations, auth (JWT, password hashing), time
```

Project names use the dot-separated `Aurora.Flowboard.*` convention — never a space (it breaks `dotnet publish`; see `.claude/rules/build-and-packaging.md`).

Tests live under `test/`: `Aurora.Flowboard.Domain.UnitTests`, `Aurora.Flowboard.Application.UnitTests`, and `Aurora.Flowboard.ArchitectureTests` (enforces the conventions in this file and in `.claude/rules/`: layer dependencies, naming, sealing, visibility, slice layout).

## Domain aggregates

| Aggregate    | Key entities                                                              |
|--------------|---------------------------------------------------------------------------|
| Projects     | `Project` (has `Color`, `ProjectKind`, `ProjectStatus`), `ProjectMember` (has `ProjectRole`), `ProjectChangeLog`, `ProjectCode` (VO, exposed as `Prefix`), `FlowState` (has `Color`), `FlowTransition`, `FlowStateCategory` |
| Milestones   | `Milestone` (own aggregate root, FK `project_id`, has `MilestoneStatus`) |
| Components   | `Component` (own aggregate root, FK `project_id`, has `ComponentStatus`) |
| TemplateFlows | `TemplateFlow` (own aggregate root, keyed by `ProjectKind` — one per kind, not FK'd to a project), `TemplateFlowState` (has `FlowStateCategory`, `Color`) |
| WorkItems    | `WorkItem` (optional FK `milestone_id`, `component_id`), `Comment`, `TimeEntry`, `WorkItemTag`, `StateTransitionHistory`, `WorkItemChangeLog` |
| Users        | `User`, `UserToken` (issued access/refresh token pair), `Password` (VO), `Role` (closed value type: `Administrator`/`Member`, not a DB entity) |
| Shared       | `Email` (VO), `Color` (VO)                                                |

`ProjectKind`: `Product`, `Client`, `Research`, `Internal`.

`ProjectStatus`: `Active`, `Maintenance`, `Completed`, `Archived`.

`ProjectRole` (project membership role, distinct from `Role`): `Admin`, `Analyst`, `Developer`, `QA`, `Viewer`.

## Key patterns

- **CQRS** — every operation is an `ICommand`/`IQuery` + handler returning `Result` or `Result<T>`. Validators are auto-wired via `ValidationBehavior`. Pipeline: `LoggingBehavior → PerformanceBehavior → ValidationBehavior → Handler`.
- **Result type** — railway-oriented `Result`/`Result<TValue>` with `BaseError` categories `Failure`, `Validation`, `NotFound`, `Conflict`, `Forbidden`. Endpoints map them with `ResultExtensions.Match(...)`.
- **Minimal APIs** — endpoints implement `IBaseEndpoint`, auto-registered via Scrutor, grouped under `/api/v1/flowboard`.
- **Data access** — no repository pattern: handlers query `IApplicationDbContext` directly with LINQ. One `IEntityTypeConfiguration<T>` per entity in `Infrastructure/Configurations/`, schema `flowboard`, snake_case, private field navigations mapped explicitly. Migrations auto-apply on startup.
- **Domain entities** — private setters, static factory methods, domain events via `BaseEntity`. Enums live in their owning aggregate folder. Value objects implement the `IValueObject` marker and are mapped with `OwnsOne`.
- **Auth** — `POST auth/login` issues a JWT + opaque refresh token (`JwtTokenProvider`); PBKDF2 password hashing (`PasswordHasher`). Endpoints call `RequireAuthorization()`, or `RequireAuthorization(policy => policy.RequireRole(Role.Administrator.Name))` for admin-only ones. `IUserContext` exposes the current user to handlers. A default Administrator is seeded at startup.
- **Flow** — there is no `Flow` aggregate: `FlowState`/`FlowTransition` are children of `Project`, set at creation and not editable over HTTP.

## Area-specific rules

Detailed conventions live in `.claude/rules/` and load automatically when you read files matching their `paths:`. When creating something in an area without reading its existing files first, read the relevant rule.

| Rule | Covers |
|---|---|
| `ef-core-queries.md` | Query shape (no sibling collections, correlated subqueries, expression-tree limits), tracking with `ValueGeneratedNever` keys, raw SQL, pagination |
| `work-items.md` | Board endpoint, detail vs. paginated activity endpoints, change log semantics, `Viewer` permissions |
| `project-flow.md` | One flow per project, removed flow endpoints (don't reintroduce), TemplateFlows |
| `milestones-components.md` | Aggregate ownership, admin-only changes, milestone state machine |
| `domain-model.md` | Enum placement, value object conventions |
| `startup-and-seeding.md` | Migrations on startup, Administrator and template seeding, `Bootstrap` config |
| `unit-tests.md` | Mock `DbSet` setup, pagination test across page boundaries |
| `architecture-tests.md` | NetArchTest pitfalls, Mono.Cecil pin |
| `build-and-packaging.md` | No spaces in project names, central package management |

When a change introduces a new area-specific decision, add it to the matching rule (or a new one), not here. This file only changes when the architecture, stack or workflow changes.

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
- After modifying domain/application logic or tests, run **all three** test projects and report pass/fail counts.
- After adding or renaming a type in any layer, also run `ArchitectureTests` — it is the fastest way to catch a convention violation (unsealed handler, wrong namespace, public validator, missing validator).
- Do not consider a task complete until build and tests pass.

**Never use `dotnet test`.** All three test projects are xunit.v3 (Microsoft.Testing.Platform) and fail under `dotnet test` with *"Testing with VSTest target is no longer supported..."*. Build, then run the produced `.exe`; filter with `-class "Namespace.ClassName"` or `-method "*MethodName"`. `dotnet run` / `dotnet exec` on a test project exit 0 *without running any test*, so always confirm the runner printed a test count.

## Commands

```bash
dotnet build "Aurora Flowboard.slnx"   # whole solution, including test projects

# Tests (run the built executables, NOT dotnet test)
./test/Aurora.Flowboard.Domain.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Domain.UnitTests.exe
./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests.exe
./test/Aurora.Flowboard.ArchitectureTests/bin/Debug/net10.0/Aurora.Flowboard.ArchitectureTests.exe
./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests.exe -class "Aurora.Flowboard.Application.UnitTests.WorkItems.GetWorkItemByCodeHandlerTests"

dotnet ef migrations add <Name> --project src/Aurora.Flowboard.Infrastructure --startup-project src/Aurora.Flowboard.Api
dotnet ef database update --project src/Aurora.Flowboard.Infrastructure --startup-project src/Aurora.Flowboard.Api

# Run locally via Aspire (provisions Postgres, wires connection string, sets up dashboard)
dotnet run --project "src/Aurora.Flowboard.AppHost"

# Build and run the API image directly (requires an external Postgres via ConnectionStrings__Database)
docker build -f src/Aurora.Flowboard.Api/Dockerfile -t aurora-flowboard-api .
docker run -p 8080:8080 aurora-flowboard-api
```

## Code style

`Directory.Build.props` treats warnings as errors and enables SonarAnalyzer. `Directory.Packages.props` manages all NuGet versions centrally. `.editorconfig` enforces:

- File-scoped namespaces
- No `var` for built-in types; `var` allowed when type is apparent
- No `this.` qualification
- Namespace must match folder structure
- Expression-bodied members for properties/lambdas where applicable
- Null propagation (`?.`) over explicit null checks
- No magic numbers or strings — use constants
