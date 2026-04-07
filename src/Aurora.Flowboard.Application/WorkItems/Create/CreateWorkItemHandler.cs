namespace Aurora.Flowboard.Application.WorkItems.Create;

internal sealed class CreateWorkItemHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CreateWorkItemCommand, Guid>
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
            .SingleOrDefaultAsync(cancellationToken);

        if (project is null)
        {
            return Result.Fail<Guid>(ProjectErrors.NotFound);
        }

        FlowState? flowState = await dbContext
            .FlowStates
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.Id == command.FlowStateId, cancellationToken);

        if (flowState is null)
        {
            return Result.Fail<Guid>(FlowErrors.StateNotFound);
        }

        User? createdBy = await dbContext
            .Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == command.CreatedById, cancellationToken);

        if (createdBy is null)
        {
            return Result.Fail<Guid>(UserErrors.NotFound);
        }

        int sequenceNumber = project.IncrementWorkItemCounter();
        string code = $"{project.Code}-{sequenceNumber}";

        Result<WorkItem> result = WorkItem.Create(
            command.Title,
            command.Description,
            command.Type,
            command.Priority,
            project,
            flowState,
            createdBy,
            code,
            sequenceNumber,
            command.EstimatedPoints,
            command.EstimatedCompletionDate,
            dateTimeProvider.UtcNow);

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
