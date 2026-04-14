namespace Aurora.Flowboard.Application.Flows.Update;

internal sealed class UpdateFlowHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ICommandHandler<UpdateFlowCommand>
{
    public async Task<Result> Handle(
        UpdateFlowCommand command,
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

        Result result = flow.Update(
            command.Name,
            command.Description,
            changedBy,
            dateTimeProvider.UtcNow);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
