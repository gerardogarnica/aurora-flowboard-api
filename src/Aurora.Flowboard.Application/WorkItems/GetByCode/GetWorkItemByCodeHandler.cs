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
                    w.Comments
                        .Where(c => !c.IsDeleted)
                        .Select(c => new WorkItemCommentResponse(
                            c.Id,
                            c.AuthorId,
                            dbContext.Users
                                .Where(u => u.Id == c.AuthorId)
                                .Select(u => u.FirstName + " " + u.LastName)
                                .FirstOrDefault() ?? string.Empty,
                            c.Content,
                            c.CreatedOnUtc,
                            c.UpdatedOnUtc))
                        .ToList(),
                    w.TimeEntries
                        .Select(t => new WorkItemTimeEntryResponse(
                            t.Id,
                            t.UserId,
                            dbContext.Users
                                .Where(u => u.Id == t.UserId)
                                .Select(u => u.FirstName + " " + u.LastName)
                                .FirstOrDefault() ?? string.Empty,
                            t.Hours,
                            t.Description,
                            t.LoggedOnUtc))
                        .ToList(),
                    w.StateHistory
                        .OrderBy(s => s.ChangedOnUtc)
                        .Select(s => new WorkItemStateTransitionResponse(
                            s.Id,
                            s.FromStateId,
                            s.FromStateId != null
                                ? dbContext.FlowStates
                                    .Where(fs => fs.Id == s.FromStateId)
                                    .Select(fs => fs.Name)
                                    .FirstOrDefault()
                                : null,
                            s.ToStateId,
                            dbContext.FlowStates
                                .Where(fs => fs.Id == s.ToStateId)
                                .Select(fs => fs.Name)
                                .FirstOrDefault() ?? string.Empty,
                            s.ChangedById,
                            dbContext.Users
                                .Where(u => u.Id == s.ChangedById)
                                .Select(u => u.FirstName + " " + u.LastName)
                                .FirstOrDefault() ?? string.Empty,
                            s.Reason,
                            s.ChangedOnUtc))
                        .ToList(),
                    w.ChangeLogs
                        .OrderBy(c => c.ChangedOnUtc)
                        .Select(c => new WorkItemChangeLogResponse(
                            c.Id,
                            c.ChangedById,
                            dbContext.Users
                                .Where(u => u.Id == c.ChangedById)
                                .Select(u => u.FirstName + " " + u.LastName)
                                .FirstOrDefault() ?? string.Empty,
                            c.ChangeType,
                            c.AffectedEntityId,
                            c.ChangeType == WorkItemChangeType.Assigned
                                ? dbContext.Users
                                    .Where(u => u.Id == c.AffectedEntityId)
                                    .Select(u => u.FirstName + " " + u.LastName)
                                    .FirstOrDefault()
                                : c.ChangeType == WorkItemChangeType.Moved
                                    ? dbContext.FlowStates
                                        .Where(fs => fs.Id == c.AffectedEntityId)
                                        .Select(fs => fs.Name)
                                        .FirstOrDefault()
                                    : c.ChangeType == WorkItemChangeType.ComponentChanged
                                        ? dbContext.Components
                                            .Where(comp => comp.Id == c.AffectedEntityId)
                                            .Select(comp => comp.Name)
                                            .FirstOrDefault()
                                        : c.ChangeType == WorkItemChangeType.MilestoneChanged
                                            ? dbContext.Milestones
                                                .Where(m => m.Id == c.AffectedEntityId)
                                                .Select(m => m.Name)
                                                .FirstOrDefault()
                                            : null,
                            c.ChangedOnUtc))
                        .ToList(),
                    Array.Empty<WorkItemFlowTransitionResponse>()),
                w.FlowStateId,
                MemberRole = w.Project.Members.First(m => m.UserId == userContext.UserId).Role
            })
            .AsNoTracking()
            .AsSplitQuery()
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
