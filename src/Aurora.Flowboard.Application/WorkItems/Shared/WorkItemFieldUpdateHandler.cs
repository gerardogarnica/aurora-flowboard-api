namespace Aurora.Flowboard.Application.WorkItems.Shared;

internal abstract class WorkItemFieldUpdateHandler<TCommand>(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    protected IApplicationDbContext DbContext => dbContext;

    // Override only for the write-skew cases in concurrency.md; the override documents its own race.
    protected virtual IsolationLevel? TransactionIsolationLevel => null;

    public Task<Result> Handle(TCommand command, CancellationToken cancellationToken) =>
        dbContext.ExecuteInTransactionAsync(
            TransactionIsolationLevel,
            ct => UpdateAsync(command, ct),
            cancellationToken);

    protected abstract Guid GetWorkItemId(TCommand command);

    // Adds the includes ApplyAsync needs beyond Project.Members (tags, comments, flow state...).
    protected virtual IQueryable<WorkItem> Shape(IQueryable<WorkItem> query) => query;

    protected abstract Task<Result> ApplyAsync(
        WorkItem workItem,
        TCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken);

    private async Task<Result> UpdateAsync(TCommand command, CancellationToken cancellationToken)
    {
        Guid workItemId = GetWorkItemId(command);

        IQueryable<WorkItem> query = dbContext
            .WorkItems
            .Include(w => w.Project)
            .ThenInclude(p => p.Members);

        WorkItem? workItem = await Shape(query)
            .AsSplitQuery()
            .SingleOrDefaultAsync(w => w.Id == workItemId, cancellationToken);

        if (workItem is null)
        {
            return Result.Fail(WorkItemErrors.NotFound);
        }

        Result<User> changedByResult = await dbContext.GetCurrentUserAsync(userContext, cancellationToken);

        if (!changedByResult.IsSuccessful)
        {
            return Result.Fail(changedByResult.Error);
        }

        Result result = await ApplyAsync(
            workItem,
            command,
            changedByResult.Value,
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
