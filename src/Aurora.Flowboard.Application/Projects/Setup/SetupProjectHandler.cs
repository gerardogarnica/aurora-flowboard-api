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

        Result<ProjectCode> projectPrefixResult = ProjectCode.Create(command.Prefix);
        if (!projectPrefixResult.IsSuccessful)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Fail<Guid>(projectPrefixResult.Error);
        }

        bool prefixInUse = await dbContext
            .Projects
            .AnyAsync(p => p.Prefix.Value == projectPrefixResult.Value.Value, cancellationToken);

        if (prefixInUse)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Fail<Guid>(ProjectErrors.PrefixAlreadyExists);
        }

        Result<Project> projectResult = Project.Create(
            command.Name,
            command.Description,
            projectPrefixResult.Value,
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

        foreach (SetupProjectFlowStateDto state in command.FlowStates)
        {
            Result<Color> stateColorResult = Color.Create(state.Color);
            if (!stateColorResult.IsSuccessful)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result.Fail<Guid>(stateColorResult.Error);
            }

            Result stateResult = project.AddFlowState(state.Name, state.Category, stateColorResult.Value, state.Roles, createdBy);

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
