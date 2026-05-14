using Aurora.Flowboard.Infrastructure.Database;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Aurora.Flowboard.Infrastructure.Outbox;

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
        logger.LogInformation("Starting to process Flowboard outbox messages.");

        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        var messages = GetOutboxMessages();

        foreach (OutboxMessageResponse outboxMessage in messages)
        {
            Exception? exception = null;

            try
            {
                IDomainEvent domainEvent = JsonConvert.DeserializeObject<IDomainEvent>(
                    outboxMessage.Content,
                    SerializerSettings.Instance)!;

                await domainEventsDispatcher.DispatchAsync([domainEvent]);
            }
            catch (Exception processException)
            {
                exception = processException;

                logger.LogError(processException, "Exception occurred processing outbox message {OutboxId}. Details: {Message}", outboxMessage.OutboxId, processException.Message);
            }

            await UpdateOutboxMessageAsync(outboxMessage, exception);
        }

        await transaction.CommitAsync();

        logger.LogInformation("Completed processing Flowboard outbox messages.");
    }

    private IReadOnlyCollection<OutboxMessageResponse> GetOutboxMessages()
    {
        string sql = $"""
                SELECT outbox_id, content
                FROM {ApplicationDbContext.DefaultSchema}.outbox_messages
                WHERE is_processed = false
                ORDER BY occurred_on_utc
                LIMIT {options.Value.BatchSize}
                FOR UPDATE
                """;

        IEnumerable<OutboxMessageResponse> messages = dbContext
            .Database
            .SqlQueryRaw<OutboxMessageResponse>(sql);

        return [.. messages];
    }

    private async Task UpdateOutboxMessageAsync(OutboxMessageResponse messageResponse, Exception? exception)
    {
        var messageError = exception is null ? string.Empty : exception.ToString();
        if (messageError.Length > ErrorMessageMaxLength)
        {
            messageError = messageError[..ErrorMessageMaxLength];
        }

        var processedOnUtc = new NpgsqlParameter("ProcessedOnUtc", dateTimeService.UtcNow);
        var error = new NpgsqlParameter("Error", messageError);
        var outboxId = new NpgsqlParameter("OutboxId", messageResponse.OutboxId);

        string sql = $"""
                UPDATE {ApplicationDbContext.DefaultSchema}.outbox_messages
                SET is_processed = true,
                    processed_on_utc = @ProcessedOnUtc,
                    error = @Error
                WHERE outbox_id = @OutboxId
                """;

        await dbContext
            .Database
            .ExecuteSqlRawAsync(sql, processedOnUtc, error, outboxId);
    }

    internal sealed record OutboxMessageResponse(Guid OutboxId, string Content);
}
