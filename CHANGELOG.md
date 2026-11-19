# Changelog

All notable changes to the Aurora Flowboard API are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2026-11-19

First stable release. All endpoints are served under `/api/v1/flowboard`.

### Added

#### Authentication and users
- Login with a JWT access token and an opaque refresh token, refresh, logout and password change.
- Role-based authorization with two roles, `Administrator` and `Member`.
- User management: create users and change their role (Administrator only), list users, and the current user's profile and summary.
- A default Administrator is seeded at startup.

#### Projects
- Create, list, get and update projects, and change their kind (`Product`, `Client`, `Research`, `Internal`) and status (`Active`, `Maintenance`, `Completed`, `Archived`).
- Project members with project roles (`Admin`, `Analyst`, `Developer`, `QA`, `Viewer`).
- One flow per project, defined at creation from the flow template of its kind and read-only afterwards.
- Board grouped by the flow's active states, with the transitions available in each column.
- Paginated project change log.

#### Milestones and components
- Milestones with an optional color and a status lifecycle.
- Components that can be renamed and retired.
- Both are managed by project administrators.

#### Work items
- Create work items and search the open work items of the user's active projects, with filters and pagination.
- Get a work item by code, with its tags and available transitions. Comments, time entries, state history and change log have their own paginated endpoints.
- Update the title, description, type, priority, estimated points and estimated completion date.
- Move a work item between states following the project's transition rules.
- Assign and unassign, change its milestone and component, comments, tags and time logging.
- `Viewer` members can read work items but not modify them.

#### Dashboard
- Dashboard for the current user's active projects.

#### Operations
- `/alive` liveness and `/health` readiness endpoints in every environment. `/health` checks the database.
- Rate limiting on the authentication endpoints and on `/health`.
- OpenTelemetry logs, traces and metrics.
- Swagger with Bearer authentication.
- EF Core migrations applied on startup.
- Domain events delivered through an outbox, at least once, with retries and dead letters.

### Security
- Refresh tokens are stored hashed. Reusing a token concurrently is rejected.
- JWT options are validated at startup. The signing key must be at least 32 bytes.
- The last active Administrator cannot be demoted, and it is restored at startup.
- Pipeline logs no longer include request payloads or response values.
- Outbox deserialization only accepts the application's domain event types.
- Optimistic concurrency on aggregate roots: conflicting writes return `409 Conflict`.

### Deployment notes
- Required configuration:
  - `ConnectionStrings__Database`;
  - `Jwt__Key`, a random value of at least 32 bytes per environment (for example `openssl rand -base64 48`);
  - `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` behind a reverse proxy.
- Point the container health check at `/alive`. Use `/health` for readiness.
- Images are published to `ghcr.io/gerardogarnica/aurora-flowboard-api` as `:1.0.0` and `:1.0`.

### Known limitations
These are planned for later releases.

- An access token stays valid until it expires (60 minutes by default) after logout, a password change or a role change.
- Login has per-IP rate limiting but no per-account lockout or backoff.
- The user and project lists are not paginated.
- Synchronous updates return `202 Accepted`, and creates return `201 Created` with an empty `Location` header.
- PBKDF2 uses 310,000 iterations, and the iteration count is not stored in the hash.
- Failed outbox messages are retried every interval without backoff.

[Unreleased]: https://github.com/gerardogarnica/aurora-flowboard-api/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/gerardogarnica/aurora-flowboard-api/releases/tag/v1.0.0
