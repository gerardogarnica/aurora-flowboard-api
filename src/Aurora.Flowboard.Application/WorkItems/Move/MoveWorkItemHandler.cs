namespace Aurora.Flowboard.Application.WorkItems.Move;

internal sealed class MoveWorkItemHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : WorkItemFieldUpdateHandler<MoveWorkItemCommand>(dbContext, dateTimeProvider, userContext)
{
    protected override Guid GetWorkItemId(MoveWorkItemCommand command) => command.Id;

    protected override IQueryable<WorkItem> Shape(IQueryable<WorkItem> query) =>
        query
            .Include(w => w.FlowState)
            .Include(w => w.Project)
            .ThenInclude(p => p.FlowTransitions);

    protected override async Task<Result> ApplyAsync(
        WorkItem workItem,
        MoveWorkItemCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        FlowState? toState = await DbContext
            .FlowStates
            .SingleOrDefaultAsync(s => s.Id == command.ToStateId, cancellationToken);

        if (toState is null)
        {
            return Result.Fail(ProjectErrors.FlowStateNotFound);
        }

        return workItem.Move(toState, changedBy, command.Reason, utcNow);
    }
}
