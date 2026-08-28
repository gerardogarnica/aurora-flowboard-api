namespace Aurora.Flowboard.Application.Projects.AddFlowTransitionRole;

internal sealed class AddFlowTransitionRoleHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext) : ICommandHandler<AddFlowTransitionRoleCommand>
{
    public async Task<Result> Handle(
        AddFlowTransitionRoleCommand command,
        CancellationToken cancellationToken)
    {
        Project? project = await dbContext
            .Projects
            .Include(p => p.Members)
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

        Result result = project.AddFlowTransitionRole(command.TransitionId, command.Role, changedBy);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
