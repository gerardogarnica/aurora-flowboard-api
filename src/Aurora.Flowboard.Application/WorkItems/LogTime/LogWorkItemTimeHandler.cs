namespace Aurora.Flowboard.Application.WorkItems.LogTime;

internal sealed class LogWorkItemTimeHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ICommandHandler<LogWorkItemTimeCommand>
{
    public async Task<Result> Handle(
        LogWorkItemTimeCommand command,
        CancellationToken cancellationToken)
    {
        WorkItem? workItem = await dbContext
            .WorkItems
            .Include(w => w.Project)
            .ThenInclude(p => p.Members)
            .AsSplitQuery()
            .SingleOrDefaultAsync(w => w.Id == command.WorkItemId, cancellationToken);

        if (workItem is null)
        {
            return Result.Fail(WorkItemErrors.NotFound);
        }

        Result<User> userResult = await dbContext.GetCurrentUserAsync(userContext, cancellationToken);

        if (!userResult.IsSuccessful)
        {
            return Result.Fail(userResult.Error);
        }

        Result result = workItem.LogTime(
            userResult.Value,
            command.Hours,
            command.Description,
            command.LoggedOnUtc,
            dateTimeProvider.UtcNow);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
