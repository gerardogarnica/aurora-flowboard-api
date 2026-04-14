namespace Aurora.Flowboard.Application.Flows.Deactivate;

internal sealed class DeactivateFlowHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ICommandHandler<DeactivateFlowCommand>
{
    public async Task<Result> Handle(
        DeactivateFlowCommand command,
        CancellationToken cancellationToken)
    {
        Flow? flow = await dbContext
            .Flows
            .Include(f => f.Project).ThenInclude(p => p.Members)
            .SingleOrDefaultAsync(f => f.Id == command.Id, cancellationToken);

        if (flow is null)
        {
            return Result.Fail(FlowErrors.NotFound);
        }

        User? changedBy = await dbContext
            .Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);

        if (changedBy is null)
        {
            return Result.Fail(UserErrors.NotFound);
        }

        Result result = flow.Deactivate(changedBy, dateTimeProvider.UtcNow);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
