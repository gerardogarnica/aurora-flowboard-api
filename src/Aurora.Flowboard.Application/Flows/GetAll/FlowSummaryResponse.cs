namespace Aurora.Flowboard.Application.Flows.GetAll;

public sealed record FlowSummaryResponse(
    Guid FlowId,
    string Name,
    string? Description,
    Guid ProjectId,
    bool IsDefault,
    bool IsActive,
    int StateCount,
    int TransitionCount);
