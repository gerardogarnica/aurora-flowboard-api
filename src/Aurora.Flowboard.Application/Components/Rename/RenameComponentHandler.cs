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
        Component? component = await dbContext
            .Components
            .Include(c => c.Project)
            .ThenInclude(p => p.Members)
            .Include(c => c.Project)
            .ThenInclude(p => p.Components)
            .AsSplitQuery()
            .SingleOrDefaultAsync(c => c.Id == command.ComponentId, cancellationToken);

        if (component is null)
        {
            return Result.Fail(ComponentErrors.NotFound);
        }

        User? changedBy = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);

        if (changedBy is null)
        {
            return Result.Fail(UserErrors.NotFound);
        }

        Result result = component.Rename(command.Name, changedBy, dateTimeProvider.UtcNow);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
