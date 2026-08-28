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

        if (command.FlowStates.Count > 0)
        {
            if (!command.FlowStates.Any(s => s.Category == FlowStateCategory.Active))
            {
                return Result.Fail<Guid>(ProjectErrors.InitialFlowStatesMissingActiveState);
            }

            if (!command.FlowStates.Any(s => s.Category == FlowStateCategory.Completed))
            {
                return Result.Fail<Guid>(ProjectErrors.InitialFlowStatesMissingCompletedState);
            }

            if (!command.FlowStates.Any(s => s.Category == FlowStateCategory.Cancelled))
            {
                return Result.Fail<Guid>(ProjectErrors.InitialFlowStatesMissingCancelledState);
            }
        }

        Result<Project> result = Project.Create(
            command.Name,
            command.Description,
            prefixResult.Value,
            command.Kind,
            colorResult.Value,
            createdBy,
            dateTimeProvider.UtcNow);

        if (!result.IsSuccessful)
        {
            return Result.Fail<Guid>(result.Error);
        }

        Project project = result.Value;

        IEnumerable<CreateProjectState> orderedFlowStates = command.FlowStates
            .OrderBy(s => s.Category switch
            {
                FlowStateCategory.Active => 0,
                FlowStateCategory.Completed => 1,
                FlowStateCategory.Cancelled => 2,
                _ => 3
            });

        foreach (CreateProjectState state in orderedFlowStates)
        {
            Result<Color> stateColorResult = Color.Create(state.Color);
            if (!stateColorResult.IsSuccessful)
            {
                return Result.Fail<Guid>(stateColorResult.Error);
            }

            Result addStateResult = project.AddFlowState(
                state.Name,
                state.Category,
                stateColorResult.Value,
                state.AllowedRoles,
                createdBy);

            if (!addStateResult.IsSuccessful)
            {
                return Result.Fail<Guid>(addStateResult.Error);
            }
        }

        dbContext.Projects.Add(project);

        await dbContext.SaveChangesAsync(cancellationToken);

        return project.Id;
    }
}
