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

        return Result.Ok();
    }
}
