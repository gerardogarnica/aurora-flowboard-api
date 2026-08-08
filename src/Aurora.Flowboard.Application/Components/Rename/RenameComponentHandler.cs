namespace Aurora.Flowboard.Application.Components.Rename;

internal sealed class RenameComponentHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ICommandHandler<RenameComponentCommand>
{
    public async Task<Result> Handle(
        RenameComponentCommand command,
        CancellationToken cancellationToken)
    {
        Project? project = await dbContext
            .Projects
            .Include(p => p.Components)
            .SingleOrDefaultAsync(p => p.Components.Any(c => c.Id == command.ComponentId), cancellationToken);

        if (project is null)
        {
            return Result.Fail(ProjectErrors.ComponentNotFound);
        }

        User? changedBy = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);

        if (changedBy is null)
        {
            return Result.Fail(UserErrors.NotFound);
        }

        Result result = project.RenameComponent(command.ComponentId, command.Name, changedBy, dateTimeProvider.UtcNow);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
