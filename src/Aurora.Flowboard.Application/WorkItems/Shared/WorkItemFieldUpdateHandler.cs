namespace Aurora.Flowboard.Application.WorkItems.Shared;

internal abstract class WorkItemFieldUpdateHandler<TCommand>(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    protected IApplicationDbContext DbContext => dbContext;

    // Updates that read another aggregate's state to decide (milestone, component status) race with the
    // handler that closes it; they return Serializable so Postgres (SSI) aborts one of the two requests.
    protected virtual IsolationLevel? TransactionIsolationLevel => null;

    public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
    {
        if (TransactionIsolationLevel is not { } isolationLevel)
        {
            return await UpdateAsync(command, cancellationToken);
        }

        await using IDbContextTransaction transaction =
            await dbContext.BeginTransactionAsync(isolationLevel, cancellationToken);

        Result result = await UpdateAsync(command, cancellationToken);

        if (result.IsSuccessful)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return result;
    }

    protected abstract Guid GetWorkItemId(TCommand command);

    protected abstract Task<Result> ApplyAsync(
        WorkItem workItem,
        TCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken);

    private async Task<Result> UpdateAsync(TCommand command, CancellationToken cancellationToken)
    {
        Guid workItemId = GetWorkItemId(command);

        WorkItem? workItem = await dbContext
            .WorkItems
            .Include(w => w.Project)
            .ThenInclude(p => p.Members)
            .AsSplitQuery()
            .SingleOrDefaultAsync(w => w.Id == workItemId, cancellationToken);

        if (workItem is null)
        {
            return Result.Fail(WorkItemErrors.NotFound);
        }

        User? changedBy = await dbContext
            .Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);

        if (changedBy is null)
        {
            return Result.Fail(UserErrors.NotFound);
        }

        Result result = await ApplyAsync(
            workItem,
            command,
            changedBy,
            dateTimeProvider.UtcNow,
            cancellationToken);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
