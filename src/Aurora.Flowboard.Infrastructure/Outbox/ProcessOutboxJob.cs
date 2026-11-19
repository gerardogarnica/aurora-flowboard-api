using Aurora.Flowboard.Infrastructure.Database;
using Aurora.Flowboard.Infrastructure.Serialization;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Aurora.Flowboard.Infrastructure.Outbox;

// Delivery is at-least-once. A failed message stays pending and is retried on the next runs
// until it reaches MaxAttempts, and a run whose token is cancelled rolls back the whole batch, so a handler can see
// the same event more than once and must be idempotent.
[DisallowConcurrentExecution]
internal sealed class ProcessOutboxJob(
    ApplicationDbContext dbContext,
    IDomainEventsDispatcher domainEventsDispatcher,
    IDateTimeProvider dateTimeService,
    IOptions<OutboxOptions> options,
    ILogger<ProcessOutboxJob> logger) : IJob
{
    private const int ErrorMessageMaxLength = 4000;

    public async Task Execute(IJobExecutionContext context)
    {
        CancellationToken cancellationToken = context.CancellationToken;

        logger.LogDebug("Starting to process Flowboard outbox messages.");

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        IReadOnlyCollection<OutboxMessageResponse> messages = await GetOutboxMessagesAsync(cancellationToken);

        foreach (OutboxMessageResponse outboxMessage in messages)
        {
            Exception? exception = null;

            try
            {
                IDomainEvent domainEvent = (IDomainEvent)JsonConvert.DeserializeObject(
                    outboxMessage.Content,
                    SerializerSettings.Instance)!;

                await domainEventsDispatcher.DispatchAsync([domainEvent], cancellationToken);
            }
            // Only the run's own cancellation propagates. A handler's local timeout (TaskCanceledException)
            // is a failed attempt; letting it through would roll back the batch, and the attempt count with
            // it, on every run, so the message would never dead-letter and would block the queue.
            catch (Exception processException) when (!cancellationToken.IsCancellationRequested)
            {
                exception = processException;
            }

            if (exception is null)
            {
                await MarkAsProcessedAsync(outboxMessage.OutboxId, string.Empty, outboxMessage.Attempts, cancellationToken);
            }
            else
            {
                await RegisterFailureAsync(outboxMessage, exception, cancellationToken);
            }
        }

        await transaction.CommitAsync(cancellationToken);

        if (messages.Count > 0 && logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Processed {Count} Flowboard outbox messages.", messages.Count);
        }
    }

    private async Task<IReadOnlyCollection<OutboxMessageResponse>> GetOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        string sql = $"""
                SELECT outbox_id, content, attempts
                FROM {ApplicationDbContext.DefaultSchema}.outbox_messages
                WHERE is_processed = false
                ORDER BY occurred_on_utc
                LIMIT {options.Value.BatchSize}
                FOR UPDATE
                """;

        return await dbContext
            .Database
            .SqlQueryRaw<OutboxMessageResponse>(sql)
            .ToListAsync(cancellationToken);
    }

    private async Task RegisterFailureAsync(
        OutboxMessageResponse outboxMessage,
        Exception exception,
        CancellationToken cancellationToken)
    {
        int attempts = outboxMessage.Attempts + 1;
        string error = Truncate(exception.ToString());

        if (attempts >= options.Value.MaxAttempts)
        {
            logger.LogError(
                exception,
                "Outbox message {OutboxId} failed {Attempts} times and will not be retried.",
                outboxMessage.OutboxId,
                attempts);

            await MarkAsProcessedAsync(outboxMessage.OutboxId, error, attempts, cancellationToken);

            return;
        }

        logger.LogWarning(
            exception,
            "Outbox message {OutboxId} failed (attempt {Attempts} of {MaxAttempts}); it will be retried.",
            outboxMessage.OutboxId,
            attempts,
            options.Value.MaxAttempts);

        var errorParameter = new NpgsqlParameter("Error", error);
        var attemptsParameter = new NpgsqlParameter("Attempts", attempts);
        var outboxIdParameter = new NpgsqlParameter("OutboxId", outboxMessage.OutboxId);

        string sql = $"""
                UPDATE {ApplicationDbContext.DefaultSchema}.outbox_messages
                SET attempts = @Attempts,
                    error = @Error
                WHERE outbox_id = @OutboxId
                """;

        await dbContext
            .Database
            .ExecuteSqlRawAsync(
                sql,
                [errorParameter, attemptsParameter, outboxIdParameter],
                cancellationToken);
    }

    private async Task MarkAsProcessedAsync(
        Guid outboxId,
        string error,
        int attempts,
        CancellationToken cancellationToken)
    {
        var processedOnUtcParameter = new NpgsqlParameter("ProcessedOnUtc", dateTimeService.UtcNow);
        var errorParameter = new NpgsqlParameter("Error", error);
        var attemptsParameter = new NpgsqlParameter("Attempts", attempts);
        var outboxIdParameter = new NpgsqlParameter("OutboxId", outboxId);

        string sql = $"""
                UPDATE {ApplicationDbContext.DefaultSchema}.outbox_messages
                SET is_processed = true,
                    processed_on_utc = @ProcessedOnUtc,
                    error = @Error,
                    attempts = @Attempts
                WHERE outbox_id = @OutboxId
                """;

        await dbContext
            .Database
            .ExecuteSqlRawAsync(
                sql,
                [processedOnUtcParameter, errorParameter, attemptsParameter, outboxIdParameter],
                cancellationToken);
    }

    private static string Truncate(string value) =>
        value.Length > ErrorMessageMaxLength ? value[..ErrorMessageMaxLength] : value;

    internal sealed record OutboxMessageResponse(Guid OutboxId, string Content, int Attempts);
}
