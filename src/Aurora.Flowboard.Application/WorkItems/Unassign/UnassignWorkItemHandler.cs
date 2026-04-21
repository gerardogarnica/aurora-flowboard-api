namespace Aurora.Flowboard.Application.WorkItems.Unassign;

internal sealed class UnassignWorkItemHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<UnassignWorkItemCommand>
{
    public async Task<Result> Handle(
        UnassignWorkItemCommand command,
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

        Result result = workItem.Unassign(changedBy, dateTimeProvider.UtcNow);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
