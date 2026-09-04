namespace Aurora.Flowboard.Application.WorkItems.GetChangeLogs;

public sealed record GetWorkItemChangeLogsQuery(
    Guid WorkItemId,
    int Page,
    int PageSize) : IQuery<PagedResponse<WorkItemChangeLogResponse>>;
