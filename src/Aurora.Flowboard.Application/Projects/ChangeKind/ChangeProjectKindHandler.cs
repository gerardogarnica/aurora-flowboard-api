namespace Aurora.Flowboard.Application.Projects.ChangeKind;

internal sealed class ChangeProjectKindHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ICommandHandler<ChangeProjectKindCommand>
{
    public async Task<Result> Handle(
        ChangeProjectKindCommand command,
        CancellationToken cancellationToken)
    {
        Project? project = await dbContext
            .Projects
            .Include(p => p.Members)
            .SingleOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

        if (project is null)
        {
            return Result.Fail(ProjectErrors.NotFound);
        }

        User? changedBy = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);

        if (changedBy is null)
        {
            return Result.Fail(UserErrors.NotFound);
        }

        Result result = project.ChangeKind(command.NewKind, changedBy, dateTimeProvider.UtcNow);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
