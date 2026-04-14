namespace Aurora.Flowboard.Application.Flows.RemoveState;

internal sealed class RemoveFlowStateHandler(
    IApplicationDbContext dbContext) : ICommandHandler<RemoveFlowStateCommand>
{
    public async Task<Result> Handle(
        RemoveFlowStateCommand command,
        CancellationToken cancellationToken)
    {
        Flow? flow = await dbContext
            .Flows
            .Include(f => f.Project)
            .Include(f => f.States)
            .Include(f => f.Transitions)
            .SingleOrDefaultAsync(f => f.Id == command.FlowId, cancellationToken);

        if (flow is null)
        {
            return Result.Fail(FlowErrors.NotFound);
        }

        Result result = flow.RemoveState(command.StateId);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
