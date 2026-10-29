namespace Aurora.Flowboard.Application.Milestones.Shared;

internal abstract class MilestoneCommandHandler<TCommand>(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ICommandHandler<TCommand> where TCommand : ICommand
{
    protected IApplicationDbContext DbContext => dbContext;

    // Override only for the write-skew cases in concurrency.md; the override documents its own race.
    protected virtual IsolationLevel? TransactionIsolationLevel => null;

    public Task<Result> Handle(TCommand command, CancellationToken cancellationToken) =>
        dbContext.ExecuteInTransactionAsync(
            TransactionIsolationLevel,
            ct => UpdateAsync(command, ct),
            cancellationToken);

    protected abstract Guid GetMilestoneId(TCommand command);

    // Adds the includes ApplyAsync needs beyond Project.Members; a second collection also needs AsSplitQuery().
    protected virtual IQueryable<Milestone> Shape(IQueryable<Milestone> query) => query;

    protected abstract Task<Result> ApplyAsync(
        Milestone milestone,
        TCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken);

    private async Task<Result> UpdateAsync(TCommand command, CancellationToken cancellationToken)
    {
        Guid milestoneId = GetMilestoneId(command);

        IQueryable<Milestone> query = dbContext
            .Milestones
            .Include(m => m.Project)
            .ThenInclude(p => p.Members);

        Milestone? milestone = await Shape(query)
            .SingleOrDefaultAsync(m => m.Id == milestoneId, cancellationToken);

        if (milestone is null)
        {
            return Result.Fail(MilestoneErrors.NotFound);
        }

        Result<User> changedByResult = await dbContext.GetCurrentUserAsync(userContext, cancellationToken);

        if (!changedByResult.IsSuccessful)
        {
            return Result.Fail(changedByResult.Error);
        }

        Result result = await ApplyAsync(
            milestone,
            command,
            changedByResult.Value,
            dateTimeProvider.UtcNow,
            cancellationToken);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
