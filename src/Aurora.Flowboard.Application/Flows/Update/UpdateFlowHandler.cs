namespace Aurora.Flowboard.Application.Flows.Update;

internal sealed class UpdateFlowHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<UpdateFlowCommand>
{
    public async Task<Result> Handle(
        UpdateFlowCommand command,
        CancellationToken cancellationToken)
    {
        Flow? flow = await dbContext
            .Flows
            .SingleOrDefaultAsync(f => f.Id == command.Id, cancellationToken);

        if (flow is null)
        {
            return Result.Fail(FlowErrors.NotFound);
        }

        Result result = flow.Update(
            command.Name,
            command.Description,
            dateTimeProvider.UtcNow);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
