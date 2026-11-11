namespace Aurora.Flowboard.Application.Projects.ChangeStatus;

internal sealed class ChangeProjectStatusHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ProjectAdminCommandHandler<ChangeProjectStatusCommand>(dbContext, dateTimeProvider, userContext)
{
    protected override Guid GetProjectId(ChangeProjectStatusCommand command) => command.Id;

    // No Serializable transaction, unlike closing a milestone: an open item can only appear through
    // CreateWorkItem, which locks and writes this project row (FOR UPDATE, WorkItemCounter), so xmin turns
    // a concurrent create into a 409. A closed item cannot be reopened: no flow transition leaves a
    // terminal state (ProjectTests.Should_HaveNoTransitionOutOfATerminalState).
    protected override async Task<Result> ApplyAsync(
        Project project,
        ChangeProjectStatusCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        int openWorkItemCount = await DbContext
            .WorkItems
            .Where(w => w.ProjectId == project.Id)
            .CountOpenAsync(cancellationToken);

        return project.ChangeStatus(command.NewStatus, changedBy, openWorkItemCount, utcNow);
    }
}
