namespace Aurora.Flowboard.Application.Projects.Setup;

internal sealed class SetupProjectHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ICommandHandler<SetupProjectCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        SetupProjectCommand command,
        CancellationToken cancellationToken)
    {
        await using IDbContextTransaction transaction =
            await dbContext.BeginTransactionAsync(cancellationToken);

        User? createdBy = await dbContext
            .Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);

        if (createdBy is null)
        {
            return Result.Fail<Guid>(UserErrors.NotFound);
        }

        Result<Project> projectResult = Project.Create(
            command.Name,
            command.Description,
            command.Code,
            command.EstimatedCompletionDate,
            createdBy,
            dateTimeProvider.UtcNow);

        if (!projectResult.IsSuccessful)
        {
            return Result.Fail<Guid>(projectResult.Error);
        }

        Project project = projectResult.Value;
        dbContext.Projects.Add(project);

        Result<Flow> flowResult = Flow.Create(
            command.Flow.Name,
            command.Flow.Description,
            project,
            true,
            dateTimeProvider.UtcNow);

        if (!flowResult.IsSuccessful)
        {
            return Result.Fail<Guid>(flowResult.Error);
        }

        Flow flow = flowResult.Value;
        dbContext.Flows.Add(flow);

        foreach (SetupProjectFlowStateDto state in command.Flow.States)
        {
            Result stateResult = flow.AddState(state.Name, state.Category, state.Roles);

            if (!stateResult.IsSuccessful)
            {
                return Result.Fail<Guid>(stateResult.Error);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return project.Id;
    }
}
