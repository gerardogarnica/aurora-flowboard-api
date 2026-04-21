namespace Aurora.Flowboard.Application.WorkItems.GetById;

internal sealed class GetWorkItemByIdHandler(
    IApplicationDbContext dbContext) : IQueryHandler<GetWorkItemByIdQuery, WorkItemResponse>
{
    public async Task<Result<WorkItemResponse>> Handle(
        GetWorkItemByIdQuery query,
        CancellationToken cancellationToken)
    {
        WorkItem? workItem = await dbContext
            .WorkItems
            .Include(w => w.FlowState)
            .Include(w => w.Comments)
            .Include(w => w.TimeEntries)
            .Include(w => w.StateHistory)
            .Include(w => w.ChangeLogs)
            .AsSplitQuery()
            .AsNoTracking()
            .SingleOrDefaultAsync(w => w.Id == query.WorkItemId, cancellationToken);

        if (workItem is null)
        {
            return Result.Fail<WorkItemResponse>(WorkItemErrors.NotFound);
        }

        string? projectName = await dbContext
            .Projects
            .Where(p => p.Id == workItem.ProjectId)
            .Select(p => p.Name)
            .FirstOrDefaultAsync(cancellationToken);

        string? createdByFullName = await dbContext
            .Users
            .Where(u => u.Id == workItem.CreatedById)
            .Select(u => u.FirstName + " " + u.LastName)
            .FirstOrDefaultAsync(cancellationToken);

        string? assigneeFullName = null;

        if (workItem.AssigneeId.HasValue)
        {
            assigneeFullName = await dbContext
                .Users
                .Where(u => u.Id == workItem.AssigneeId.Value)
                .Select(u => u.FirstName + " " + u.LastName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var response = new WorkItemResponse(
            workItem.Id,
            workItem.Title,
            workItem.Description,
            workItem.Type,
            workItem.Priority,
            workItem.ProjectId,
            projectName ?? string.Empty,
            workItem.FlowStateId,
            workItem.FlowState.Name,
            workItem.AssigneeId,
            assigneeFullName,
            workItem.CreatedById,
            createdByFullName ?? string.Empty,
            workItem.EstimatedPoints,
            workItem.EstimatedCompletionDate,
            workItem.CreatedOnUtc,
            workItem.UpdatedOnUtc,
            workItem.CompletedOnUtc,
            [.. workItem.Comments
                .Where(c => !c.IsDeleted)
                .Select(c => new WorkItemCommentResponse(
                    c.Id,
                    c.AuthorId,
                    c.Content,
                    c.CreatedOnUtc,
                    c.UpdatedOnUtc))],
            [.. workItem.TimeEntries
                .Select(t => new WorkItemTimeEntryResponse(
                    t.Id,
                    t.UserId,
                    t.Hours,
                    t.Description,
                    t.LoggedOnUtc))],
            [.. workItem.StateHistory
                .OrderBy(s => s.ChangedOnUtc)
                .Select(s => new WorkItemStateTransitionResponse(
                    s.Id,
                    s.FromStateId,
                    s.ToStateId,
                    s.ChangedById,
                    s.Reason,
                    s.ChangedOnUtc))],
            [.. workItem.ChangeLogs
                .OrderBy(c => c.ChangedOnUtc)
                .Select(c => new WorkItemChangeLogResponse(
                    c.Id,
                    c.ChangedById,
                    c.ChangeType,
                    c.AffectedEntityId,
                    c.ChangedOnUtc))]);

        return response;
    }
}
