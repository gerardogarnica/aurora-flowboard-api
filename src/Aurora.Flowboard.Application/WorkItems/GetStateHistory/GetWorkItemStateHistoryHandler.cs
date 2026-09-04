namespace Aurora.Flowboard.Application.WorkItems.GetStateHistory;

internal sealed class GetWorkItemStateHistoryHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext) : IQueryHandler<GetWorkItemStateHistoryQuery, PagedResponse<WorkItemStateTransitionResponse>>
{
    public async Task<Result<PagedResponse<WorkItemStateTransitionResponse>>> Handle(
        GetWorkItemStateHistoryQuery query,
        CancellationToken cancellationToken)
    {
        bool canAccess = await dbContext.CanAccessWorkItemAsync(query.WorkItemId, userContext.UserId, cancellationToken);

        if (!canAccess)
        {
            return Result.Fail<PagedResponse<WorkItemStateTransitionResponse>>(WorkItemErrors.NotFound);
        }

        IQueryable<StateTransitionHistory> stateHistory = dbContext
            .StateTransitionHistories
            .AsNoTracking()
            .Where(s => s.WorkItemId == query.WorkItemId);

        int totalCount = await stateHistory.CountAsync(cancellationToken);

        List<WorkItemStateTransitionResponse> items = await stateHistory
            .OrderByDescending(s => s.ChangedOnUtc)
            .ThenByDescending(s => s.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
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
            .ToListAsync(cancellationToken);

        return new PagedResponse<WorkItemStateTransitionResponse>(items, query.Page, query.PageSize, totalCount);
    }
}
