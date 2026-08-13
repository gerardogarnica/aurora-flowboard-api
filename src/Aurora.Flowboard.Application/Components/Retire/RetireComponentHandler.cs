namespace Aurora.Flowboard.Application.Components.Retire;

internal sealed class RetireComponentHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ICommandHandler<RetireComponentCommand>
{
    public async Task<Result> Handle(
        RetireComponentCommand command,
        CancellationToken cancellationToken)
    {
        Component? component = await dbContext
            .Components
            .Include(c => c.Project)
            .ThenInclude(p => p.Members)
            .SingleOrDefaultAsync(c => c.Id == command.ComponentId, cancellationToken);

        if (component is null)
        {
            return Result.Fail(ComponentErrors.NotFound);
        }

        User? changedBy = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);

        if (changedBy is null)
        {
            return Result.Fail(UserErrors.NotFound);
        }

        int openWorkItemCount = await dbContext
            .WorkItems
            .Where(w => w.ComponentId == component.Id
                && w.FlowState.Category != FlowStateCategory.Completed
                && w.FlowState.Category != FlowStateCategory.Cancelled)
            .CountAsync(cancellationToken);

        Result result = component.Retire(changedBy, openWorkItemCount, dateTimeProvider.UtcNow);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
