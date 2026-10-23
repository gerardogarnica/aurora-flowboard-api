namespace Aurora.Flowboard.Application.WorkItems.AddTag;

internal sealed class AddWorkItemTagHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : WorkItemFieldUpdateHandler<AddWorkItemTagCommand>(dbContext, dateTimeProvider, userContext)
{
    protected override Guid GetWorkItemId(AddWorkItemTagCommand command) => command.WorkItemId;

    protected override IQueryable<WorkItem> Shape(IQueryable<WorkItem> query) =>
        query.Include(w => w.Tags);

    protected override Task<Result> ApplyAsync(
        WorkItem workItem,
        AddWorkItemTagCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        Task.FromResult(workItem.AddTag(command.Name, changedBy, utcNow));
}
