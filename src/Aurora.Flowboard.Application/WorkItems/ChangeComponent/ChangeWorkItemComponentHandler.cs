namespace Aurora.Flowboard.Application.WorkItems.ChangeComponent;

internal sealed class ChangeWorkItemComponentHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : WorkItemFieldUpdateHandler<ChangeWorkItemComponentCommand>(dbContext, dateTimeProvider, userContext)
{
    protected override Guid GetWorkItemId(ChangeWorkItemComponentCommand command) => command.Id;

    protected override async Task<Result> ApplyAsync(
        WorkItem workItem,
        ChangeWorkItemComponentCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        Component? component = null;

        if (command.ComponentId is not null)
        {
            component = await DbContext
                .Components
                .SingleOrDefaultAsync(c => c.Id == command.ComponentId, cancellationToken);

            if (component is null)
            {
                return Result.Fail(ComponentErrors.NotFound);
            }
        }

        return workItem.ChangeComponent(component, changedBy, utcNow);
    }
}
