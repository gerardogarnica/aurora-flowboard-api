namespace Aurora.Flowboard.Application.Flows.GetById;

public sealed record FlowResponse(
    Guid FlowId,
    string Name,
    string? Description,
    Guid ProjectId,
    bool IsDefault,
    bool IsActive,
    DateTime CreatedOnUtc,
    DateTime? UpdatedOnUtc,
    IReadOnlyCollection<FlowStateResponse> States,
    IReadOnlyCollection<FlowTransitionResponse> Transitions);

public sealed record FlowStateResponse(
    Guid StateId,
    string Name,
    int SortOrder,
    FlowStateCategory Category);

public sealed record FlowTransitionResponse(
    Guid TransitionId,
    Guid FromStateId,
    string FromStateName,
    Guid ToStateId,
    string ToStateName,
    ProjectRole AllowedRole);
