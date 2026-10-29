namespace Aurora.Flowboard.Application.Components.Shared;

internal abstract class ComponentCommandHandler<TCommand>(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ICommandHandler<TCommand> where TCommand : ICommand
{
    protected IApplicationDbContext DbContext => dbContext;

    // Override only for the write-skew cases in concurrency.md; the override documents its own race.
    protected virtual IsolationLevel? TransactionIsolationLevel => null;

    public Task<Result> Handle(TCommand command, CancellationToken cancellationToken) =>
        dbContext.ExecuteInTransactionAsync(
            TransactionIsolationLevel,
            ct => UpdateAsync(command, ct),
            cancellationToken);

    protected abstract Guid GetComponentId(TCommand command);

    // Adds the includes ApplyAsync needs beyond Project.Members; a second collection also needs AsSplitQuery().
    protected virtual IQueryable<Component> Shape(IQueryable<Component> query) => query;

    protected abstract Task<Result> ApplyAsync(
        Component component,
        TCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken);

    private async Task<Result> UpdateAsync(TCommand command, CancellationToken cancellationToken)
    {
        Guid componentId = GetComponentId(command);

        IQueryable<Component> query = dbContext
            .Components
            .Include(c => c.Project)
            .ThenInclude(p => p.Members);

        Component? component = await Shape(query)
            .SingleOrDefaultAsync(c => c.Id == componentId, cancellationToken);

        if (component is null)
        {
            return Result.Fail(ComponentErrors.NotFound);
        }

        Result<User> changedByResult = await dbContext.GetCurrentUserAsync(userContext, cancellationToken);

        if (!changedByResult.IsSuccessful)
        {
            return Result.Fail(changedByResult.Error);
        }

        Result result = await ApplyAsync(
            component,
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
}
