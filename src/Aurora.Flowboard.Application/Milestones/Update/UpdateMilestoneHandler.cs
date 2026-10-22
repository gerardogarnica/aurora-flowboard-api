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

        Result<User> changedByResult = await dbContext.GetCurrentUserAsync(userContext, cancellationToken);

        if (!changedByResult.IsSuccessful)
        {
            return Result.Fail(changedByResult.Error);
        }

        Result<Color> colorResult = Color.Create(command.Color);
        if (!colorResult.IsSuccessful)
        {
            return Result.Fail(colorResult.Error);
        }

        Result result = milestone.Update(
            command.Name,
            command.Description,
            colorResult.Value,
            command.TargetStartDate,
            command.TargetEndDate,
            changedByResult.Value,
            dateTimeProvider.UtcNow);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
