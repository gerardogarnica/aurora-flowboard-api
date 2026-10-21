namespace Aurora.Flowboard.Application.Components.Create;

internal sealed class CreateComponentHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ICommandHandler<CreateComponentCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CreateComponentCommand command,
        CancellationToken cancellationToken)
    {
        Project? project = await dbContext
            .Projects
            .Include(p => p.Members)
            .Include(p => p.Components)
            .AsSplitQuery()
            .SingleOrDefaultAsync(p => p.Id == command.ProjectId, cancellationToken);

        if (project is null)
        {
            return Result.Fail<Guid>(ProjectErrors.NotFound);
        }

        Result<User> createdByResult = await dbContext.GetCurrentUserAsync(userContext, cancellationToken);

        if (!createdByResult.IsSuccessful)
        {
            return Result.Fail<Guid>(createdByResult.Error);
        }

        Result<Component> result = Component.Create(command.Name, project, createdByResult.Value, dateTimeProvider.UtcNow);

        if (!result.IsSuccessful)
        {
            return Result.Fail<Guid>(result.Error);
        }

        Component component = result.Value;

        dbContext.Components.Add(component);

        await dbContext.SaveChangesAsync(cancellationToken);

        return component.Id;
    }
}
