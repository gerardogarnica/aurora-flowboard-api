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

        Result<User> changedByResult = await dbContext.GetCurrentUserAsync(userContext, cancellationToken);

        if (!changedByResult.IsSuccessful)
        {
            return Result.Fail(changedByResult.Error);
        }

        Result result = component.Rename(command.Name, changedByResult.Value, dateTimeProvider.UtcNow);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
