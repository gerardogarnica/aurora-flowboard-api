namespace Aurora.Flowboard.Application.WorkItems.Move;

internal sealed class MoveWorkItemHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<MoveWorkItemCommand>
{
    public async Task<Result> Handle(
        MoveWorkItemCommand command,
        CancellationToken cancellationToken)
    {
        WorkItem? workItem = await dbContext
            .WorkItems
            .Include(w => w.FlowState)
            .SingleOrDefaultAsync(w => w.Id == command.Id, cancellationToken);

        if (workItem is null)
        {
            return Result.Fail(WorkItemErrors.NotFound);
        }

        FlowState? toState = await dbContext
            .FlowStates
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.Id == command.ToStateId, cancellationToken);

        if (toState is null)
        {
            return Result.Fail(FlowErrors.StateNotFound);
        }

        User? changedBy = await dbContext
            .Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == command.ChangedById, cancellationToken);

        if (changedBy is null)
        {
            return Result.Fail(UserErrors.NotFound);
        }

        Result result = workItem.Move(toState, changedBy, command.Reason, dateTimeProvider.UtcNow);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
