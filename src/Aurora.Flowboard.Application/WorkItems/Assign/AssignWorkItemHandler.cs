namespace Aurora.Flowboard.Application.WorkItems.Assign;

internal sealed class AssignWorkItemHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<AssignWorkItemCommand>
{
    public async Task<Result> Handle(
        AssignWorkItemCommand command,
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

        User? assignee = await dbContext
            .Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == command.AssigneeId, cancellationToken);

        if (assignee is null)
        {
            return Result.Fail(WorkItemErrors.AssigneeNotFound);
        }

        User? changedBy = await dbContext
            .Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == command.ChangedById, cancellationToken);

        if (changedBy is null)
        {
            return Result.Fail(UserErrors.NotFound);
        }

        Result result = workItem.Assign(assignee, changedBy, dateTimeProvider.UtcNow);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
