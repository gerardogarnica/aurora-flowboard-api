namespace Aurora.Flowboard.Application.WorkItems.Create;

internal sealed class CreateWorkItemHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ICommandHandler<CreateWorkItemCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CreateWorkItemCommand command,
        CancellationToken cancellationToken)
    {
        await using IDbContextTransaction transaction =
            await dbContext.BeginTransactionAsync(cancellationToken);

        // Pessimistic row-level lock prevents duplicate sequence numbers under concurrency.
        // Table/column names must match the EF Core Infrastructure configuration.
        Project? project = await dbContext
            .Projects
            .FromSqlRaw("SELECT * FROM projects WHERE \"Id\" = {0} FOR UPDATE", command.ProjectId)
            .Include(p => p.Members)
            .SingleOrDefaultAsync(cancellationToken);

        if (project is null)
        {
            return Result.Fail<Guid>(ProjectErrors.NotFound);
        }

        Flow? flow = await dbContext
            .Flows
            .AsNoTracking()
            .Include(f => f.States)
            .AsSplitQuery()
            .SingleOrDefaultAsync(f => f.Id == command.FlowId && f.ProjectId == command.ProjectId, cancellationToken);

        if (flow is null)
        {
            return Result.Fail<Guid>(FlowErrors.NotFound);
        }

        User? createdBy = await dbContext
            .Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);

        if (createdBy is null)
        {
            return Result.Fail<Guid>(UserErrors.NotFound);
        }

        User? assignee = null;

        if (command.AssigneeId.HasValue)
        {
            assignee = await dbContext
                .Users
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.Id == command.AssigneeId.Value, cancellationToken);

            if (assignee is null)
            {
                return Result.Fail<Guid>(WorkItemErrors.AssigneeNotFound);
            }
        }

        Result<WorkItem> result = WorkItem.Create(
            command.Title,
            command.Description,
            command.Type,
            command.Priority,
            project,
            flow,
            createdBy,
            command.EstimatedPoints,
            command.EstimatedCompletionDate,
            dateTimeProvider.UtcNow,
            assignee);

        if (!result.IsSuccessful)
        {
            return Result.Fail<Guid>(result.Error);
        }

        WorkItem workItem = result.Value;

        dbContext.WorkItems.Add(workItem);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return workItem.Id;
    }
}
