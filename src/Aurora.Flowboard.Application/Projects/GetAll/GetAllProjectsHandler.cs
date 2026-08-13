using Aurora.Flowboard.Application.Projects.GetFlow;

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
            .Include(p => p.FlowStates)
            .Include(p => p.Members).ThenInclude(m => m.User)
            .Include(p => p.WorkItems).ThenInclude(wi => wi.FlowState)
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
                p.Prefix.Value,
                p.Color,
                p.Kind,
                p.Status,
                p.WorkItems.Count(wi => wi.FlowState.Category == FlowStateCategory.Active),
                p.WorkItems.Count(wi => wi.FlowState.Category == FlowStateCategory.Completed),
                p.CanModifyFlowStates(),
                p.CanAddOrUpdateWorkItem(),
                [.. p.Members.OrderBy(m => m.User.FullName).Select(
                    m => new ProjectMemberSummaryResponse(
                        m.UserId,
                        m.User.FullName,
                        m.User.Initials,
                        m.Role))],
                [.. p.FlowStates
                    .OrderBy(s => s.Category)
                    .ThenBy(s => s.SortOrder)
                    .Select(s => new FlowStateResponse(
                        s.Id,
                        s.Name,
                        s.SortOrder,
                        s.Category,
                        s.Color.Value))]))
            .ToList();
    }
}
