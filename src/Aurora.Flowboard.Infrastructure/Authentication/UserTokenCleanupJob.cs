using Aurora.Flowboard.Infrastructure.Database;
using Microsoft.Extensions.Logging;

namespace Aurora.Flowboard.Infrastructure.Authentication;

/// <summary>
/// Deletes user tokens whose refresh token expired more than <see cref="UserTokenCleanupOptions.RetentionDays"/>
/// ago. Revoked tokens expire on the same schedule as active ones, so expiry alone covers both.
/// Rows are deleted in batches to avoid one long-running DELETE locking the table.
/// </summary>
[DisallowConcurrentExecution]
internal sealed class UserTokenCleanupJob(
    ApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IOptions<UserTokenCleanupOptions> options,
    ILogger<UserTokenCleanupJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        UserTokenCleanupOptions cleanupOptions = options.Value;
        DateTime cutoff = dateTimeProvider.UtcNow.AddDays(-cleanupOptions.RetentionDays);

        int totalDeleted = 0;
        int deleted;

        do
        {
            deleted = await dbContext.UserTokens
                .Where(t => t.RefreshTokenExpiresOnUtc < cutoff)
                .OrderBy(t => t.RefreshTokenExpiresOnUtc)
                .Take(cleanupOptions.BatchSize)
                .ExecuteDeleteAsync(context.CancellationToken);

            totalDeleted += deleted;
        }
        while (deleted == cleanupOptions.BatchSize);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Deleted {Count} user tokens that expired before {Cutoff}.", totalDeleted, cutoff);
        }
    }
}
