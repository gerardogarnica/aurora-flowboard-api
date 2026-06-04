namespace Aurora.Flowboard.Application.Projects.GetAll;

internal sealed class GetAllProjectsHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext) : IQueryHandler<GetAllProjectsQuery, IReadOnlyCollection<ProjectSummaryResponse>>
{
    public async Task<Result<IReadOnlyCollection<ProjectSummaryResponse>>> Handle(
        GetAllProjectsQuery query,
        CancellationToken cancellationToken)
    {
        List<Project> projects = await dbContext
            .Projects
            .Include(p => p.Flows)
            .Include(p => p.Members).ThenInclude(m => m.User)
            .Where(p => p.Members.Any(m => m.UserId == userContext.UserId))
            .Where(p => query.StatusFilter == null || p.Status == query.StatusFilter)
            .OrderBy(p => p.Name)
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return projects
            .Select(p => new ProjectSummaryResponse(
                p.Id,
                p.Name,
                p.Description,
                p.Code,
                p.Color,
                p.EstimatedCompletionDate,
                p.Status,
                [.. p.Members.OrderBy(m => m.User.FullName).Select(m => new ProjectMemberSummaryResponse(m.UserId, m.User.FullName, m.User.Initials))],
                [.. p.Flows.Select(f => new ProjectFlowSummaryResponse(f.Id, f.Name, f.Description, f.IsDefault, f.IsActive)).OrderByDescending(f => f.IsDefault)]))
            .ToList();
    }
}
