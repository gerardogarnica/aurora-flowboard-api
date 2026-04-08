namespace Aurora.Flowboard.Application.Flows.AddTransition;

internal sealed class AddFlowTransitionHandler(
    IApplicationDbContext dbContext) : ICommandHandler<AddFlowTransitionCommand>
{
    public async Task<Result> Handle(
        AddFlowTransitionCommand command,
        CancellationToken cancellationToken)
    {
        Flow? flow = await dbContext
            .Flows
            .Include(f => f.States)
            .Include(f => f.Transitions)
            .SingleOrDefaultAsync(f => f.Id == command.FlowId, cancellationToken);

        if (flow is null)
        {
            return Result.Fail(FlowErrors.NotFound);
        }

        FlowState? fromState = flow.States.SingleOrDefault(s => s.Id == command.FromStateId);

        if (fromState is null)
        {
            return Result.Fail(FlowErrors.TransitionFromStateNotFound);
        }

        FlowState? toState = flow.States.SingleOrDefault(s => s.Id == command.ToStateId);

        if (toState is null)
        {
            return Result.Fail(FlowErrors.TransitionToStateNotFound);
        }

        Result result = flow.AddTransition(fromState, toState, command.IsGlobal, command.AllowedRoles);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
