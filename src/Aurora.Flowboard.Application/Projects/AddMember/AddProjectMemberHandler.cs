namespace Aurora.Flowboard.Application.Projects.AddMember;

internal sealed class AddProjectMemberHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ProjectAdminCommandHandler<AddProjectMemberCommand>(dbContext, dateTimeProvider, userContext)
{
    protected override Guid GetProjectId(AddProjectMemberCommand command) => command.ProjectId;

    protected override async Task<Result> ApplyAsync(
        Project project,
        AddProjectMemberCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        User? user = await DbContext
            .Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Fail(UserErrors.NotFound);
        }

        return project.AddMember(user, command.Role, changedBy, utcNow);
    }
}
