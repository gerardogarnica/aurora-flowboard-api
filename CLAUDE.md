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

> Note: the README currently also lists Redis and Serilog — neither is present in the code or `Directory.Packages.props`. Treat this file as the source of truth over the README until it's reconciled.

## Architecture

```
Aurora.Flowboard.AppHost         → .NET Aspire orchestration (Postgres + Api resource wiring)
Aurora.Flowboard.ServiceDefaults → Shared Aspire defaults (OpenTelemetry, health checks, resilience)
Aurora.Flowboard.Api             → Minimal API endpoints, middleware, DI composition root
Aurora.Flowboard.Application     → CQRS handlers, validators, behavior pipeline
Aurora.Flowboard.Domain          → Entities, value objects, domain events, Result type
Aurora.Flowboard.Infrastructure  → EF Core, PostgreSQL, migrations, auth (JWT, password hashing), time
```

All project folder/file names use the dot-separated `Aurora.Flowboard.*` convention. Do not reintroduce a space in a project name: a space in a `ProjectReference`'s target breaks the .NET SDK's publish-time copy-local resolution for that project's *transitive* `PackageReference`s — `dotnet build` copies them fine, but `dotnet publish` silently drops them, which only surfaces as a `FileNotFoundException` at runtime in the published/container image.

Tests live under `test/`:
- `Aurora.Flowboard.Domain.UnitTests`
- `Aurora.Flowboard.Application.UnitTests`

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

**CQRS** — every operation is a `ICommand`/`IQuery` + handler. Handlers return `Result` or `Result<T>`. Validators are auto-wired via `ValidationBehavior`. Behavior pipeline: `LoggingBehavior → PerformanceBehavior → ValidationBehavior → Handler`.

**Result type** — railway-oriented `Result`/`Result<TValue>` with `BaseError` categories: `Failure`, `Validation`, `NotFound`, `Conflict`, `Forbidden`. Endpoints use `ResultExtensions.Match(...)` to map to HTTP responses.

**Minimal APIs** — endpoints implement `IBaseEndpoint`, auto-registered via Scrutor. All routes grouped under `/api/v1/flowboard`.

**EF Core** — one `IEntityTypeConfiguration<T>` per entity in `Infrastructure/Configurations/`. Schema `flowboard`, snake_case naming. Private field navigation (`_members`, `_changeLogs`, etc.) mapped explicitly. Migrations auto-apply on startup in every environment, including Production, controlled by the `Database:ApplyMigrationsOnStartup` config flag (default `true`; `Extensions/MigrationServiceExtensions.cs`). Set `Database__ApplyMigrationsOnStartup=false` to disable and apply migrations manually instead.

**EF Core query shape** — there is no repository pattern; handlers query `IApplicationDbContext` directly with LINQ, and that makes the shape of the query the handler's responsibility:

- **Never project more than one collection per query.** Sibling collections become same-level `LEFT JOIN`s and the row count is their *product*, not their sum. A work item with 3 tags × 15 comments × 8 time entries × 6 transitions × 40 change logs returned 86,400 rows — each carrying the full duplicated scalar payload. Split the collections into separate queries (or separate endpoints, as the activity collections are). `AsSplitQuery()` mitigates it when several collections genuinely must load together, but it is not a substitute for splitting an unbounded collection out; with a single collection it only buys an extra round trip, so don't add it reflexively.
- **Display names are resolved with correlated subqueries** against `dbContext.Users` / `FlowStates` / `Components` / `Milestones`, e.g. `dbContext.Users.Where(u => u.Id == x.UserId).Select(u => u.FirstName + " " + u.LastName).FirstOrDefault() ?? string.Empty`. `Comment`, `StateTransitionHistory` and `WorkItemChangeLog` deliberately hold raw `Guid` FKs with no navigation properties, so there is nothing to join through. Keep these subqueries inside a page-limited query — they are evaluated per returned row.
- **Expression trees cannot contain `switch` expressions (CS8514).** Inside an `IQueryable` projection the lambda is an `Expression<Func<>>`, so multi-branch logic has to be a ternary chain. `GetWorkItemChangeLogsHandler`'s `AffectedEntityName` looks verbose for exactly this reason and carries a comment saying so — don't "simplify" it into a `switch`, it won't compile.

**Domain entities** — private setters, static factory methods, domain events via `BaseEntity`. Enum types belong in their owning aggregate folder.

**Default Administrator seeding** — on every startup, right after migrations apply, `SeedAdministratorAsync` (`Api/Extensions/SeedingServiceExtensions.cs`) creates a default `Role.Administrator` user if none exists yet in `flowboard.user_roles` (idempotent no-op otherwise). Credentials come from the `Bootstrap` config section (`BootstrapOptions`, `Infrastructure/Bootstrap/`): `AdminEmail`/`AdminPassword` are required and must be set per environment (`Bootstrap__AdminEmail`/`Bootstrap__AdminPassword` env vars in staging/prod, e.g. via Dokploy secrets — never commit real values); `AdminFirstName`/`AdminLastName` default to `"System"`/`"Administrator"`. This solves the bootstrap chicken-and-egg problem: `POST users` requires an existing Administrator, so the very first one must be created outside that endpoint. There is no forced-password-change mechanism — rotating the seeded password after first login is an operational convention, not enforced by the domain.

**Authentication & authorization** — `POST auth/login` (anonymous) issues a JWT access token + opaque refresh token via `ITokenProvider`/`JwtTokenProvider`; passwords are hashed with PBKDF2 (`PasswordHasher`, not BCrypt/ASP.NET Identity). Protected endpoints use `RequireAuthorization()`; admin-only endpoints use `RequireAuthorization(policy => policy.RequireRole(Role.Administrator.Name))` (e.g. `POST users`). `IUserContext` exposes the current user's id/claims to handlers.

**One flow per project** — there is no `Flow` aggregate. `FlowState` and `FlowTransition` are child entities of `Project` (FK `project_id`), and a work item reaches its transitions via `Project.FindFlowTransition(...)`. Flow operations are exposed under `projects/{id}/flow/...`.

**Work item board response** — `GET projects/{projectId:guid}/work-items` returns work items grouped by the project's `FlowState`s (Kanban board shape), not a flat list. A project with no flow states returns an empty board, not a 404.

**Work item detail vs. activity collections** — `GET work-items/{code}` returns only bounded data: the scalars, `tags`, and `availableTransitions`. The four unbounded activity collections live in their own paginated sub-endpoints, keyed by the work item's **`{id:guid}`** (not its code, matching the existing sub-resources like `POST work-items/{id:guid}/comments`):

```
GET work-items/{id:guid}/comments
GET work-items/{id:guid}/change-logs
GET work-items/{id:guid}/state-history
GET work-items/{id:guid}/time-entries
```

Do not move these back into the detail payload. They were split out because projecting five sibling collections in one EF query produced a cartesian product (see *EF Core query shape* below), and `change_logs` in particular grows monotonically — every field update, move, assignment, comment and tag operation writes a row, and nothing prunes them.

**Pagination** — `PagedResponse<T>` (`Application/Abstractions/Pagination/`) is the shared envelope: `Items`, `Page`, `PageSize`, `TotalCount`, and a computed `TotalPages`. Offset-based, `Page` is 1-based. `PaginationDefaults` holds `DefaultPage = 1`, `DefaultPageSize = 20`, `MaxPageSize = 100`; endpoints take `page`/`pageSize` as optional query params defaulted from those constants. Validators apply `MustBeValidPage()` / `MustBeValidPageSize()` (`Abstractions/Validations/PaginationRuleExtensions.cs`) — exceeding `MaxPageSize` is a 400, not a silent clamp. Activity collections are ordered newest-first, always with the entity `Id` as tie-breaker so paging stays stable. A page past the end returns 200 with an empty `Items` and the real `TotalCount`, never a 404.

**Work item change log semantics** — `WorkItemChangeLog.AffectedEntityId` points at a different table depending on `ChangeType`: a `User` for `Assigned`, a `FlowState` for `Moved`, a `Component` for `ComponentChanged`, a `Milestone` for `MilestoneChanged`; it is null for the rest. `GetWorkItemChangeLogsHandler` resolves it into `AffectedEntityName` accordingly. `WorkItem.Create` writes `MilestoneChanged`/`ComponentChanged` entries when created with a milestone or component, mirroring `ChangeMilestone`/`ChangeComponent`.

**Milestones & Components** — both are project-owned aggregate roots (own `Domain/Milestones` and `Domain/Components` folders, FK `project_id`), not child collections mapped through `Project` the way `FlowState`/`FlowTransition` are. Only a project admin (`Project.IsAdmin`) can create/update/change status. `Milestone` has a status state machine (`Draft → Active/Archived`, `Active → OnHold/Completed/Archived`, `OnHold → Active/Archived`) enforced in `Milestone.ChangeStatus`; closing (`Completed`/`Archived`) or retiring a `Component` is blocked while it has open work items. `WorkItem` optionally references a `Milestone` and/or `Component` via nullable `milestone_id`/`component_id`. Endpoints are under `projects/{id}/milestones/...` and `projects/{id}/components/...`.

**TemplateFlows** — `TemplateFlow` (`Domain/TemplateFlows/`) is a global aggregate root keyed by `ProjectKind`, not owned by any single `Project` (no `project_id` FK). It holds suggested `TemplateFlowState` entries (`Name`, `FlowStateCategory`, `Color`, `SortOrder`) that the front-end will use to pre-fill a new project's initial flow states — `CreateProjectCommand`/`CreateProjectHandler` already accept an explicit `FlowStates` list from the caller, so templates plug in purely on the front-end without any change to project creation. One template per `ProjectKind`; uniqueness is enforced at the Application layer (query + DB unique index), not in `TemplateFlow.Create`. Unlike `Milestone`/`Component`, authorization is **not** checked in the domain — it's enforced at the (future) endpoint level via `RequireAuthorization(Role.Administrator)`, the same pattern as `POST users`; `TemplateFlow.CreatedBy` is just an audit `Guid`. `TemplateFlowState.Category` is immutable after creation — `TemplateFlow.UpdateState` only takes `(stateId, name, color)`; to change a state's category, remove it and add it again. EF configuration, `DbSet`s, and an initial migration already exist, and `SeedTemplateFlowsAsync` (`Api/Extensions/SeedingServiceExtensions.cs`) seeds the default templates at startup; Application-layer commands/queries and public endpoints don't exist yet.

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
- After modifying domain/application logic or tests, run **both** test projects (see the runner note below — they need different commands) and report pass/fail counts.
- Do not consider a task complete until build and tests pass

**The two test projects need different runners.** `dotnet test` on the whole solution fails, because it hits the Application project:

- **`Application.UnitTests`** references `xunit.v3` (Microsoft.Testing.Platform) and self-hosts an executable. `dotnet test` on it fails with *"Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later."* Build it, then run the `.exe` directly. Filter with `-class "Namespace.ClassName"` or `-method "*MethodName"`.
- **`Domain.UnitTests`** references `xunit` v2 (VSTest) and produces only a `.dll` — there is no self-hosting `.exe`. Run `dotnet test` on that csproj specifically. Note that `dotnet run` / `dotnet exec` against it exit 0 *without running any test*, so always confirm the runner printed a test count before reporting a pass.

## Commands

```bash
dotnet build
dotnet build "Aurora Flowboard.slnx"   # whole solution

# Application tests (xunit v3 — run the built executable, NOT dotnet test)
dotnet build test/Aurora.Flowboard.Application.UnitTests/Aurora.Flowboard.Application.UnitTests.csproj
./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests.exe
./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests.exe -class "Aurora.Flowboard.Application.UnitTests.WorkItems.GetWorkItemByCodeHandlerTests"

# Domain tests (xunit v2 / VSTest — dotnet test on the csproj)
dotnet test test/Aurora.Flowboard.Domain.UnitTests/Aurora.Flowboard.Domain.UnitTests.csproj

dotnet ef migrations add <Name> --project src/Aurora.Flowboard.Infrastructure --startup-project src/Aurora.Flowboard.Api
dotnet ef database update --project src/Aurora.Flowboard.Infrastructure --startup-project src/Aurora.Flowboard.Api

# Run locally via Aspire (provisions Postgres, wires connection string, sets up dashboard)
dotnet run --project "src/Aurora.Flowboard.AppHost"

# Build and run the API image directly (no Aspire, requires an external Postgres via ConnectionStrings__Database)
docker build -f src/Aurora.Flowboard.Api/Dockerfile -t aurora-flowboard-api .
docker run -p 8080:8080 aurora-flowboard-api
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

- **Domain tests**: assert entity behavior and domain events. Use `BaseTest` helpers. Pattern: `*Data.cs` builders + `*Tests.cs` xUnit facts. Stack: xUnit **v2** + FluentAssertions.
- **Application tests**: test CQRS handler logic with NSubstitute mocks and `MockDbSetHelper`. Stack: xUnit **v3** + NSubstitute + FluentAssertions.
- **Assign mock `DbSet`s to a local before `Returns(...)`.** `MockDbSetHelper.CreateMockDbSet(...)` builds a substitute internally, and NSubstitute throws `CouldNotSetReturnDueToNoLastCallException` if you nest it inside `Returns(...)`. Write `DbSet<X> xMock = MockDbSetHelper.CreateMockDbSet([...]); _dbContext.X.Returns(xMock);` — never `_dbContext.X.Returns(MockDbSetHelper.CreateMockDbSet([...]))`.
- `MockDbSetHelper` runs on real LINQ-to-Objects, so `Skip`/`Take`/`OrderBy` behave for real — **paginated handlers must be tested across a page boundary** (3+ items, `pageSize` 2, asserting page 1 and page 2 hold *different* items). Asserting only page 1 or only an out-of-range page does not exercise the `Skip` offset. It does **not** exercise EF translation, though: provider-level concerns (`AsSplitQuery`, SQL shape, subquery translation) need the real Npgsql provider and must be verified by running the app and reading the SQL logs.
