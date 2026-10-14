namespace Aurora.Flowboard.Application.Projects.Create;

internal sealed class CreateProjectHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ICommandHandler<CreateProjectCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CreateProjectCommand command,
        CancellationToken cancellationToken)
    {
        User? createdBy = await dbContext
            .Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);

        if (createdBy is null)
        {
            return Result.Fail<Guid>(UserErrors.NotFound);
        }

        Result<ProjectCode> prefixResult = ProjectCode.Create(command.Prefix);
        if (!prefixResult.IsSuccessful)
        {
            return Result.Fail<Guid>(prefixResult.Error);
        }

        bool prefixInUse = await dbContext
            .Projects
            .AnyAsync(p => p.Prefix.Value == prefixResult.Value.Value, cancellationToken);

        if (prefixInUse)
        {
            return Result.Fail<Guid>(ProjectErrors.PrefixAlreadyExists);
        }

        Result<Color> colorResult = Color.Create(command.Color);
        if (!colorResult.IsSuccessful)
        {
            return Result.Fail<Guid>(colorResult.Error);
        }

        List<FlowStateDefinition> flowStates = [];

        foreach (CreateProjectState state in command.FlowStates)
        {
            Result<Color> stateColorResult = Color.Create(state.Color);
            if (!stateColorResult.IsSuccessful)
            {
                return Result.Fail<Guid>(stateColorResult.Error);
            }

            Result<FlowStateDefinition> stateResult = FlowStateDefinition.Create(
                state.Name,
                state.Category,
                stateColorResult.Value,
                state.AllowedRoles);

            if (!stateResult.IsSuccessful)
            {
                return Result.Fail<Guid>(stateResult.Error);
            }

            flowStates.Add(stateResult.Value);
        }

        Result<Project> result = Project.Create(
            command.Name,
            command.Description,
            prefixResult.Value,
            command.Kind,
            colorResult.Value,
            flowStates,
            createdBy,
            dateTimeProvider.UtcNow);

        if (!result.IsSuccessful)
        {
            return Result.Fail<Guid>(result.Error);
        }

        Project project = result.Value;

        dbContext.Projects.Add(project);

        await dbContext.SaveChangesAsync(cancellationToken);

        return project.Id;
    }
}
