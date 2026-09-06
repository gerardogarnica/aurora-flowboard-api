namespace Aurora.Flowboard.Application.WorkItems.GetTimeEntries;

public sealed record GetWorkItemTimeEntriesQuery(
    Guid WorkItemId,
    int Page,
    int PageSize) : IQuery<PagedResponse<WorkItemTimeEntryResponse>>;
