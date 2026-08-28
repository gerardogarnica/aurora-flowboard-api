namespace Aurora.Flowboard.Application.Milestones.Update;

internal sealed class UpdateMilestoneHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ICommandHandler<UpdateMilestoneCommand>
{
    public async Task<Result> Handle(
        UpdateMilestoneCommand command,
        CancellationToken cancellationToken)
    {
        Milestone? milestone = await dbContext
            .Milestones
            .Include(m => m.Project)
            .ThenInclude(p => p.Members)
            .AsSplitQuery()
            .SingleOrDefaultAsync(m => m.Id == command.MilestoneId, cancellationToken);

        if (milestone is null)
        {
            return Result.Fail(MilestoneErrors.NotFound);
        }

        User? changedBy = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);

        if (changedBy is null)
        {
            return Result.Fail(UserErrors.NotFound);
        }

        Result result = milestone.Update(
            command.Name,
            command.Description,
            command.TargetStartDate,
            command.TargetEndDate,
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
