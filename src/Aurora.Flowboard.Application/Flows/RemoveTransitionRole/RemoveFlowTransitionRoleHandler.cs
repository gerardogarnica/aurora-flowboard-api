namespace Aurora.Flowboard.Application.Flows.RemoveTransitionRole;

internal sealed class RemoveFlowTransitionRoleHandler(
    IApplicationDbContext dbContext) : ICommandHandler<RemoveFlowTransitionRoleCommand>
{
    public async Task<Result> Handle(
        RemoveFlowTransitionRoleCommand command,
        CancellationToken cancellationToken)
    {
        Flow? flow = await dbContext
            .Flows
            .Include(f => f.Transitions)
            .SingleOrDefaultAsync(f => f.Id == command.FlowId, cancellationToken);

        if (flow is null)
        {
            return Result.Fail(FlowErrors.NotFound);
        }

        Result result = flow.RemoveTransitionRole(command.TransitionId, command.Role);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
