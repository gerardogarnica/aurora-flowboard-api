namespace Aurora.Flowboard.Application.Projects.GetBoard;

internal sealed class GetProjectBoardHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext) : IQueryHandler<GetProjectBoardQuery, IReadOnlyCollection<BoardColumnResponse>>
{
    public async Task<Result<IReadOnlyCollection<BoardColumnResponse>>> Handle(
        GetProjectBoardQuery query,
        CancellationToken cancellationToken)
    {
        bool isMember = await dbContext.IsProjectMemberAsync(query.ProjectId, userContext.UserId, cancellationToken);

        if (!isMember)
        {
            return Result.Fail<IReadOnlyCollection<BoardColumnResponse>>(ProjectErrors.NotFound);
        }

        List<FlowStateProjection> stateEntities = await dbContext
            .FlowStates
            .AsNoTracking()
            .Where(fs => fs.ProjectId == query.ProjectId && fs.Category != FlowStateCategory.Cancelled)
            .Select(fs => new FlowStateProjection(fs.Id, fs.Name, fs.Category, fs.SortOrder, fs.Color.Value))
            .ToListAsync(cancellationToken);

        List<FlowStateProjection> orderedStates = [
            .. stateEntities.Where(s => s.Category == FlowStateCategory.Active).OrderBy(s => s.SortOrder),
            .. stateEntities.Where(s => s.Category == FlowStateCategory.Completed).OrderBy(s => s.Name, StringComparer.Ordinal)
        ];

        List<Guid> stateIds = [.. orderedStates.Select(s => s.Id)];

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
                w.ComponentId,
                w.MilestoneId,
                AssigneeFirstName = dbContext.Users
                    .Where(u => u.Id == w.AssigneeId)
                    .Select(u => u.FirstName)
                    .FirstOrDefault(),
                AssigneeLastName = dbContext.Users
                    .Where(u => u.Id == w.AssigneeId)
                    .Select(u => u.LastName)
                    .FirstOrDefault(),
                ComponentName = dbContext.Components
                    .Where(c => c.Id == w.ComponentId)
                    .Select(c => c.Name)
                    .FirstOrDefault(),
                MilestoneName = dbContext.Milestones
                    .Where(m => m.Id == w.MilestoneId)
                    .Select(m => m.Name)
                    .FirstOrDefault(),
                w.EstimatedPoints,
                w.EstimatedCompletionDate,
                w.CreatedOnUtc,
                CommentCount = w.Comments.Count(c => !c.IsDeleted),
                TimeEntryCount = w.TimeEntries.Count
            })
            .ToListAsync(cancellationToken);

        List<BoardColumnResponse> board = [.. orderedStates
            .Select(s => new BoardColumnResponse(
                s.Id,
                s.Name,
                s.Category,
                s.SortOrder,
                s.Color,
                [.. workItemRows
                    .Where(w => w.FlowStateId == s.Id)
                    .OrderByDescending(w => w.Priority)
                    .ThenBy(w => w.CreatedOnUtc)
                    .Select(w => new BoardWorkItemResponse(
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
                        w.ComponentName,
                        w.MilestoneName,
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
