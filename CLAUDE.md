# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Aurora Flowboard is a .NET 10 internal REST API for software project management. It follows **Clean Architecture + DDD** with a modular monolith approach. The stack is .NET 10, Entity Framework Core, and PostgreSQL with JWT authentication and RBAC.

## Tech stack
- .NET 10 / C#
- ASP.NET Core (Minimal APIs)
- Entity Framework Core
- PostgreSQL
- Redis for caching
- Scrutor for DI assembly scanning
- Serilog for logging

## Architecture

The intended layer structure per the PRD:

```
Aurora.Flowboard.API          → Controllers, middleware, DI composition root
Aurora.Flowboard.Application  → Use cases, commands/queries (CQRS), handlers
Aurora.Flowboard.Domain       → Entities, domain events, business rules (no dependencies)
Aurora.Flowboard.Infrastructure → EF Core, PostgreSQL, repositories, external services
```

## Workflow
1. Ask clarifying questions if requirements are unclear.
2. Propose a plan and list files to change.
3. Implement the smallest viable change.
4. Add or update tests when appropriate.
5. Provide commands to verify changes.

## Hard rules
- Do not introduce new architectural layers.
- Do not add frameworks we do not already use.
- Always pass CancellationToken through async calls.
- No sync over async.
- No Task.Run in request handlers.
- Outbound HTTP calls must have timeouts and cancellation.
- Caching must consider time budgets and stampede protection.

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

## Code Style Enforcement

`Directory.Build.props` treats **warnings as errors** and enables SonarAnalyzer. The `.editorconfig` enforces many rules as errors — key ones:

- File-scoped namespaces (`namespace Foo;` not `namespace Foo { }`)
- No `var` for built-in types; `var` is allowed when the type is apparent
- No `this.` qualification
- Namespace must match folder structure
- Expression-bodied members for properties and lambdas where applicable
- Null propagation (`?.`) required over null checks where possible
- Use constants — avoid magic numbers or strings

`Directory.Packages.props` manages all NuGet versions centrally — add package references without version numbers in individual `.csproj` files; declare the version only in `Directory.Packages.props`.
