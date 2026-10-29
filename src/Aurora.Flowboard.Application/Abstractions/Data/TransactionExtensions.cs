namespace Aurora.Flowboard.Application.Abstractions.Data;

internal static class TransactionExtensions
{
    // A null isolation level runs the operation without an explicit transaction. Otherwise the
    // transaction is committed only when the operation succeeds; a failed Result rolls back on dispose.
    public static async Task<Result> ExecuteInTransactionAsync(
        this IApplicationDbContext dbContext,
        IsolationLevel? isolationLevel,
        Func<CancellationToken, Task<Result>> operation,
        CancellationToken cancellationToken)
    {
        if (isolationLevel is not { } level)
        {
            return await operation(cancellationToken);
        }

        await using IDbContextTransaction transaction = await dbContext.BeginTransactionAsync(level, cancellationToken);

        Result result = await operation(cancellationToken);

        if (result.IsSuccessful)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return result;
    }
}
