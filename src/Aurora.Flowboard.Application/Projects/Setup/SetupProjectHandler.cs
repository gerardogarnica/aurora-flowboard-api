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
            await transaction.RollbackAsync(cancellationToken);
            return Result.Fail<Guid>(UserErrors.NotFound);
        }

        Result<Color> projectColorResult = Color.Create(command.Color);
        if (!projectColorResult.IsSuccessful)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Fail<Guid>(projectColorResult.Error);
        }

        Result<ProjectCode> projectCodeResult = ProjectCode.Create(command.Code);
        if (!projectCodeResult.IsSuccessful)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Fail<Guid>(projectCodeResult.Error);
        }

        bool codeInUse = await dbContext
            .Projects
            .AnyAsync(p => p.Code == projectCodeResult.Value.Value, cancellationToken);

        if (codeInUse)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Fail<Guid>(ProjectErrors.CodeAlreadyExists);
        }

        Result<Project> projectResult = Project.Create(
            command.Name,
            command.Description,
            command.Code,
            command.Kind,
            projectColorResult.Value,
            createdBy,
            dateTimeProvider.UtcNow);

        if (!projectResult.IsSuccessful)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Fail<Guid>(projectResult.Error);
        }

        Project project = projectResult.Value;
        dbContext.Projects.Add(project);

        Result<Flow> flowResult = Flow.Create(
            command.Flow.Name,
            command.Flow.Description,
            project,
            true,
            createdBy,
            dateTimeProvider.UtcNow);

        if (!flowResult.IsSuccessful)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Fail<Guid>(flowResult.Error);
        }

        Flow flow = flowResult.Value;
        dbContext.Flows.Add(flow);

        foreach (SetupProjectFlowStateDto state in command.Flow.States)
        {
            Result<Color> stateColorResult = Color.Create(state.Color);
            if (!stateColorResult.IsSuccessful)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result.Fail<Guid>(stateColorResult.Error);
            }

            Result stateResult = flow.AddState(state.Name, state.Category, stateColorResult.Value, state.Roles, createdBy);

            if (!stateResult.IsSuccessful)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result.Fail<Guid>(stateResult.Error);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return project.Id;
    }
}
