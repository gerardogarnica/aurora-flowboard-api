namespace Aurora.Flowboard.Application.WorkItems.Shared;

internal abstract class WorkItemFieldUpdateHandler<TCommand>(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    protected IApplicationDbContext DbContext => dbContext;

    public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
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

    protected abstract Guid GetWorkItemId(TCommand command);

    protected abstract Task<Result> ApplyAsync(
        WorkItem workItem,
        TCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken);
}
