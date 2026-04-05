namespace Aurora.Flowboard.Application.Flows.Deactivate;

internal sealed class DeactivateFlowHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<DeactivateFlowCommand>
{
    public async Task<Result> Handle(
        DeactivateFlowCommand command,
        CancellationToken cancellationToken)
    {
        Flow? flow = await dbContext
            .Flows
            .SingleOrDefaultAsync(f => f.Id == command.Id, cancellationToken);

        if (flow is null)
        {
            return Result.Fail(FlowErrors.NotFound);
        }

        Result result = flow.Deactivate(dateTimeProvider.UtcNow);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
