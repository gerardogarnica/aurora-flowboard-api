namespace Aurora.Flowboard.Application.WorkItems.Unassign;

internal sealed class UnassignWorkItemHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : WorkItemFieldUpdateHandler<UnassignWorkItemCommand>(dbContext, dateTimeProvider, userContext)
{
    protected override Guid GetWorkItemId(UnassignWorkItemCommand command) => command.Id;

    protected override IQueryable<WorkItem> Shape(IQueryable<WorkItem> query) =>
        query.Include(w => w.FlowState);

    protected override Task<Result> ApplyAsync(
        WorkItem workItem,
        UnassignWorkItemCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        Task.FromResult(workItem.Unassign(changedBy, utcNow));
}
