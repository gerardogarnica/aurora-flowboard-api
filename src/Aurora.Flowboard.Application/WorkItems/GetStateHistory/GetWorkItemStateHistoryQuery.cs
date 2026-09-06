namespace Aurora.Flowboard.Application.WorkItems.GetStateHistory;

public sealed record GetWorkItemStateHistoryQuery(
    Guid WorkItemId,
    int Page,
    int PageSize) : IQuery<PagedResponse<WorkItemStateTransitionResponse>>;
