namespace Aurora.Flowboard.Application.Projects.Shared;

internal abstract class ProjectAdminCommandHandler<TCommand>(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ICommandHandler<TCommand> where TCommand : ICommand
{
    protected IApplicationDbContext DbContext => dbContext;

    public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
    {
        Guid projectId = GetProjectId(command);

        Project? project = await dbContext
            .Projects
            .Include(p => p.Members)
            .SingleOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project is null)
        {
            return Result.Fail(ProjectErrors.NotFound);
        }

        Result<User> changedByResult = await dbContext.GetCurrentUserAsync(userContext, cancellationToken);

        if (!changedByResult.IsSuccessful)
        {
            return Result.Fail(changedByResult.Error);
        }

        Result result = await ApplyAsync(
            project,
            command,
            changedByResult.Value,
            dateTimeProvider.UtcNow,
            cancellationToken);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    protected abstract Guid GetProjectId(TCommand command);

    protected abstract Task<Result> ApplyAsync(
        Project project,
        TCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken);
}
