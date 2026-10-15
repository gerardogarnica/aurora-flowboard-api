namespace Aurora.Flowboard.Application.Projects.GetAll;

internal sealed class GetAllProjectsHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext) : IQueryHandler<GetAllProjectsQuery, IReadOnlyCollection<ProjectSummaryResponse>>
{
    public async Task<Result<IReadOnlyCollection<ProjectSummaryResponse>>> Handle(
        GetAllProjectsQuery query,
        CancellationToken cancellationToken)
    {
        // Counts are computed in SQL and members are the only collection projected.
        var rows = await dbContext
            .Projects
            .AsNoTracking()
            .Where(p => p.Members.Any(m => m.UserId == userContext.UserId))
            .OrderBy(p => p.Name)
            .Select(p => new
            {
                Project = p,
                OpenWorkItems = p.WorkItems.Count(wi => wi.FlowState.Category == FlowStateCategory.Active),
                ClosedWorkItems = p.WorkItems.Count(wi => wi.FlowState.Category == FlowStateCategory.Completed),
                Members = p.Members
                    .Select(m => new { m.UserId, m.User.FirstName, m.User.LastName, m.Role })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new ProjectSummaryResponse(
                row.Project.Id,
                row.Project.Name,
                row.Project.Description,
                row.Project.Prefix.Value,
                row.Project.Color,
                row.Project.Kind,
                row.Project.Status,
                row.OpenWorkItems,
                row.ClosedWorkItems,
                row.Project.CanModifyFlowStates(),
                row.Project.CanAddOrUpdateWorkItem(),
                [.. row.Members
                    .Select(m => new ProjectMemberSummaryResponse(
                        m.UserId,
                        User.FormatFullName(m.FirstName, m.LastName),
                        User.FormatInitials(m.FirstName, m.LastName),
                        m.Role))
                    .OrderBy(m => m.FullName)]))
            .ToList();
    }
}
