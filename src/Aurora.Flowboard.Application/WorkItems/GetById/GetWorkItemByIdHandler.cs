namespace Aurora.Flowboard.Application.WorkItems.GetById;

internal sealed class GetWorkItemByIdHandler(
    IApplicationDbContext dbContext) : IQueryHandler<GetWorkItemByIdQuery, WorkItemResponse>
{
    public async Task<Result<WorkItemResponse>> Handle(
        GetWorkItemByIdQuery query,
        CancellationToken cancellationToken)
    {
        WorkItemResponse? response = await dbContext
            .WorkItems
            .Where(w => w.Id == query.WorkItemId)
            .Select(w => new WorkItemResponse(
                w.Id,
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
                    .Select(c => new WorkItemCommentResponse(c.Id, c.AuthorId, c.Content, c.CreatedOnUtc, c.UpdatedOnUtc))
                    .ToList(),
                w.TimeEntries
                    .Select(t => new WorkItemTimeEntryResponse(t.Id, t.UserId, t.Hours, t.Description, t.LoggedOnUtc))
                    .ToList(),
                w.StateHistory
                    .OrderBy(s => s.ChangedOnUtc)
                    .Select(s => new WorkItemStateTransitionResponse(s.Id, s.FromStateId, s.ToStateId, s.ChangedById, s.Reason, s.ChangedOnUtc))
                    .ToList(),
                w.ChangeLogs
                    .OrderBy(c => c.ChangedOnUtc)
                    .Select(c => new WorkItemChangeLogResponse(c.Id, c.ChangedById, c.ChangeType, c.AffectedEntityId, c.ChangedOnUtc))
                    .ToList()))
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);

        if (response is null)
        {
            return Result.Fail<WorkItemResponse>(WorkItemErrors.NotFound);
        }

        return response;
    }
}
