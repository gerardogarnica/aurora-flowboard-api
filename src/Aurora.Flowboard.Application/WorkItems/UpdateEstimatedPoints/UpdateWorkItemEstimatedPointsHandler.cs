namespace Aurora.Flowboard.Application.WorkItems.UpdateEstimatedPoints;

internal sealed class UpdateWorkItemEstimatedPointsHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : WorkItemFieldUpdateHandler<UpdateWorkItemEstimatedPointsCommand>(dbContext, dateTimeProvider, userContext)
{
    protected override Guid GetWorkItemId(UpdateWorkItemEstimatedPointsCommand command) => command.Id;

    protected override Task<Result> ApplyAsync(
        WorkItem workItem,
        UpdateWorkItemEstimatedPointsCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        Task.FromResult(workItem.UpdateEstimatedPoints(command.EstimatedPoints, changedBy, utcNow));
}
