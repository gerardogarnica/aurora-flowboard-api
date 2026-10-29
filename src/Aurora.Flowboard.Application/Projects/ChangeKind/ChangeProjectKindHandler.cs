namespace Aurora.Flowboard.Application.Projects.ChangeKind;

internal sealed class ChangeProjectKindHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ProjectAdminCommandHandler<ChangeProjectKindCommand>(dbContext, dateTimeProvider, userContext)
{
    protected override Guid GetProjectId(ChangeProjectKindCommand command) => command.Id;

    protected override Task<Result> ApplyAsync(
        Project project,
        ChangeProjectKindCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        Task.FromResult(project.ChangeKind(command.NewKind, changedBy, utcNow));
}
