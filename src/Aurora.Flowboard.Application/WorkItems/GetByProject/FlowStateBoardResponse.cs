namespace Aurora.Flowboard.Application.WorkItems.GetByProject;

public sealed record FlowStateBoardResponse(
    Guid FlowStateId,
    string FlowStateName,
    FlowStateCategory Category,
    int SortOrder,
    string Color,
    IReadOnlyCollection<WorkItemSummaryResponse> WorkItems);
