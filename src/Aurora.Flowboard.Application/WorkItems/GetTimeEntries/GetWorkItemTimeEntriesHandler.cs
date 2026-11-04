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

        return await dbContext
            .TimeEntries
            .AsNoTracking()
            .Where(t => t.WorkItemId == query.WorkItemId)
            .OrderByDescending(t => t.LoggedOnUtc)
            .ThenByDescending(t => t.Id)
            .ToPagedResponseAsync(
                query,
                t => new WorkItemTimeEntryResponse(
                    t.Id,
                    t.UserId,
                    dbContext.Users
                        .Where(u => u.Id == t.UserId)
                        .Select(u => u.FirstName + " " + u.LastName)
                        .FirstOrDefault() ?? string.Empty,
                    t.Hours,
                    t.Description,
                    t.LoggedOnUtc),
                cancellationToken);
    }
}
