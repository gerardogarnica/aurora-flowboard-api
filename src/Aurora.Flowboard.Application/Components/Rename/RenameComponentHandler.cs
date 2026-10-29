namespace Aurora.Flowboard.Application.Components.Rename;

internal sealed class RenameComponentHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ComponentCommandHandler<RenameComponentCommand>(dbContext, dateTimeProvider, userContext)
{
    protected override Guid GetComponentId(RenameComponentCommand command) => command.ComponentId;

    // Project.Components backs the sibling name check; next to Project.Members that is two collections.
    protected override IQueryable<Component> Shape(IQueryable<Component> query) =>
        query
            .Include(c => c.Project)
            .ThenInclude(p => p.Components)
            .AsSplitQuery();

    protected override Task<Result> ApplyAsync(
        Component component,
        RenameComponentCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        Task.FromResult(component.Rename(command.Name, changedBy, utcNow));
}
