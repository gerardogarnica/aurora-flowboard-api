namespace Aurora.Flowboard.Application.Flows.RemoveTransition;

internal sealed class RemoveFlowTransitionHandler(
    IApplicationDbContext dbContext) : ICommandHandler<RemoveFlowTransitionCommand>
{
    public async Task<Result> Handle(
        RemoveFlowTransitionCommand command,
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

        Result result = flow.RemoveTransition(command.TransitionId);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
