namespace Aurora.Flowboard.Application.Projects.GetFlow;

internal sealed class GetProjectFlowHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext) : IQueryHandler<GetProjectFlowQuery, ProjectFlowResponse>
{
    public async Task<Result<ProjectFlowResponse>> Handle(
        GetProjectFlowQuery query,
        CancellationToken cancellationToken)
    {
        Project? project = await dbContext
            .Projects
            .Include(p => p.Members)
            .Include(p => p.FlowStates)
            .Include(p => p.FlowTransitions)
            .AsNoTracking()
            .AsSplitQuery()
            .SingleOrDefaultAsync(p => p.Id == query.ProjectId, cancellationToken);

        if (project is null || !project.Members.Any(m => m.UserId == userContext.UserId))
        {
            return Result.Fail<ProjectFlowResponse>(ProjectErrors.NotFound);
        }

        Dictionary<Guid, string> stateNames = project.FlowStates.ToDictionary(s => s.Id, s => s.Name);

        return new ProjectFlowResponse(
            project.Id,
            [.. project.FlowStates
                .OrderBy(s => s.Category)
                .ThenBy(s => s.SortOrder)
                .Select(s => new FlowStateResponse(s.Id, s.Name, s.SortOrder, s.Category, s.Color.Value))],
            [.. project.FlowTransitions
                .Select(t => new FlowTransitionResponse(
                    t.Id,
                    t.FromStateId,
                    stateNames.GetValueOrDefault(t.FromStateId, string.Empty),
                    t.ToStateId,
                    stateNames.GetValueOrDefault(t.ToStateId, string.Empty),
                    t.AllowedRoles))]);
    }
}
