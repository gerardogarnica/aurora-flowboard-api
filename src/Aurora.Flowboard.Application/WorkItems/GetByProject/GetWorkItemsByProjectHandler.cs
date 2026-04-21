namespace Aurora.Flowboard.Application.WorkItems.GetByProject;

internal sealed class GetWorkItemsByProjectHandler(
    IApplicationDbContext dbContext) : IQueryHandler<GetWorkItemsByProjectQuery, IReadOnlyCollection<WorkItemSummaryResponse>>
{
    public async Task<Result<IReadOnlyCollection<WorkItemSummaryResponse>>> Handle(
        GetWorkItemsByProjectQuery query,
        CancellationToken cancellationToken)
    {
        bool projectExists = await dbContext
            .Projects
            .AnyAsync(p => p.Id == query.ProjectId, cancellationToken);

        if (!projectExists)
        {
            return Result.Fail<IReadOnlyCollection<WorkItemSummaryResponse>>(ProjectErrors.NotFound);
        }

        List<WorkItemSummaryResponse> workItems = await dbContext
            .WorkItems
            .AsNoTracking()
            .Where(w => w.ProjectId == query.ProjectId)
            .OrderBy(w => w.CreatedOnUtc)
            .Select(w => new WorkItemSummaryResponse(
                w.Id,
                w.Title,
                w.Type,
                w.Priority,
                w.FlowStateId,
                w.FlowState.Name,
                w.AssigneeId,
                w.EstimatedPoints,
                w.EstimatedCompletionDate,
                w.CreatedOnUtc,
                w.Comments.Count(c => !c.IsDeleted),
                w.TimeEntries.Count))
            .ToListAsync(cancellationToken);

        return workItems;
    }
}
