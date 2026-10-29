namespace Aurora.Flowboard.Application.Components.Retire;

internal sealed class RetireComponentHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ComponentCommandHandler<RetireComponentCommand>(dbContext, dateTimeProvider, userContext)
{
    protected override Guid GetComponentId(RetireComponentCommand command) => command.ComponentId;

    // Serializable (SSI): retiring counts the open work items and writes the component, while
    // ChangeWorkItemComponent and CreateWorkItem read the component status and write work_items.
    // Postgres aborts one of two concurrent requests with 40001 (PersistenceConflictBehavior → 409).
    protected override IsolationLevel? TransactionIsolationLevel => IsolationLevel.Serializable;

    protected override async Task<Result> ApplyAsync(
        Component component,
        RetireComponentCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        int openWorkItemCount = await DbContext
            .WorkItems
            .Where(w => w.ComponentId == component.Id)
            .CountOpenAsync(cancellationToken);

        return component.Retire(changedBy, openWorkItemCount, utcNow);
    }
}
