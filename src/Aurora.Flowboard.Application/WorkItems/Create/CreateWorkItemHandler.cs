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
        // Tracked (not AsNoTracking): IncrementWorkItemCounter mutates the project, and the initial
        // FlowState assigned to WorkItem.FlowState below must be tracked or EF inserts a duplicate.
        // Single query (no AsSplitQuery): splitting would re-execute the FOR UPDATE subquery per split.
        Project? project = await dbContext
            .Projects
            .FromSqlRaw("SELECT * FROM flowboard.projects WHERE id = {0} FOR UPDATE", command.ProjectId)
            .Include(p => p.Members)
            .Include(p => p.FlowStates)
            .SingleOrDefaultAsync(cancellationToken);

        if (project is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Fail<Guid>(ProjectErrors.NotFound);
        }

        User? createdBy = await dbContext
            .Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);

        if (createdBy is null)
        {
            await transaction.RollbackAsync(cancellationToken);
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
                await transaction.RollbackAsync(cancellationToken);
                return Result.Fail<Guid>(WorkItemErrors.AssigneeNotFound);
            }
        }

        Milestone? milestone = null;

        if (command.MilestoneId.HasValue)
        {
            // Tracked (not AsNoTracking): assigned to WorkItem.Milestone below, and an entity
            // attached to a navigation property must be tracked or EF treats it as a new insert.
            milestone = await dbContext
                .Milestones
                .SingleOrDefaultAsync(m => m.Id == command.MilestoneId.Value, cancellationToken);

            if (milestone is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result.Fail<Guid>(MilestoneErrors.NotFound);
            }
        }

        Component? component = null;

        if (command.ComponentId.HasValue)
        {
            component = await dbContext
                .Components
                .SingleOrDefaultAsync(c => c.Id == command.ComponentId.Value, cancellationToken);

            if (component is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result.Fail<Guid>(ComponentErrors.NotFound);
            }
        }

        Result<WorkItem> result = WorkItem.Create(
            command.Title,
            command.Description,
            command.Type,
            command.Priority,
            project,
            createdBy,
            command.EstimatedPoints,
            command.EstimatedCompletionDate,
            dateTimeProvider.UtcNow,
            assignee,
            milestone,
            component);

        if (!result.IsSuccessful)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Fail<Guid>(result.Error);
        }

        WorkItem workItem = result.Value;

        dbContext.WorkItems.Add(workItem);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return workItem.Id;
    }
}
