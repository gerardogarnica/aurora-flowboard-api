namespace Aurora.Flowboard.Application.Projects.ChangeStatus;

internal sealed class ChangeProjectStatusHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<ChangeProjectStatusCommand>
{
    public async Task<Result> Handle(
        ChangeProjectStatusCommand command,
        CancellationToken cancellationToken)
    {
        Project? project = await dbContext
            .Projects
            .SingleOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

        if (project is null)
        {
            return Result.Fail(ProjectErrors.NotFound);
        }

        Result result = project.ChangeStatus(command.NewStatus, dateTimeProvider.UtcNow);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
