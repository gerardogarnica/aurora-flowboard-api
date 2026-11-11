namespace Aurora.Flowboard.Application.Projects.GetBoard;

public sealed record BoardColumnResponse(
    Guid FlowStateId,
    string FlowStateName,
    FlowStateCategory Category,
    int SortOrder,
    string Color,
    FlowStateStage Stage,
    IReadOnlyCollection<BoardWorkItemResponse> WorkItems,
    IReadOnlyCollection<WorkItemFlowTransitionResponse> AvailableTransitions);
