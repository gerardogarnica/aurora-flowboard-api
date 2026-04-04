namespace Aurora.Flowboard.Application.Projects.RemoveMember;

internal sealed class RemoveProjectMemberHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<RemoveProjectMemberCommand>
{
    public async Task<Result> Handle(
        RemoveProjectMemberCommand command,
        CancellationToken cancellationToken)
    {
        Project? project = await dbContext
            .Projects
            .Include(p => p.Members)
            .SingleOrDefaultAsync(p => p.Id == command.ProjectId, cancellationToken);

        if (project is null)
        {
            return Result.Fail(ProjectErrors.NotFound);
        }

        bool userExists = await dbContext
            .Users
            .AnyAsync(u => u.Id == command.UserId, cancellationToken);

        if (!userExists)
        {
            return Result.Fail(UserErrors.NotFound);
        }

        Result result = project.RemoveMember(command.UserId, dateTimeProvider.UtcNow);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
