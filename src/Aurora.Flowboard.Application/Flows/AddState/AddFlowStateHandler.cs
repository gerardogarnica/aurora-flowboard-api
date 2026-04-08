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
            .Include(f => f.States)
            .SingleOrDefaultAsync(f => f.Id == command.FlowId, cancellationToken);

        if (flow is null)
        {
            return Result.Fail(FlowErrors.NotFound);
        }

        Result result = flow.AddState(command.Name, command.Category);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
