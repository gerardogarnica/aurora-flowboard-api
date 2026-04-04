namespace Aurora.Flowboard.Application.Projects.Update;

internal sealed class UpdateProjectHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<UpdateProjectCommand>
{
    public async Task<Result> Handle(
        UpdateProjectCommand command,
        CancellationToken cancellationToken)
    {
        Project? project = await dbContext
            .Projects
            .SingleOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

        if (project is null)
        {
            return Result.Fail(ProjectErrors.NotFound);
        }

        Result result = project.Update(
            command.Name,
            command.Description,
            command.EstimatedCompletionDate,
            dateTimeProvider.UtcNow);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
