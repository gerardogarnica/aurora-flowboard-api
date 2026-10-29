namespace Aurora.Flowboard.Application.Projects.RemoveMember;

internal sealed class RemoveProjectMemberHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ProjectAdminCommandHandler<RemoveProjectMemberCommand>(dbContext, dateTimeProvider, userContext)
{
    protected override Guid GetProjectId(RemoveProjectMemberCommand command) => command.ProjectId;

    protected override async Task<Result> ApplyAsync(
        Project project,
        RemoveProjectMemberCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        bool userExists = await DbContext
            .Users
            .AnyAsync(u => u.Id == command.UserId, cancellationToken);

        if (!userExists)
        {
            return Result.Fail(UserErrors.NotFound);
        }

        return project.RemoveMember(command.UserId, changedBy, utcNow);
    }
}
