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
                    null,
                    null,
                    w.CreatedById,
                    string.Empty,
                    string.Empty,
                    w.ComponentId,
                    w.Component != null ? w.Component.Name : null,
                    w.MilestoneId,
                    w.Milestone != null ? w.Milestone.Name : null,
                    w.Milestone != null ? w.Milestone.Color.Value : null,
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
                AssigneeFirstName = dbContext.Users
                    .Where(u => u.Id == w.AssigneeId)
                    .Select(u => u.FirstName)
                    .FirstOrDefault(),
                AssigneeLastName = dbContext.Users
                    .Where(u => u.Id == w.AssigneeId)
                    .Select(u => u.LastName)
                    .FirstOrDefault(),
                CreatedByFirstName = dbContext.Users
                    .Where(u => u.Id == w.CreatedById)
                    .Select(u => u.FirstName)
                    .FirstOrDefault(),
                CreatedByLastName = dbContext.Users
                    .Where(u => u.Id == w.CreatedById)
                    .Select(u => u.LastName)
                    .FirstOrDefault(),
                w.FlowStateId,
                MemberRole = w.Project.Members.First(m => m.UserId == userContext.UserId).Role
            })
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);

        if (result is null)
        {
            return Result.Fail<WorkItemResponse>(WorkItemErrors.NotFound);
        }

        Guid projectId = result.Response.ProjectId;

        Dictionary<Guid, string> stateNames = await dbContext
            .FlowStates
            .Where(s => s.ProjectId == projectId)
            .AsNoTracking()
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        Dictionary<Guid, List<WorkItemFlowTransitionResponse>> transitionsByState = await dbContext.GetAvailableTransitionsByStateAsync(
            projectId,
            [result.FlowStateId],
            result.MemberRole,
            stateNames,
            cancellationToken);

        List<WorkItemFlowTransitionResponse> availableTransitions = transitionsByState.GetValueOrDefault(result.FlowStateId, []);

        // An unassigned item has no assignee name or initials; a missing creator falls back to an
        // empty name and the placeholder initials of User.FormatInitials.
        return result.Response with
        {
            AssigneeFullName = result.AssigneeFirstName is null || result.AssigneeLastName is null
                ? null
                : User.FormatFullName(result.AssigneeFirstName, result.AssigneeLastName),
            AssigneeInitials = result.AssigneeFirstName is null || result.AssigneeLastName is null
                ? null
                : User.FormatInitials(result.AssigneeFirstName, result.AssigneeLastName),
            CreatedByFullName = result.CreatedByFirstName is null || result.CreatedByLastName is null
                ? string.Empty
                : User.FormatFullName(result.CreatedByFirstName, result.CreatedByLastName),
            CreatedByInitials = User.FormatInitials(result.CreatedByFirstName, result.CreatedByLastName),
            AvailableTransitions = availableTransitions
        };
    }
}
