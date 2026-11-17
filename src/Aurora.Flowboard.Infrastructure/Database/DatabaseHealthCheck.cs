using Microsoft.Extensions.Logging;

namespace Aurora.Flowboard.Infrastructure.Database;

// REMARK: /health is anonymous and writes each check's description, so the failure description is
// generic: it must not carry the host, the port or the exception message. An exception that escapes a
// check becomes its description (ex.Message), so the check catches it and logs it server-side instead.
internal sealed class DatabaseHealthCheck(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    ILogger<DatabaseHealthCheck> logger) : IHealthCheck
{
    internal const string Name = "database";
    internal const string ReadyTag = "ready";
    internal static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private const string UnreachableDescription = "Database is unreachable.";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            bool canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

            return canConnect
                ? HealthCheckResult.Healthy()
                : new HealthCheckResult(context.Registration.FailureStatus, UnreachableDescription);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Cancellation propagates: the health check service reports the timeout with its own message.
            logger.LogWarning(ex, "Database health check failed.");

            return new HealthCheckResult(context.Registration.FailureStatus, UnreachableDescription);
        }
    }
}
