namespace Aurora.Flowboard.Application.WorkItems.Update;

internal sealed class UpdateWorkItemHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<UpdateWorkItemCommand>
{
    public async Task<Result> Handle(
        UpdateWorkItemCommand command,
        CancellationToken cancellationToken)
    {
        WorkItem? workItem = await dbContext
            .WorkItems
            .Include(w => w.Project)
            .ThenInclude(p => p.Members)
            .AsSplitQuery()
            .SingleOrDefaultAsync(w => w.Id == command.Id, cancellationToken);

        if (workItem is null)
        {
            return Result.Fail(WorkItemErrors.NotFound);
        }

        User? changedBy = await dbContext
            .Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == command.ChangedById, cancellationToken);

        if (changedBy is null)
        {
            return Result.Fail(UserErrors.NotFound);
        }

        Result result = workItem.Update(
            command.Title,
            command.Description,
            command.Priority,
            command.EstimatedPoints,
            command.EstimatedCompletionDate,
            changedBy,
            dateTimeProvider.UtcNow);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
