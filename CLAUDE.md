# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Aurora Flowboard is a .NET 10 internal REST API for software project management. It follows **Clean Architecture + DDD** with a modular monolith approach. The stack is .NET 10, Entity Framework Core, and PostgreSQL with JWT authentication and RBAC.

## Commands

```bash
# Build
dotnet build

# Run tests
dotnet test

# Run a single test
dotnet test --filter "FullyQualifiedName~TestClassName"

# EF migrations (once Infrastructure layer exists)
dotnet ef migrations add <MigrationName> --project src/Aurora.Flowboard.Infrastructure
dotnet ef database update --project src/Aurora.Flowboard.Infrastructure
```

The solution file is `Aurora Flowboard.slnx` (modern slim format).

## Architecture

The intended layer structure per the PRD:

```
Aurora.Flowboard.API          → Controllers, middleware, DI composition root
Aurora.Flowboard.Application  → Use cases, commands/queries (CQRS), handlers
Aurora.Flowboard.Domain       → Entities, domain events, business rules (no dependencies)
Aurora.Flowboard.Infrastructure → EF Core, PostgreSQL, repositories, external services
```

Currently only the Domain layer exists. New layers should be added as separate `.csproj` projects under `src/`.

## Domain Abstractions

All domain abstractions live in `src/Aurora.Flowboard.Domain/Abstractions/`.

**Result pattern (railway-oriented programming):** Business operations return `Result` or `Result<T>` — never throw exceptions for business errors. Use `Result.Ok(value)` for success and `Result.Fail(error)` for failure. `BaseError` has static factory methods: `Failure`, `Validation`, `NotFound`, `Conflict`.

**Entities:** Inherit from `BaseEntity`, which provides a `Guid Id` (init-only) and domain event management (`AddDomainEvent`, `ClearDomainEvents`).

**Domain events:** Inherit from `DomainEvent` (which implements `IDomainEvent`). Events are raised on entities and dispatched by the infrastructure layer. `SerializerSettings` provides Newtonsoft.Json settings for type-aware event serialization.

**Password hashing:** Always go through `IPasswordHasher` — never hash directly in domain or application code.

## Core Domain Modules (planned per PRD)

- **Projects** — project lifecycle, user membership
- **WorkItems** — types: Historia, Bug, Tarea Técnica, Investigación; priority, estimation, assignment
- **Flows** — configurable FlowStates and transitions with role-based validations (e.g., Backlog → In Progress → Code Review → QA → Done)
- **Boards** — Kanban view, grouping, filtering
- **Users** — RBAC roles: Admin, Analyst, Developer, QA

## Code Style Enforcement

`Directory.Build.props` treats **warnings as errors** and enables SonarAnalyzer. The `.editorconfig` enforces many rules as errors — key ones:

- File-scoped namespaces (`namespace Foo;` not `namespace Foo { }`)
- No `var` for built-in types; `var` is allowed when the type is apparent
- No `this.` qualification
- Namespace must match folder structure
- Expression-bodied members for properties and lambdas where applicable
- Null propagation (`?.`) required over null checks where possible

`Directory.Packages.props` manages all NuGet versions centrally — add package references without version numbers in individual `.csproj` files; declare the version only in `Directory.Packages.props`.
