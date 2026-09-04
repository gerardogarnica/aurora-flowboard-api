namespace Aurora.Flowboard.Application.WorkItems.GetTimeEntries;

internal sealed class GetWorkItemTimeEntriesHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext) : IQueryHandler<GetWorkItemTimeEntriesQuery, PagedResponse<WorkItemTimeEntryResponse>>
{
    public async Task<Result<PagedResponse<WorkItemTimeEntryResponse>>> Handle(
        GetWorkItemTimeEntriesQuery query,
        CancellationToken cancellationToken)
    {
        bool canAccess = await dbContext.CanAccessWorkItemAsync(query.WorkItemId, userContext.UserId, cancellationToken);

        if (!canAccess)
        {
            return Result.Fail<PagedResponse<WorkItemTimeEntryResponse>>(WorkItemErrors.NotFound);
        }

        IQueryable<TimeEntry> timeEntries = dbContext
            .TimeEntries
            .AsNoTracking()
            .Where(t => t.WorkItemId == query.WorkItemId);

        int totalCount = await timeEntries.CountAsync(cancellationToken);

        List<WorkItemTimeEntryResponse> items = await timeEntries
            .OrderByDescending(t => t.LoggedOnUtc)
            .ThenByDescending(t => t.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
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
            .ToListAsync(cancellationToken);

        return new PagedResponse<WorkItemTimeEntryResponse>(items, query.Page, query.PageSize, totalCount);
    }
}
