namespace Aurora.Flowboard.Application.Milestones.Create;

internal sealed class CreateMilestoneHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ICommandHandler<CreateMilestoneCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CreateMilestoneCommand command,
        CancellationToken cancellationToken)
    {
        Project? project = await dbContext
            .Projects
            .Include(p => p.Members)
            .Include(p => p.Milestones)
            .AsSplitQuery()
            .SingleOrDefaultAsync(p => p.Id == command.ProjectId, cancellationToken);

        if (project is null)
        {
            return Result.Fail<Guid>(ProjectErrors.NotFound);
        }

        Result<User> createdByResult = await dbContext.GetCurrentUserAsync(userContext, cancellationToken);

        if (!createdByResult.IsSuccessful)
        {
            return Result.Fail<Guid>(createdByResult.Error);
        }

        Result<Color> colorResult = Color.Create(command.Color);
        if (!colorResult.IsSuccessful)
        {
            return Result.Fail<Guid>(colorResult.Error);
        }

        Result<Milestone> result = Milestone.Create(
            command.Name,
            command.Description,
            colorResult.Value,
            command.TargetStartDate,
            command.TargetEndDate,
            project,
            createdByResult.Value,
            dateTimeProvider.UtcNow);

        if (!result.IsSuccessful)
        {
            return Result.Fail<Guid>(result.Error);
        }

        Milestone milestone = result.Value;

        dbContext.Milestones.Add(milestone);

        await dbContext.SaveChangesAsync(cancellationToken);

        return milestone.Id;
    }
}
