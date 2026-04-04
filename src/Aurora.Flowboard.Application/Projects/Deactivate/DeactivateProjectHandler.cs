namespace Aurora.Flowboard.Application.Projects.Deactivate;

internal sealed class DeactivateProjectHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<DeactivateProjectCommand>
{
    public async Task<Result> Handle(
        DeactivateProjectCommand command,
        CancellationToken cancellationToken)
    {
        Project? project = await dbContext
            .Projects
            .SingleOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

        if (project is null)
        {
            return Result.Fail(ProjectErrors.NotFound);
        }

        Result result = project.Deactivate(dateTimeProvider.UtcNow);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
