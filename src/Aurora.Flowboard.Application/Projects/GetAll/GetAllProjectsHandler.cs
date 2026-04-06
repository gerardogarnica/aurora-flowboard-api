namespace Aurora.Flowboard.Application.Projects.GetAll;

internal sealed class GetAllProjectsHandler(
    IApplicationDbContext dbContext) : IQueryHandler<GetAllProjectsQuery, IReadOnlyCollection<ProjectSummaryResponse>>
{
    public async Task<Result<IReadOnlyCollection<ProjectSummaryResponse>>> Handle(
        GetAllProjectsQuery query,
        CancellationToken cancellationToken)
    {
        List<ProjectSummaryResponse> projects = await dbContext
            .Projects
            .AsNoTracking()
            .Where(p => query.StatusFilter == null || p.Status == query.StatusFilter)
            .OrderBy(p => p.Name)
            .Select(p => new ProjectSummaryResponse(
                p.Id,
                p.Name,
                p.Description,
                p.EstimatedCompletionDate,
                p.Status,
                p.Members.Count))
            .ToListAsync(cancellationToken);

        return projects;
    }
}
