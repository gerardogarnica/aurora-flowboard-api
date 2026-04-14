namespace Aurora.Flowboard.Application.Flows.AddState;

internal sealed class AddFlowStateHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext) : ICommandHandler<AddFlowStateCommand>
{
    public async Task<Result> Handle(
        AddFlowStateCommand command,
        CancellationToken cancellationToken)
    {
        Flow? flow = await dbContext
            .Flows
            .Include(f => f.Project).ThenInclude(p => p.Members)
            .Include(f => f.States)
            .Include(f => f.Transitions)
            .SingleOrDefaultAsync(f => f.Id == command.FlowId, cancellationToken);

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

        Result result = flow.AddState(command.Name, command.Category, command.AllowedRoles, changedBy);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
