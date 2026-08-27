namespace Aurora.Flowboard.Application.WorkItems.UpdatePriority;

internal sealed class UpdateWorkItemPriorityHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : WorkItemFieldUpdateHandler<UpdateWorkItemPriorityCommand>(dbContext, dateTimeProvider, userContext)
{
    protected override Guid GetWorkItemId(UpdateWorkItemPriorityCommand command) => command.Id;

    protected override Task<Result> ApplyAsync(
        WorkItem workItem,
        UpdateWorkItemPriorityCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        Task.FromResult(workItem.UpdatePriority(command.Priority, changedBy, utcNow));
}
