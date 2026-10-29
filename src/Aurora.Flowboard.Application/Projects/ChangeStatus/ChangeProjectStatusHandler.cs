namespace Aurora.Flowboard.Application.Projects.ChangeStatus;

internal sealed class ChangeProjectStatusHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ProjectAdminCommandHandler<ChangeProjectStatusCommand>(dbContext, dateTimeProvider, userContext)
{
    protected override Guid GetProjectId(ChangeProjectStatusCommand command) => command.Id;

    protected override Task<Result> ApplyAsync(
        Project project,
        ChangeProjectStatusCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        Task.FromResult(project.ChangeStatus(command.NewStatus, changedBy, utcNow));
}
