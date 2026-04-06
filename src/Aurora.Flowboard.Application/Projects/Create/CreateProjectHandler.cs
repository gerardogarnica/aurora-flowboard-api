namespace Aurora.Flowboard.Application.Projects.Create;

internal sealed class CreateProjectHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CreateProjectCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CreateProjectCommand command,
        CancellationToken cancellationToken)
    {
        List<Guid> userIds = [.. command.Members.Select(m => m.UserId)];

        List<User> users = await dbContext
            .Users
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        var members = new List<(User User, ProjectRole Role)>();

        foreach (ProjectMemberRequest memberRequest in command.Members)
        {
            User? user = users.SingleOrDefault(u => u.Id == memberRequest.UserId);

            if (user is null)
            {
                return Result.Fail<Guid>(UserErrors.NotFound);
            }

            members.Add((user, memberRequest.Role));
        }

        Result<Project> result = Project.Create(
            command.Name,
            command.Description,
            command.EstimatedCompletionDate,
            members,
            dateTimeProvider.UtcNow);

        if (!result.IsSuccessful)
        {
            return Result.Fail<Guid>(result.Error);
        }

        Project project = result.Value;

        dbContext.Projects.Add(project);

        await dbContext.SaveChangesAsync(cancellationToken);

        return project.Id;
    }
}
