namespace Aurora.Flowboard.Application.WorkItems.ChangeMilestone;

internal sealed class ChangeWorkItemMilestoneHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : WorkItemFieldUpdateHandler<ChangeWorkItemMilestoneCommand>(dbContext, dateTimeProvider, userContext)
{
    protected override Guid GetWorkItemId(ChangeWorkItemMilestoneCommand command) => command.Id;

    protected override async Task<Result> ApplyAsync(
        WorkItem workItem,
        ChangeWorkItemMilestoneCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        Milestone? milestone = null;

        if (command.MilestoneId is not null)
        {
            milestone = await DbContext
                .Milestones
                .SingleOrDefaultAsync(m => m.Id == command.MilestoneId, cancellationToken);

            if (milestone is null)
            {
                return Result.Fail(MilestoneErrors.NotFound);
            }
        }

        return workItem.ChangeMilestone(milestone, changedBy, utcNow);
    }
}
