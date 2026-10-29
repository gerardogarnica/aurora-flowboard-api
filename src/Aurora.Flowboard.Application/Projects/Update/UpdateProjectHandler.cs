namespace Aurora.Flowboard.Application.Projects.Update;

internal sealed class UpdateProjectHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ProjectAdminCommandHandler<UpdateProjectCommand>(dbContext, dateTimeProvider, userContext)
{
    protected override Guid GetProjectId(UpdateProjectCommand command) => command.Id;

    protected override Task<Result> ApplyAsync(
        Project project,
        UpdateProjectCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        Result<Color> colorResult = Color.Create(command.Color);
        if (!colorResult.IsSuccessful)
        {
            return Task.FromResult(Result.Fail(colorResult.Error));
        }

        return Task.FromResult(project.Update(
            command.Name,
            command.Description,
            colorResult.Value,
            changedBy,
            utcNow));
    }
}
