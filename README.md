# Aurora Flowboard API

Aurora Flowboard is an internal REST API for software project management that helps teams track projects, workflows, milestones, work items, and time entries efficiently and securely.

The current version is **1.0.0**. See the [CHANGELOG](CHANGELOG.md) for release notes.

## Table of Contents

- [Technologies](#technologies)
- [Features](#features)
  - [Authentication and Users](#authentication-and-users)
  - [Projects](#projects)
  - [Flows](#flows)
  - [Milestones and Components](#milestones-and-components)
  - [Work Items](#work-items)
  - [Dashboard](#dashboard)
  - [Operations](#operations)
  - [Architecture and Quality](#architecture-and-quality)
- [Installation](#installation)
- [Usage](#usage)
- [Deployment and Releases](#deployment-and-releases)
- [Project Structure](#project-structure)
- [Roadmap and Upcoming Releases](#roadmap-and-upcoming-releases)
- [Contributing](#contributing)
- [License](#license)
- [Contact](#contact)
- [Acknowledgements](#acknowledgements)

## Technologies

- **.NET 10**: Modern, high-performance backend framework.
- **C# 14**: Latest language features (records, primary constructors, etc.).
- **.NET Aspire**: Local orchestration (Postgres container provisioning) and shared service defaults.
- **Entity Framework Core**: ORM for data access and persistence with PostgreSQL.
- **PostgreSQL**: Robust, open-source relational database (snake_case schema via EFCore.NamingConventions).
- **FluentValidation**: Command validation and consistent error handling.
- **JWT**: Authentication with access and refresh tokens (PBKDF2 password hashing) and role-based authorization (RBAC).
- **Swagger/OpenAPI**: Interactive API documentation via Swashbuckle, with Bearer authentication.
- **OpenTelemetry**: Structured logging, tracing, and metrics.
- **Quartz.NET**: Background jobs (outbox processing and expired token cleanup).
- **Scrutor**: Assembly scanning for automatic DI registration.
- **xUnit v3**: Unit and architecture test suites, run through the Microsoft.Testing.Platform host.
- **NetArchTest**: Architecture tests that enforce layer boundaries and project conventions on every build.
- **Clean Architecture**: Clear separation of concerns (Domain, Application, Infrastructure, API).
- **Outbox pattern**: Reliable domain event publishing with retries and dead letters.
- **Automated migrations**: EF Core migrations apply on startup (`Database:ApplyMigrationsOnStartup`).

## Features

All routes are grouped under `/api/v1/flowboard`.

### Authentication and Users

- **Login** (`POST auth/login`): Sign in and receive a JWT access token and a refresh token.
- **Refresh token** (`POST auth/refresh-token`): Exchange a refresh token for a new token pair. Refresh tokens are stored hashed, and reusing one concurrently is rejected.
- **Logout** (`POST auth/logout`): Revoke the refresh token.
- **Change password** (`PUT auth/change-password`): Change the current user's password.
- **Users** (`POST users`, `GET users`, `PATCH users/{id}/role`): Create users and change their role (Administrator only), and list users.
- **My profile and summary** (`GET users/me`, `GET users/my-summary`): The authenticated user's profile and summary.
- Role-based authorization (RBAC) with two roles, `Administrator` and `Member`, enforced across all protected endpoints. A default Administrator is seeded at startup, and the last active Administrator cannot be demoted.

### Projects

- **Create project** (`POST projects`): Define a new project with name, description, kind, a unique project code, and its flow.
- **List projects** (`GET projects`): Retrieve all projects accessible to the authenticated user.
- **Get and update project** (`GET projects/{id}`, `PUT projects/{id}`): Project details with members, and name and description changes.
- **Kind and status** (`PATCH projects/{id}/kind`, `PATCH projects/{id}/activate|maintenance|complete|archive`): Change the `ProjectKind` (`Product`, `Client`, `Research`, `Internal`) and move through the `ProjectStatus` lifecycle (`Active`, `Maintenance`, `Completed`, `Archived`).
- **Project members** (`POST projects/{id}/members`, `DELETE projects/{id}/members/{userId}`): Add and remove members with a project role (`Admin`, `Analyst`, `Developer`, `QA`, `Viewer`).
- **Board** (`GET projects/{projectId}/board`): In-flight work items grouped by the project's `Active` flow states (Kanban board shape), ordered by state and priority, with the transitions available in each column. Items in a `Completed` or `Cancelled` state are not on the board.
- **Change log** (`GET projects/{id}/change-logs`): Paginated audit trail of every project modification.

### Flows

Each project has exactly one flow, owned by the `Project` aggregate. There is no standalone flow entity.

- **Flow states**: Ordered states (e.g. Backlog, In Progress, Done) classified as `Active`, `Completed`, or `Cancelled`. Only `Active` states carry an order and render as board columns; `Completed` and `Cancelled` are terminal.
- **Flow transitions**: Allowed state transitions, each carrying the set of project roles permitted to perform it.
- **Defined at project creation**: the flow states are supplied in the `POST projects` payload; clients pre-fill them from the flow template for the project's `ProjectKind` (`GET template-flows/{kind}`).
- **Read-only afterwards**: there are no endpoints to edit a project's flow. The `Active` flow states are surfaced through the board; the terminal ones only through a work item's available transitions.

### Milestones and Components

Milestones and components belong to a project. Only project admins can change them, and only while the project is `Active` or in `Maintenance`.

- **Milestones** (`POST projects/{id}/milestones`, `GET projects/{id}/milestones`, `PUT milestones/{id}`): Create, list, and update milestones, with an optional color.
- **Milestone status** (`PATCH milestones/{id}/activate|hold|complete|archive`): Move a milestone through its status lifecycle.
- **Components** (`POST projects/{id}/components`, `GET projects/{id}/components`, `PATCH components/{id}`, `PATCH components/{id}/retire`): Create, list, rename, and retire components.

### Work Items

- **Create work item** (`POST work-items`): Open a new work item with title, description, type, and priority.
- **Search work items** (`GET work-items`): Paginated search of the open work items of the user's active projects, with filters (`assignee=me`, `createdBy=me`, `stage`, `stalled`, `priority`).
- **Get work item** (`GET work-items/{code}`): Detail view by code, with tags and the available transitions. Comments, time entries, state history, and change log have their own paginated endpoints.
- **Update fields** (`PATCH work-items/{id}/title|description|type|priority|estimated-points|estimated-completion-date`): Edit one field at a time.
- **Move** (`PATCH work-items/{id}/move`): Move a work item to another state following the flow transition rules for the user's project role.
- **Assignment** (`PATCH work-items/{id}/assign`, `PATCH work-items/{id}/unassign`): Assign or unassign a project member.
- **Milestone and component** (`PATCH work-items/{id}/milestone`, `PATCH work-items/{id}/component`): Link a work item to a milestone or component.
- **Comments** (`POST`, `GET work-items/{id}/comments`, `PUT`, `DELETE work-items/{id}/comments/{commentId}`): Add, list (paginated), edit, and remove comments.
- **Tags** (`POST work-items/{id}/tags`, `DELETE work-items/{id}/tags/{tagId}`): Attach and remove tags for classification and filtering.
- **Time entries** (`POST`, `GET work-items/{id}/time-entries`): Log and list (paginated) time spent on a work item.
- **History** (`GET work-items/{id}/state-history`, `GET work-items/{id}/change-logs`): Paginated state transitions and audit trail of every modification.
- Members with the `Viewer` project role can read work items but not modify them.

### Dashboard

- **Dashboard** (`GET dashboard`): Summary of the current user's active projects and their work.

### Operations

- **Health checks**: `/alive` (liveness, no dependencies) and `/health` (readiness, checks the database) in every environment, outside the `/api/v1/flowboard` prefix.
- **Rate limiting**: On the authentication endpoints (configurable via `RateLimiting:Auth`) and on `/health`.
- **Startup validation**: JWT, outbox, and token cleanup options are validated at startup; for example, `Jwt:Key` must be at least 32 bytes.
- **Outbox**: Domain events are delivered at least once by a background job, retried up to `Outbox:MaxAttempts` and then kept as dead letters.

### Architecture and Quality

- **Business validations**: Rules enforced in domain and application layers; no logic leaks into infrastructure.
- **CQRS**: Every operation modeled as an `ICommand` or `IQuery` with a dedicated handler.
- **Result type**: Railway-oriented `Result<T>` with typed error categories (`Failure`, `Validation`, `NotFound`, `Conflict`, `Forbidden`), mapped to HTTP status codes via `ResultExtensions.Match`.
- **Behavior pipeline**: `LoggingBehavior → PerformanceBehavior → ValidationBehavior → PersistenceConflictBehavior → Handler`. Logs never include request payloads or response values.
- **Optimistic concurrency**: Aggregate roots use PostgreSQL `xmin`; conflicting writes and unique-index violations return `409 Conflict`.
- **Domain events**: Raised from aggregate roots and dispatched via the Outbox pattern for reliable side-effect processing.
- **Error handling**: Problem Details (RFC 9457) responses and global exception middleware.
- **Architecture tests**: A dedicated suite (NetArchTest + reflection) that fails the build when a convention is broken — an inward layer referencing an outward one, a domain project pulling in a third-party package, an unsealed or public handler, a command without a validator, a handler living apart from its command, an endpoint reaching into Infrastructure or EF Core, or an endpoint that declares neither `RequireAuthorization` nor `AllowAnonymous`.

## Installation

### Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL (local or container) — not needed if running via Aspire, which provisions it automatically
- Optional: Docker, to run the API image directly or let Aspire provision Postgres in a container

### Option A — Run via .NET Aspire (recommended for local development)

Aspire provisions a Postgres container, wires the connection string automatically, and opens the Aspire dashboard.

1. **Clone the repository**

   ```bash
   git clone https://github.com/gerardogarnica/aurora-flowboard-api.git
   cd aurora-flowboard-api
   ```

2. **Set the JWT signing key** (at least 32 bytes; the API does not start without it)

   ```bash
   dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 48)" --project src/Aurora.Flowboard.Api
   ```

3. **Run the AppHost**

   ```bash
   dotnet run --project "src/Aurora.Flowboard.AppHost"
   ```

Migrations apply automatically on startup. The Api project's URL and Swagger link are shown in the Aspire dashboard.

### Option B — Run the API directly

1. **Clone the repository and restore dependencies**

   ```bash
   git clone https://github.com/gerardogarnica/aurora-flowboard-api.git
   cd aurora-flowboard-api
   dotnet restore
   ```

2. **Configure user secrets**

   Set the JWT signing key (at least 32 bytes) and the connection string via user secrets so nothing sensitive is committed:

   ```bash
   dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 48)" --project src/Aurora.Flowboard.Api
   dotnet user-secrets set "ConnectionStrings:Database" "Host=localhost;Database=flowboard;Username=postgres;Password=postgres" --project src/Aurora.Flowboard.Api
   ```

3. **Run the API** (migrations apply on startup; to apply them manually, run `dotnet ef database update --project src/Aurora.Flowboard.Infrastructure --startup-project src/Aurora.Flowboard.Api`)

   ```bash
   dotnet run --project src/Aurora.Flowboard.Api
   ```

### Option C — Run via Docker

Requires an external Postgres reachable via `ConnectionStrings__Database` and a JWT signing key in `Jwt__Key`.

```bash
docker build -f src/Aurora.Flowboard.Api/Dockerfile -t aurora-flowboard-api .
docker run -p 8080:8080 \
  -e ConnectionStrings__Database="Host=host.docker.internal;Database=flowboard;Username=postgres;Password=postgres" \
  -e Jwt__Key="$(openssl rand -base64 48)" \
  aurora-flowboard-api
```

## Usage

1. Open in your browser: **http://localhost:5000/swagger** (or the URL shown in the terminal / Aspire dashboard).
2. Use the Swagger documentation to explore and test endpoints under the `/api/v1/flowboard/` prefix.
3. For protected endpoints, call `auth/login` first, then use **Authorize** in Swagger (or send `Authorization: Bearer <token>`).
4. Check the service with `GET /alive` (liveness) and `GET /health` (readiness).

## Deployment and Releases

- **CI/CD** (`.github/workflows/build.yml`): every push to `staging` or `main` builds, runs the three test suites, publishes the image to `ghcr.io/gerardogarnica/aurora-flowboard-api` (`:staging` or `:production`, plus `:<commit sha>`), and triggers the Dokploy deploy for that environment.
- **Releases**: the version lives in `Directory.Build.props` (`<Version>`) and in the [CHANGELOG](CHANGELOG.md). Pushing an annotated `vX.Y.Z` tag on a commit already deployed from `main` tags that same image as `:X.Y.Z` and `:X.Y`, without rebuilding or deploying. The tag must match `<Version>`.
- **Required configuration**: `ConnectionStrings__Database`, `Jwt__Key` (a random value of at least 32 bytes per environment), and `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` behind a reverse proxy. Point the container health check at `/alive`.

## Project Structure

Aurora Flowboard follows Clean Architecture with a modular monolith approach:

| Layer | Project | Contents |
|-------|---------|----------|
| **Domain** | `Aurora.Flowboard.Domain` | Entities, value objects, business rules, domain events, enums, `Result` type. |
| **Application** | `Aurora.Flowboard.Application` | CQRS handlers, DTOs, FluentValidation validators, behavior pipeline, infrastructure interfaces. |
| **Infrastructure** | `Aurora.Flowboard.Infrastructure` | EF Core `DbContext`, Fluent API configurations, migrations, JWT, password hashing, Outbox, background jobs, time services. |
| **API** | `Aurora.Flowboard.Api` | Minimal API endpoints, middleware, Swagger, rate limiting, DI composition root. |
| **Orchestration** | `Aurora.Flowboard.AppHost` | .NET Aspire orchestration — provisions Postgres and wires the Api project for local development. |
| **Orchestration** | `Aurora.Flowboard.ServiceDefaults` | Shared Aspire defaults — OpenTelemetry, health checks, resilience. |
| **Tests** | `Aurora.Flowboard.Domain.UnitTests` | Entity invariants, state machines, and domain events. |
| **Tests** | `Aurora.Flowboard.Application.UnitTests` | CQRS handler, validator, and behavior tests, with mocked persistence. |
| **Tests** | `Aurora.Flowboard.ArchitectureTests` | Layer boundaries and project conventions, enforced with NetArchTest and reflection. |

```
src/
├── Aurora.Flowboard.AppHost/
├── Aurora.Flowboard.ServiceDefaults/
├── Aurora.Flowboard.Api/
├── Aurora.Flowboard.Application/
├── Aurora.Flowboard.Domain/
└── Aurora.Flowboard.Infrastructure/
test/
├── Aurora.Flowboard.Application.UnitTests/
├── Aurora.Flowboard.ArchitectureTests/
└── Aurora.Flowboard.Domain.UnitTests/
```

## Roadmap and Upcoming Releases

Planned features and improvements for future versions. Known limitations of 1.0.0 are listed in the [CHANGELOG](CHANGELOG.md).

### Upcoming releases

- **Domain event handlers**: First side effects on the outbox (for example, notifications).
- **Reporting**: Project metrics — velocity, cycle time, work item distribution by state and assignee.
- **Notifications**: In-app or webhook notifications on work item assignment, state transitions, and comments.
- **Attachments**: File attachments on work items stored in object storage.
- **Sub-items**: Hierarchical work items (epics → stories → tasks).
- **Sprints / iterations**: Time-boxed planning cycles with capacity tracking.
- **Search**: Full-text search across work items and comments.
- **Tests**: Infrastructure unit tests and an end-to-end suite against the API for regression and contract validation.

### Under consideration

- Multi-tenant support for organizations hosting multiple independent teams.
- GitHub / GitLab integration to link commits and pull requests to work items.
- Public API versioning (v1, v2) with explicit breaking-change documentation.

## Contributing

Contributions are welcome:

1. Fork the repository.
2. Create a branch from `staging` (`git checkout -b feature/your-feature-name`).
3. Commit your changes (`git commit -m 'Add some feature'`).
4. Push to the branch (`git push origin feature/your-feature-name`).
5. Open a Pull Request against `staging`.

Please ensure `dotnet build` and the full test suite pass before submitting.

All three test projects target xUnit v3 on the Microsoft.Testing.Platform, which self-hosts an executable — `dotnet test` does **not** work on them. Build the solution, then run each test host directly:

```bash
dotnet build "Aurora Flowboard.slnx"

./test/Aurora.Flowboard.Domain.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Domain.UnitTests
./test/Aurora.Flowboard.Application.UnitTests/bin/Debug/net10.0/Aurora.Flowboard.Application.UnitTests
./test/Aurora.Flowboard.ArchitectureTests/bin/Debug/net10.0/Aurora.Flowboard.ArchitectureTests
```

On Windows, append `.exe` to each path. The same three hosts run in CI on every push to `main` and `staging`.

## License

This project is licensed under the MIT License. See the `LICENSE` file for details.

## Contact

- **Author**: Gerardo Garnica
- **Email**: [gerardo.garnica@gmail.com](mailto:gerardo.garnica@gmail.com)
- **GitHub**: [@gerardogarnica](https://github.com/gerardogarnica)

## Acknowledgements

- [othneildrew's Best-README-Template](https://github.com/othneildrew/Best-README-Template) for inspiration.
- The open-source community for continuous support and contributions.
