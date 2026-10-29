namespace Aurora.Flowboard.Application.Components.Shared;

internal abstract class ComponentCommandHandler<TCommand>(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ICommandHandler<TCommand> where TCommand : ICommand
{
    protected IApplicationDbContext DbContext => dbContext;

    // Retiring a component reads its open work items while the work item handlers read its status;
    // it returns Serializable so Postgres (SSI) aborts one of the two requests.
    protected virtual IsolationLevel? TransactionIsolationLevel => null;

    public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
    {
        if (TransactionIsolationLevel is not { } isolationLevel)
        {
            return await UpdateAsync(command, cancellationToken);
        }

        await using IDbContextTransaction transaction =
            await dbContext.BeginTransactionAsync(isolationLevel, cancellationToken);

        Result result = await UpdateAsync(command, cancellationToken);

        if (result.IsSuccessful)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return result;
    }

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
