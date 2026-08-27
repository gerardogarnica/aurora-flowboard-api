namespace Aurora.Flowboard.Application.WorkItems.UpdateEstimatedCompletionDate;

internal sealed class UpdateWorkItemEstimatedCompletionDateHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : WorkItemFieldUpdateHandler<UpdateWorkItemEstimatedCompletionDateCommand>(dbContext, dateTimeProvider, userContext)
{
    protected override Guid GetWorkItemId(UpdateWorkItemEstimatedCompletionDateCommand command) => command.Id;

    protected override Task<Result> ApplyAsync(
        WorkItem workItem,
        UpdateWorkItemEstimatedCompletionDateCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        Task.FromResult(workItem.UpdateEstimatedCompletionDate(command.EstimatedCompletionDate, changedBy, utcNow));
}
