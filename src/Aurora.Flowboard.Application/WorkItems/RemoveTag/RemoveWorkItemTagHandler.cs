namespace Aurora.Flowboard.Application.WorkItems.RemoveTag;

internal sealed class RemoveWorkItemTagHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : WorkItemFieldUpdateHandler<RemoveWorkItemTagCommand>(dbContext, dateTimeProvider, userContext)
{
    protected override Guid GetWorkItemId(RemoveWorkItemTagCommand command) => command.WorkItemId;

    protected override IQueryable<WorkItem> Shape(IQueryable<WorkItem> query) =>
        query.Include(w => w.Tags);

    protected override Task<Result> ApplyAsync(
        WorkItem workItem,
        RemoveWorkItemTagCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        Task.FromResult(workItem.RemoveTag(command.TagId, changedBy, utcNow));
}
