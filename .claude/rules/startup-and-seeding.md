---
paths:
  - "src/Aurora.Flowboard.Api/Program.cs"
  - "src/Aurora.Flowboard.Api/Extensions/**/*.cs"
  - "src/Aurora.Flowboard.Infrastructure/Bootstrap/**/*.cs"
  - "src/**/appsettings*.json"
  - "src/Aurora.Flowboard.AppHost/**/*.cs"
---

# Startup: migrations and seeding

## Migrations apply on startup

Migrations auto-apply on startup in every environment, including Production, controlled by `Database:ApplyMigrationsOnStartup` (default `true`; `Api/Extensions/MigrationServiceExtensions.cs`). Set `Database__ApplyMigrationsOnStartup=false` to disable and apply migrations manually instead.

## Default Administrator seeding

- On every startup, right after migrations apply, `SeedAdministratorAsync` (`Api/Extensions/SeedingServiceExtensions.cs`) creates a default `Role.Administrator` user if none exists yet in `flowboard.user_roles` (idempotent no-op otherwise).
- Why: `POST users` requires an existing Administrator, so the very first one must be created outside that endpoint.
- Credentials come from the `Bootstrap` config section (`BootstrapOptions`, `Infrastructure/Bootstrap/`):
  - `AdminEmail` / `AdminPassword` are required and set per environment (`Bootstrap__AdminEmail` / `Bootstrap__AdminPassword` env vars in staging/prod, e.g. via Dokploy secrets). **Never commit real values.**
  - `AdminFirstName` / `AdminLastName` default to `"System"` / `"Administrator"`.
- There is no forced-password-change mechanism — rotating the seeded password after first login is an operational convention, not enforced by the domain.

## Template flows seeding

`SeedTemplateFlowsAsync` (same file) seeds the default `TemplateFlow` per `ProjectKind` at startup.
