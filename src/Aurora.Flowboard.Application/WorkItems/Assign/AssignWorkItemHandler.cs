namespace Aurora.Flowboard.Application.WorkItems.Assign;

internal sealed class AssignWorkItemHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : WorkItemFieldUpdateHandler<AssignWorkItemCommand>(dbContext, dateTimeProvider, userContext)
{
    protected override Guid GetWorkItemId(AssignWorkItemCommand command) => command.Id;

    protected override IQueryable<WorkItem> Shape(IQueryable<WorkItem> query) =>
        query.Include(w => w.FlowState);

    protected override async Task<Result> ApplyAsync(
        WorkItem workItem,
        AssignWorkItemCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        User? assignee = await DbContext
            .Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == command.AssigneeId, cancellationToken);

        if (assignee is null)
        {
            return Result.Fail(WorkItemErrors.AssigneeNotFound);
        }

        return workItem.Assign(assignee, changedBy, utcNow);
    }
}
