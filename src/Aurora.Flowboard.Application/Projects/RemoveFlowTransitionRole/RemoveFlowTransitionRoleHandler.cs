namespace Aurora.Flowboard.Application.Projects.RemoveFlowTransitionRole;

internal sealed class RemoveFlowTransitionRoleHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext) : ICommandHandler<RemoveFlowTransitionRoleCommand>
{
    public async Task<Result> Handle(
        RemoveFlowTransitionRoleCommand command,
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

        Result result = project.RemoveFlowTransitionRole(command.TransitionId, command.Role, changedBy);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
