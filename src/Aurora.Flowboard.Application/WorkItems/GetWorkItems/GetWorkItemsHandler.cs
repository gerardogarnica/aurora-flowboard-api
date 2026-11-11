namespace Aurora.Flowboard.Application.WorkItems.GetWorkItems;

internal sealed class GetWorkItemsHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider) : IQueryHandler<GetWorkItemsQuery, PagedResponse<WorkItemListItemResponse>>
{
    public async Task<Result<PagedResponse<WorkItemListItemResponse>>> Handle(
        GetWorkItemsQuery query,
        CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        // The Active states of my active projects fix the scope on their own: open items only,
        // and never from a Completed, Archived or foreign project.
        List<ActiveStateRow> activeStates = await dbContext
            .Projects
            .AsNoTracking()
            .WhereMyActiveProjects(userId)
            .SelectMany(p => p.FlowStates)
            .Where(s => s.Category == FlowStateCategory.Active)
            .Select(s => new ActiveStateRow(s.Id, s.ProjectId, s.SortOrder))
            .ToListAsync(cancellationToken);

        if (activeStates.Count == 0)
        {
            return new PagedResponse<WorkItemListItemResponse>([], query.Page, query.PageSize, 0);
        }

        HashSet<Guid> backlogStateIds = FlowStateStageResolver.GetBacklogStateIds(
            activeStates.Select(s => (s.Id, s.ProjectId, s.SortOrder)));
        List<Guid> openStateIds = [.. activeStates.Select(s => s.Id)];
        List<Guid> backlogIds = [.. backlogStateIds];
        List<Guid> inFlightStateIds = [.. openStateIds.Where(id => !backlogStateIds.Contains(id))];

        IQueryable<WorkItem> workItems = ApplyFilters(
            dbContext.WorkItems.AsNoTracking().Where(w => openStateIds.Contains(w.FlowStateId)),
            query,
            userId,
            backlogIds,
            inFlightStateIds);

        // In flight first (false sorts before true), then the longest time in state, then Id for stable pages.
        var rows = await workItems
            .OrderBy(w => backlogIds.Contains(w.FlowStateId))
            .ThenBy(w => w.EnteredCurrentStateOnUtc)
            .ThenBy(w => w.Id)
            .ToPagedResponseAsync(
                query,
                w => new
                {
                    w.Id,
                    w.Code,
                    w.Title,
                    w.Type,
                    w.Priority,
                    w.ProjectId,
                    ProjectName = w.Project.Name,
                    ProjectColor = w.Project.Color.Value,
                    w.FlowStateId,
                    FlowStateName = w.FlowState.Name,
                    w.EnteredCurrentStateOnUtc,
                    w.AssigneeId,
                    AssigneeFirstName = dbContext.Users
                        .Where(u => u.Id == w.AssigneeId)
                        .Select(u => u.FirstName)
                        .FirstOrDefault(),
                    AssigneeLastName = dbContext.Users
                        .Where(u => u.Id == w.AssigneeId)
                        .Select(u => u.LastName)
                        .FirstOrDefault(),
                    MilestoneName = w.Milestone != null ? w.Milestone.Name : null,
                    MilestoneColor = w.Milestone != null ? w.Milestone.Color.Value : null
                },
                cancellationToken);

        return rows.Map(w => new WorkItemListItemResponse(
            w.Id,
            w.Code,
            w.Title,
            w.Type,
            w.Priority,
            w.ProjectId,
            w.ProjectName,
            w.ProjectColor,
            w.FlowStateId,
            w.FlowStateName,
            backlogStateIds.Contains(w.FlowStateId) ? FlowStateStage.Backlog : FlowStateStage.InFlight,
            w.EnteredCurrentStateOnUtc,
            w.AssigneeId,
            w.AssigneeFirstName is null || w.AssigneeLastName is null
                ? null
                : User.FormatInitials(w.AssigneeFirstName, w.AssigneeLastName),
            w.AssigneeFirstName is null || w.AssigneeLastName is null
                ? null
                : User.FormatFullName(w.AssigneeFirstName, w.AssigneeLastName),
            w.MilestoneName,
            w.MilestoneColor));
    }

    // Filters combine with AND. GetWorkItemsValidator only lets "me" and "true" through, so a present
    // value means the filter is on. Sort is validated but has a single value, applied by Handle.
    private IQueryable<WorkItem> ApplyFilters(
        IQueryable<WorkItem> source,
        GetWorkItemsQuery query,
        Guid userId,
        List<Guid> backlogIds,
        List<Guid> inFlightStateIds)
    {
        IQueryable<WorkItem> filtered = source;

        if (query.Assignee is not null)
        {
            filtered = filtered.Where(w => w.AssigneeId == userId);
        }

        if (query.CreatedBy is not null)
        {
            filtered = filtered.Where(w => w.CreatedById == userId);
        }

        if (query.Stage is not null)
        {
            List<Guid> stageStateIds = WorkItemSearchFilters.ParseStage(query.Stage) == FlowStateStage.Backlog
                ? backlogIds
                : inFlightStateIds;
            filtered = filtered.Where(w => stageStateIds.Contains(w.FlowStateId));
        }

        if (query.Stalled is not null)
        {
            // Whole 24-hour blocks: entered at or before now - 7 days is 7 days or more in the state.
            DateTime stalledSince = dateTimeProvider.UtcNow.AddDays(-WorkItem.StalledThresholdDays);
            filtered = filtered.Where(w => inFlightStateIds.Contains(w.FlowStateId) && w.EnteredCurrentStateOnUtc <= stalledSince);
        }

        if (query.Priority is not null)
        {
            List<Priority> priorities = WorkItemSearchFilters.ParsePriorities(query.Priority);
            filtered = filtered.Where(w => priorities.Contains(w.Priority));
        }

        return filtered;
    }

    private sealed record ActiveStateRow(Guid Id, Guid ProjectId, int SortOrder);
}
