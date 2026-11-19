---
paths:
  - "src/Aurora.Flowboard.Api/Extensions/HealthCheckExtensions.cs"
  - "src/Aurora.Flowboard.Api/DependencyInjection.cs"
  - "src/Aurora.Flowboard.ServiceDefaults/Extensions.cs"
  - "src/Aurora.Flowboard.Infrastructure/Database/DatabaseHealthCheck.cs"
  - "src/Aurora.Flowboard.Api/Dockerfile"
  - "src/Aurora.Flowboard.Api/Extensions/RateLimitingExtensions.cs"
---

# Health checks and observability

## Two endpoints, two questions

| Endpoint | Mapped by | Runs | Answers |
|---|---|---|---|
| `/alive` | `ServiceDefaults` `MapDefaultEndpoints`, every environment | Checks tagged `live` (`self`) | Is the process responding? Plain status text. |
| `/health` | Api `MapHealthCheckEndpoints`, every environment | Every check (`self`, `database`) | Can it serve requests? JSON with each check's status and description; 503 when unhealthy. |

- `/health` opens a database connection per call, so it requires the `Health` rate limiting policy (`RateLimitingExtensions`): 30 requests per 60 s per client IP, fixed in code and separate from the `Auth` quota. Excess calls get 429. `/alive` touches nothing and has no limit.
- Point the container or orchestrator health check (Dokploy) at `/alive`, never at `/health`. A database outage makes `/health` return 503, and restarting the API would not fix the database: it would only loop. `/health` is for external monitoring.
- The `aspnet:10.0` image has no `curl` or `wget`, so a Docker `HEALTHCHECK` that relies on them does not work.
- Each route is mapped in exactly one place. Mapping `/health` in both `ServiceDefaults` and the Api gave the route two candidates and threw `AmbiguousMatchException` in Development.
- Both endpoints are anonymous. The `ServiceDefaults` tracing filter excludes them. The Api's `AddObservability` adds ASP.NET Core instrumentation without that filter, so check the exclusion again when the two configurations are merged (see Known debt).

## Readiness checks

- `DatabaseHealthCheck` (Infrastructure, tag `ready`, registered in `AddDatabaseServices` with a 5 s timeout) creates a context from `IDbContextFactory<ApplicationDbContext>` and calls `Database.CanConnectAsync`.
- `/health` writes each check's description to an anonymous caller. A failing check returns a fixed description (`"Database is unreachable."`), never the host, the port or the exception message.
- An exception that escapes a check becomes its description (`ex.Message`). Checks therefore catch everything except `OperationCanceledException`, log it server-side and return the fixed description. Cancellation propagates so the registration timeout reports its own message.
- A new dependency check goes in the layer that owns the dependency, tagged `ready`, with a timeout. Never tag it `live`.

## Swagger

- Swagger UI is only served in Development and Staging (`Program.cs`).
- It declares the `Bearer` HTTP scheme with a global requirement, so the Authorize button sends the access token from `POST auth/login`. Anonymous endpoints also show the lock; the header is only sent once a token is entered.
- Swashbuckle 10 uses Microsoft.OpenApi v2 (`using Microsoft.OpenApi;`), and `AddSecurityRequirement` takes a `Func<OpenApiDocument, OpenApiSecurityRequirement>` with `OpenApiSecuritySchemeReference`.

## Known debt (after 1.0)

- **M26:** OpenTelemetry is configured twice: `ServiceDefaults` `ConfigureOpenTelemetry` and Api `AddObservability`. The Api one adds `AddNpgsql()` tracing and the service resource name. Merge them into `ServiceDefaults` without touching the health endpoint mapping. The Dockerfile also exposes 8081 unused.
- **M27:** Synchronous updates return 202 (`ApiResponses.Accepted`) instead of 204, and creates return 201 with an empty `Location`.
