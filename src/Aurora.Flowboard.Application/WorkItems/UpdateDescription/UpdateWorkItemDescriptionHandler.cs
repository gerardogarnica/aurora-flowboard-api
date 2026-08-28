namespace Aurora.Flowboard.Application.WorkItems.UpdateDescription;

internal sealed class UpdateWorkItemDescriptionHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : WorkItemFieldUpdateHandler<UpdateWorkItemDescriptionCommand>(dbContext, dateTimeProvider, userContext)
{
    protected override Guid GetWorkItemId(UpdateWorkItemDescriptionCommand command) => command.Id;

    protected override Task<Result> ApplyAsync(
        WorkItem workItem,
        UpdateWorkItemDescriptionCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        Task.FromResult(workItem.UpdateDescription(command.Description, changedBy, utcNow));
}
