namespace Aurora.Flowboard.Application.Milestones.ChangeStatus;

internal sealed class ChangeMilestoneStatusHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ICommandHandler<ChangeMilestoneStatusCommand>
{
    public async Task<Result> Handle(
        ChangeMilestoneStatusCommand command,
        CancellationToken cancellationToken)
    {
        // Serializable (SSI): closing counts the open work items and writes the milestone, while
        // ChangeWorkItemMilestone and CreateWorkItem read the milestone status and write work_items.
        // Postgres aborts one of two concurrent requests with 40001 (PersistenceConflictBehavior → 409).
        await using IDbContextTransaction transaction =
            await dbContext.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        Milestone? milestone = await dbContext
            .Milestones
            .Include(m => m.Project)
            .ThenInclude(p => p.Members)
            .AsSplitQuery()
            .SingleOrDefaultAsync(m => m.Id == command.MilestoneId, cancellationToken);

        if (milestone is null)
        {
            return Result.Fail(MilestoneErrors.NotFound);
        }

        User? changedBy = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);

        if (changedBy is null)
        {
            return Result.Fail(UserErrors.NotFound);
        }

        int openWorkItemCount = await dbContext
            .WorkItems
            .Where(w => w.MilestoneId == milestone.Id
                && w.FlowState.Category != FlowStateCategory.Completed
                && w.FlowState.Category != FlowStateCategory.Cancelled)
            .CountAsync(cancellationToken);

        Result result = milestone.ChangeStatus(command.NewStatus, changedBy, openWorkItemCount, dateTimeProvider.UtcNow);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Ok();
    }
}
