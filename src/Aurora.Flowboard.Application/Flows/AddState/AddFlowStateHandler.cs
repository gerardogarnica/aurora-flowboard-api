namespace Aurora.Flowboard.Application.Flows.AddState;

internal sealed class AddFlowStateHandler(
    IApplicationDbContext dbContext) : ICommandHandler<AddFlowStateCommand>
{
    public async Task<Result> Handle(
        AddFlowStateCommand command,
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

        Result result = flow.AddState(command.Name, command.Category, command.AllowedRoles);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
