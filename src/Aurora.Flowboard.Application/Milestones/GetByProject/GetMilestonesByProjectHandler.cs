namespace Aurora.Flowboard.Application.Milestones.GetByProject;

internal sealed class GetMilestonesByProjectHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext) : IQueryHandler<GetMilestonesByProjectQuery, IReadOnlyCollection<MilestoneResponse>>
{
    public async Task<Result<IReadOnlyCollection<MilestoneResponse>>> Handle(
        GetMilestonesByProjectQuery query,
        CancellationToken cancellationToken)
    {
        bool isMember = await dbContext.IsProjectMemberAsync(query.ProjectId, userContext.UserId, cancellationToken);

        if (!isMember)
        {
            return Result.Fail<IReadOnlyCollection<MilestoneResponse>>(ProjectErrors.NotFound);
        }

        List<MilestoneResponse> milestones = await dbContext
            .Milestones
            .AsNoTracking()
            .Where(m => m.ProjectId == query.ProjectId)
            .OrderBy(m => m.Name)
            .Select(m => new MilestoneResponse(
                m.Id,
                m.Name,
                m.Description,
                m.Status,
                m.TargetStartDate,
                m.TargetEndDate,
                m.CreatedBy,
                m.CreatedOnUtc,
                m.UpdatedOnUtc))
            .ToListAsync(cancellationToken);

        return milestones;
    }
}
