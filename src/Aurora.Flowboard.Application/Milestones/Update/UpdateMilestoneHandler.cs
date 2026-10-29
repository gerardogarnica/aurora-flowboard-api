namespace Aurora.Flowboard.Application.Milestones.Update;

internal sealed class UpdateMilestoneHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : MilestoneCommandHandler<UpdateMilestoneCommand>(dbContext, dateTimeProvider, userContext)
{
    protected override Guid GetMilestoneId(UpdateMilestoneCommand command) => command.MilestoneId;

    protected override Task<Result> ApplyAsync(
        Milestone milestone,
        UpdateMilestoneCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        Result<Color> colorResult = Color.Create(command.Color);
        if (!colorResult.IsSuccessful)
        {
            return Task.FromResult(Result.Fail(colorResult.Error));
        }

        return Task.FromResult(milestone.Update(
            command.Name,
            command.Description,
            colorResult.Value,
            command.TargetStartDate,
            command.TargetEndDate,
            changedBy,
            utcNow));
    }
}
