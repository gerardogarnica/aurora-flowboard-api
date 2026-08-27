namespace Aurora.Flowboard.Application.WorkItems.GetByProject;

internal sealed class GetWorkItemsByProjectHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext) : IQueryHandler<GetWorkItemsByProjectQuery, IReadOnlyCollection<FlowStateBoardResponse>>
{
    public async Task<Result<IReadOnlyCollection<FlowStateBoardResponse>>> Handle(
        GetWorkItemsByProjectQuery query,
        CancellationToken cancellationToken)
    {
        bool isMember = await dbContext.IsProjectMemberAsync(query.ProjectId, userContext.UserId, cancellationToken);

        if (!isMember)
        {
            return Result.Fail<IReadOnlyCollection<FlowStateBoardResponse>>(ProjectErrors.NotFound);
        }

        List<FlowState> stateEntities = await dbContext
            .FlowStates
            .AsNoTracking()
            .Where(fs => fs.ProjectId == query.ProjectId)
            .ToListAsync(cancellationToken);

        List<FlowStateProjection> states = [.. stateEntities.Select(fs => new FlowStateProjection(fs.Id, fs.Name, fs.Category, fs.SortOrder, fs.Color.Value))];

        List<Guid> stateIds = [.. states.Select(s => s.Id)];

        var workItemRows = await dbContext
            .WorkItems
            .AsNoTracking()
            .Where(w => w.ProjectId == query.ProjectId && stateIds.Contains(w.FlowStateId))
            .Select(w => new
            {
                w.Id,
                w.Title,
                w.Code,
                w.Type,
                w.Priority,
                w.FlowStateId,
                w.AssigneeId,
                AssigneeFirstName = dbContext.Users
                    .Where(u => u.Id == w.AssigneeId)
                    .Select(u => u.FirstName)
                    .FirstOrDefault(),
                AssigneeLastName = dbContext.Users
                    .Where(u => u.Id == w.AssigneeId)
                    .Select(u => u.LastName)
                    .FirstOrDefault(),
                w.EstimatedPoints,
                w.EstimatedCompletionDate,
                w.CreatedOnUtc,
                CommentCount = w.Comments.Count(c => !c.IsDeleted),
                TimeEntryCount = w.TimeEntries.Count
            })
            .ToListAsync(cancellationToken);

        List<FlowStateBoardResponse> board = [.. states
            .OrderBy(s => s.Category).ThenBy(s => s.SortOrder)
            .Select(s => new FlowStateBoardResponse(
                s.Id,
                s.Name,
                s.Category,
                s.SortOrder,
                s.Color,
                [.. workItemRows
                    .Where(w => w.FlowStateId == s.Id)
                    .OrderByDescending(w => w.Priority)
                    .ThenBy(w => w.CreatedOnUtc)
                    .Select(w => new WorkItemSummaryResponse(
                        w.Id,
                        w.Code,
                        w.Title,
                        w.Type,
                        w.Priority,
                        w.FlowStateId,
                        s.Name,
                        w.AssigneeId,
                        BuildInitials(w.AssigneeFirstName, w.AssigneeLastName),
                        BuildFullName(w.AssigneeFirstName, w.AssigneeLastName),
                        w.EstimatedPoints,
                        w.EstimatedCompletionDate,
                        w.CreatedOnUtc,
                        w.CommentCount,
                        w.TimeEntryCount))]))];

        return board;
    }

    private static string? BuildInitials(string? firstName, string? lastName) =>
        firstName is not null && lastName is not null
            ? $"{char.ToUpperInvariant(firstName[0])}{char.ToUpperInvariant(lastName[0])}"
            : null;

    private static string? BuildFullName(string? firstName, string? lastName) =>
        firstName is not null && lastName is not null
            ? $"{firstName} {lastName}"
            : null;

    private sealed record FlowStateProjection(Guid Id, string Name, FlowStateCategory Category, int SortOrder, string Color);
}
