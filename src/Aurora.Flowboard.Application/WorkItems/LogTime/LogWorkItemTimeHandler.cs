namespace Aurora.Flowboard.Application.WorkItems.LogTime;

internal sealed class LogWorkItemTimeHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : WorkItemFieldUpdateHandler<LogWorkItemTimeCommand>(dbContext, dateTimeProvider, userContext)
{
    protected override Guid GetWorkItemId(LogWorkItemTimeCommand command) => command.WorkItemId;

    protected override Task<Result> ApplyAsync(
        WorkItem workItem,
        LogWorkItemTimeCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        Task.FromResult(workItem.LogTime(
            changedBy,
            command.Hours,
            command.Description,
            command.LoggedOnUtc,
            utcNow));
}
