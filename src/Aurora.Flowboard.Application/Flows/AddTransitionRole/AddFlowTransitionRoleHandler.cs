namespace Aurora.Flowboard.Application.Flows.AddTransitionRole;

internal sealed class AddFlowTransitionRoleHandler(
    IApplicationDbContext dbContext) : ICommandHandler<AddFlowTransitionRoleCommand>
{
    public async Task<Result> Handle(
        AddFlowTransitionRoleCommand command,
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

        Result result = flow.AddTransitionRole(command.TransitionId, command.Role);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
