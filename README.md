# Aurora Flowboard API

Aurora Flowboard is an internal REST API for software project management that helps teams track projects, workflows, work items, and time entries efficiently and securely.

## Table of Contents

- [Technologies](#technologies)
- [Features](#features)
  - [Authentication and Users](#authentication-and-users)
  - [Projects](#projects)
  - [Flows](#flows)
  - [Work Items](#work-items)
  - [Architecture and Quality](#architecture-and-quality)
- [Installation](#installation)
- [Usage](#usage)
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
- **Swagger/OpenAPI**: Interactive API documentation via Swashbuckle.
- **OpenTelemetry**: Structured logging, tracing, and metrics.
- **Scrutor**: Assembly scanning for automatic DI registration.
- **Clean Architecture**: Clear separation of concerns (Domain, Application, Infrastructure, API).
- **Outbox pattern**: Reliable domain event publishing for async processing and consistency.
- **Automated migrations**: EF Core migrations auto-apply on startup in Development/Staging.

## Features

### Authentication and Users

- **Login** (`POST /api/v1/flowboard/auth/login`): Sign in and receive a JWT access token and refresh token.
- **Create user** (`POST /api/v1/flowboard/users`): Administrator-only endpoint to create new user accounts.
- **Get my profile** (`GET /api/v1/flowboard/users/me`): Retrieve the authenticated user's profile.
- Role-based authorization (RBAC) with two roles, `Administrator` and `Member`, enforced across all protected endpoints.
- Refresh-token renewal and self-service password change are modeled in the domain (`User.IssueToken`, `RevokeToken`, `ChangePassword`) but not yet exposed as endpoints.

### Projects

- **Create project** (`POST /api/v1/flowboard/projects`): Define a new project with name, description, and a unique project code.
- **List projects** (`GET /api/v1/flowboard/projects`): Retrieve all projects accessible to the authenticated user.
- **Get project** (`GET /api/v1/flowboard/projects/{id}`): Fetch full project details including members and change log.
- **Update project** (`PUT /api/v1/flowboard/projects/{id}`): Modify project name and description.
- **Project members**: Add and remove members with role assignments (`ProjectRole`).
- **Change log**: Automatic audit trail of every project modification.

### Flows

- **Create flow** (`POST /api/v1/flowboard/flows`): Define a workflow with a set of states and transitions for a project.
- **List flows** (`GET /api/v1/flowboard/flows`): Retrieve flows associated with a project.
- **Get flow** (`GET /api/v1/flowboard/flows/{id}`): Fetch flow details including states and allowed transitions.
- **Update flow** (`PUT /api/v1/flowboard/flows/{id}`): Modify flow name and description.
- **Activate / Deactivate flow** (`PUT /api/v1/flowboard/flows/{id}/activate`, `PUT /api/v1/flowboard/flows/{id}/deactivate`): Control whether a flow is available for work items.
- **Flow states**: Define ordered states (e.g. Backlog, In Progress, Done) within a flow.
- **Flow transitions**: Specify allowed state transitions to enforce workflow rules.

### Work Items

- **Create work item** (`POST /api/v1/flowboard/workitems`): Open a new work item with title, description, type, and priority.
- **Board view** (`GET /api/v1/flowboard/projects/{projectId}/work-items`): Work items for a project grouped by the project's default flow states (Kanban board shape), ordered by state and priority.
- **Get work item** (`GET /api/v1/flowboard/workitems/{id}`): Full detail view including comments, time entries, tags, and transition history.
- **Update work item** (`PUT /api/v1/flowboard/workitems/{id}`): Edit title, description, priority, and assignee.
- **Transition state** (`PUT /api/v1/flowboard/workitems/{id}/transition`): Move a work item to the next state following flow transition rules.
- **Comments**: Add and list comments on a work item.
- **Time entries**: Log and list time spent (`TimeEntry`) on a work item.
- **Tags**: Attach and remove tags (`WorkItemTag`) for classification and filtering.
- **Change log**: Automatic audit trail of every work item modification.

### Architecture and Quality

- **Business validations**: Rules enforced in domain and application layers; no logic leaks into infrastructure.
- **CQRS**: Every operation modeled as an `ICommand` or `IQuery` with a dedicated handler.
- **Result type**: Railway-oriented `Result<T>` with typed error categories (`NotFound`, `Conflict`, `Validation`, `Forbidden`), mapped to HTTP status codes via `ResultExtensions.Match`.
- **Behavior pipeline**: `LoggingBehavior → PerformanceBehavior → ValidationBehavior → Handler` applied to every command and query.
- **Domain events**: Raised from aggregate roots and dispatched via the Outbox pattern for reliable side-effect processing.
- **Error handling**: Problem Details (RFC 9457) responses and global exception middleware.
- **RESTful API**: All routes grouped under `/api/v1/flowboard`, documented with Swagger.

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

2. **Run the AppHost**

   ```bash
   dotnet run --project "src/Aurora.Flowboard.AppHost"
   ```

Migrations apply automatically on startup in Development. The Api project's URL and Swagger link are shown in the Aspire dashboard.

### Option B — Run the API directly

1. **Clone the repository and restore dependencies**

   ```bash
   git clone https://github.com/gerardogarnica/aurora-flowboard-api.git
   cd aurora-flowboard-api
   dotnet restore
   ```

2. **Configure user secrets**

   Set the JWT signing key and connection string via user secrets so nothing sensitive is committed:

   ```bash
   dotnet user-secrets set "Jwt:Key" "<your-256-bit-key>" --project src/Aurora.Flowboard.Api
   dotnet user-secrets set "ConnectionStrings:Database" "Host=localhost;Database=flowboard;Username=postgres;Password=postgres" --project src/Aurora.Flowboard.Api
   ```

3. **Apply database migrations**

   ```bash
   dotnet ef database update --project src/Aurora.Flowboard.Infrastructure --startup-project src/Aurora.Flowboard.Api
   ```

4. **Run the API**

   ```bash
   dotnet run --project src/Aurora.Flowboard.Api
   ```

### Option C — Run via Docker

Requires an external Postgres reachable via `ConnectionStrings__Database`.

```bash
docker build -f src/Aurora.Flowboard.Api/Dockerfile -t aurora-flowboard-api .
docker run -p 8080:8080 -e ConnectionStrings__Database="Host=host.docker.internal;Database=flowboard;Username=postgres;Password=postgres" aurora-flowboard-api
```

## Usage

1. Open in your browser: **http://localhost:5000/swagger** (or the URL shown in the terminal / Aspire dashboard).
2. Use the Swagger documentation to explore and test endpoints under the `/api/v1/flowboard/` prefix.
3. For protected endpoints, call `auth/login` first, then include the token in `Authorization: Bearer <token>`.

## Project Structure

Aurora Flowboard follows Clean Architecture with a modular monolith approach:

| Layer | Project | Contents |
|-------|---------|----------|
| **Domain** | `Aurora.Flowboard.Domain` | Entities, value objects, business rules, domain events, enums, `Result` type. |
| **Application** | `Aurora.Flowboard.Application` | CQRS handlers, DTOs, FluentValidation validators, behavior pipeline, infrastructure interfaces. |
| **Infrastructure** | `Aurora.Flowboard.Infrastructure` | EF Core `DbContext`, Fluent API configurations, migrations, JWT, password hashing, Outbox, time services. |
| **API** | `Aurora.Flowboard.Api` | Minimal API endpoints, middleware, Swagger, DI composition root. |
| **Orchestration** | `Aurora.Flowboard.AppHost` | .NET Aspire orchestration — provisions Postgres and wires the Api project for local development. |
| **Orchestration** | `Aurora.Flowboard.ServiceDefaults` | Shared Aspire defaults — OpenTelemetry, health checks, resilience. |

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
└── Aurora.Flowboard.Domain.UnitTests/
```

## Roadmap and Upcoming Releases

Planned features and improvements for future versions:

### Upcoming releases

- **Refresh token and change password endpoints**: Expose the existing `User.IssueToken`, `RevokeToken`, and `ChangePassword` domain behavior via `auth/refresh` and `auth/change-password` endpoints.
- **Dashboards and reporting**: Summary views per project — velocity, cycle time, work item distribution by state and assignee.
- **Notifications**: In-app or webhook notifications on work item assignment, state transitions, and comments.
- **Attachments**: File attachments on work items stored in object storage.
- **Sub-items**: Hierarchical work items (epics → stories → tasks).
- **Sprints / iterations**: Time-boxed planning cycles with capacity tracking.
- **Search**: Full-text search across work items and comments.
- **E2E tests**: End-to-end test suite against the API for regression and contract validation.

### Under consideration

- Multi-tenant support for organizations hosting multiple independent teams.
- GitHub / GitLab integration to link commits and pull requests to work items.
- Public API versioning (v1, v2) with explicit breaking-change documentation.

## Contributing

Contributions are welcome:

1. Fork the repository.
2. Create a branch (`git checkout -b feature/your-feature-name`).
3. Commit your changes (`git commit -m 'Add some feature'`).
4. Push to the branch (`git push origin feature/your-feature-name`).
5. Open a Pull Request.

Please ensure `dotnet build` and `dotnet test` pass before submitting.

## License

This project is licensed under the MIT License. See the `LICENSE` file for details.

## Contact

- **Author**: Gerardo Garnica
- **Email**: [gerardo.garnica@gmail.com](mailto:gerardo.garnica@gmail.com)
- **GitHub**: [@gerardogarnica](https://github.com/gerardogarnica)

## Acknowledgements

- [othneildrew's Best-README-Template](https://github.com/othneildrew/Best-README-Template) for inspiration.
- The open-source community for continuous support and contributions.
