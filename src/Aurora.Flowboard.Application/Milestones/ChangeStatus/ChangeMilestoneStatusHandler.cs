namespace Aurora.Flowboard.Application.Milestones.ChangeStatus;

internal sealed class ChangeMilestoneStatusHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : MilestoneCommandHandler<ChangeMilestoneStatusCommand>(dbContext, dateTimeProvider, userContext)
{
    protected override Guid GetMilestoneId(ChangeMilestoneStatusCommand command) => command.MilestoneId;

    // Serializable (SSI): closing counts the open work items and writes the milestone, while
    // ChangeWorkItemMilestone and CreateWorkItem read the milestone status and write work_items.
    // Postgres aborts one of two concurrent requests with 40001 (PersistenceConflictBehavior → 409).
    protected override IsolationLevel? TransactionIsolationLevel => IsolationLevel.Serializable;

    protected override async Task<Result> ApplyAsync(
        Milestone milestone,
        ChangeMilestoneStatusCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        int openWorkItemCount = await DbContext
            .WorkItems
            .Where(w => w.MilestoneId == milestone.Id)
            .CountOpenAsync(cancellationToken);

        return milestone.ChangeStatus(command.NewStatus, changedBy, openWorkItemCount, utcNow);
    }
}
