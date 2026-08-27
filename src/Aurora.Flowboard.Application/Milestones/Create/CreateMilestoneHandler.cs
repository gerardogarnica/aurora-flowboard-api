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

        User? createdBy = await dbContext
            .Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);

        if (createdBy is null)
        {
            return Result.Fail<Guid>(UserErrors.NotFound);
        }

        Result<Milestone> result = Milestone.Create(
            command.Name,
            command.Description,
            command.TargetStartDate,
            command.TargetEndDate,
            project,
            createdBy,
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
