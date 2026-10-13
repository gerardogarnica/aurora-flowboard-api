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

- On every startup, right after migrations apply, `SeedAdministratorAsync` (`Api/Extensions/SeedingServiceExtensions.cs`) ensures there is at least one **active** `Role.Administrator` user (idempotent no-op otherwise). Inactive admins don't count: login rejects them.
- If none exists and a user with `Bootstrap:AdminEmail` already exists, the seeder **restores** that user (reactivates it and changes its role to Administrator, leaving the password untouched) and logs a warning. It only inserts a new user when that email is free; inserting blindly would hit the unique email index and abort startup.
- Why: `POST users` requires an existing Administrator, so the very first one must be created outside that endpoint.
- Credentials come from the `Bootstrap` config section (`BootstrapOptions`, `Infrastructure/Bootstrap/`):
  - `AdminEmail` / `AdminPassword` are required and set per environment (`Bootstrap__AdminEmail` / `Bootstrap__AdminPassword` env vars in staging/prod, e.g. via Dokploy secrets). **Never commit real values.**
  - `AdminFirstName` / `AdminLastName` default to `"System"` / `"Administrator"`.
- There is no forced-password-change mechanism — rotating the seeded password after first login is an operational convention, not enforced by the domain.

## Last Administrator guard

- `PATCH users/{id}/role` rejects demoting the last active Administrator with `User.LastAdministrator` (409). The handler queries for *other* active Administrators only when demoting an active one and passes the result to `User.ChangeRole(newRole, hasOtherActiveAdministrator, ...)`, which owns the rule. Demoting an inactive Administrator is allowed.
- `User.IsAdministrator` is the in-memory check shared by the handler, the domain and the seeder. It is ignored by EF and not translatable, so queries still use `u.Roles.Any(r => r.Name == Role.Administrator.Name)`.
- Two concurrent demotions can still race past the check (no locking yet; tracked by the optimistic concurrency work, I9); the seeder recovers on the next restart.
- `User.Deactivate` has **no** such guard yet because no endpoint calls it. When a deactivate endpoint is added, give `Deactivate` the same `hasOtherActiveAdministrator` parameter and `LastAdministrator` failure: deactivating the last active Administrator locks everyone out just like demoting it.

## Template flows seeding

`SeedTemplateFlowsAsync` (same file) seeds the default `TemplateFlow` per `ProjectKind` at startup.
