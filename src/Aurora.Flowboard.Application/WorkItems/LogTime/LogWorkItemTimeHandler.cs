namespace Aurora.Flowboard.Application.WorkItems.LogTime;

internal sealed class LogWorkItemTimeHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<LogWorkItemTimeCommand>
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

        User? user = await dbContext
            .Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Fail(UserErrors.NotFound);
        }

        Result result = workItem.LogTime(
            user,
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
