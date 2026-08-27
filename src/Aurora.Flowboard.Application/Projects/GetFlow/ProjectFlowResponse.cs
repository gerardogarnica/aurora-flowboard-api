namespace Aurora.Flowboard.Application.Projects.GetFlow;

public sealed record ProjectFlowResponse(
    Guid ProjectId,
    IReadOnlyCollection<FlowStateResponse> States,
    IReadOnlyCollection<FlowTransitionResponse> Transitions);

public sealed record FlowStateResponse(
    Guid StateId,
    string Name,
    int SortOrder,
    FlowStateCategory Category,
    string Color);

public sealed record FlowTransitionResponse(
    Guid TransitionId,
    Guid FromStateId,
    string FromStateName,
    Guid ToStateId,
    string ToStateName,
    IReadOnlyCollection<ProjectRole> AllowedRoles);
