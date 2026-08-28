namespace Aurora.Flowboard.Application.Projects.AddFlowState;

internal sealed class AddFlowStateHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext) : ICommandHandler<AddFlowStateCommand>
{
    public async Task<Result> Handle(
        AddFlowStateCommand command,
        CancellationToken cancellationToken)
    {
        Project? project = await dbContext
            .Projects
            .Include(p => p.Members)
            .Include(p => p.FlowStates)
            .Include(p => p.FlowTransitions)
            .AsSplitQuery()
            .SingleOrDefaultAsync(p => p.Id == command.ProjectId, cancellationToken);

        if (project is null)
        {
            return Result.Fail(ProjectErrors.NotFound);
        }

        User? changedBy = await dbContext
            .Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);

        if (changedBy is null)
        {
            return Result.Fail(UserErrors.NotFound);
        }

        Result<Color> colorResult = Color.Create(command.Color);
        if (!colorResult.IsSuccessful)
        {
            return Result.Fail(colorResult.Error);
        }

        Result result = project.AddFlowState(command.Name, command.Category, colorResult.Value, command.AllowedRoles, changedBy);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
