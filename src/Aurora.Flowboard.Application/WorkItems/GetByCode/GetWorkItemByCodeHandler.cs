namespace Aurora.Flowboard.Application.WorkItems.GetByCode;

internal sealed class GetWorkItemByCodeHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext) : IQueryHandler<GetWorkItemByCodeQuery, WorkItemResponse>
{
    public async Task<Result<WorkItemResponse>> Handle(
        GetWorkItemByCodeQuery query,
        CancellationToken cancellationToken)
    {
        var result = await dbContext
            .WorkItems
            .Where(w => w.Code == query.Code && w.Project.Members.Any(m => m.UserId == userContext.UserId))
            .Select(w => new
            {
                Response = new WorkItemResponse(
                    w.Id,
                    w.Code,
                    w.Title,
                    w.Description,
                    w.Type,
                    w.Priority,
                    w.ProjectId,
                    w.Project.Name,
                    w.FlowStateId,
                    w.FlowState.Name,
                    w.AssigneeId,
                    dbContext.Users
                        .Where(u => u.Id == w.AssigneeId)
                        .Select(u => u.FirstName + " " + u.LastName)
                        .FirstOrDefault(),
                    w.CreatedById,
                    dbContext.Users
                        .Where(u => u.Id == w.CreatedById)
                        .Select(u => u.FirstName + " " + u.LastName)
                        .FirstOrDefault() ?? string.Empty,
                    w.ComponentId,
                    w.Component != null ? w.Component.Name : null,
                    w.MilestoneId,
                    w.Milestone != null ? w.Milestone.Name : null,
                    w.EstimatedPoints,
                    w.EstimatedCompletionDate,
                    w.CreatedOnUtc,
                    w.UpdatedOnUtc,
                    w.CompletedOnUtc,
                    w.Tags
                        .OrderBy(t => t.Name)
                        .Select(t => new WorkItemTagResponse(t.Id, t.Name))
                        .ToList(),
                    Array.Empty<WorkItemFlowTransitionResponse>()),
                w.FlowStateId,
                MemberRole = w.Project.Members.First(m => m.UserId == userContext.UserId).Role
            })
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);

        if (result is null)
        {
            return Result.Fail<WorkItemResponse>(WorkItemErrors.NotFound);
        }

        List<FlowTransition> transitions = await dbContext
            .FlowTransitions
            .Where(t => t.FromStateId == result.FlowStateId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        List<Guid> toStateIds = [.. transitions.Select(t => t.ToStateId)];

        Dictionary<Guid, string> stateNames = await dbContext
            .FlowStates
            .Where(s => toStateIds.Contains(s.Id))
            .AsNoTracking()
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        List<WorkItemFlowTransitionResponse> availableTransitions = [.. transitions
            .Where(t => t.AllowedRoles.Contains(result.MemberRole))
            .OrderBy(t => stateNames.GetValueOrDefault(t.ToStateId, string.Empty))
            .Select(t => new WorkItemFlowTransitionResponse(t.ToStateId, stateNames.GetValueOrDefault(t.ToStateId, string.Empty)))];

        return result.Response with { AvailableTransitions = availableTransitions };
    }
}
