namespace Aurora.Flowboard.Application.Projects.Shared;

public sealed record WorkItemFlowTransitionResponse(
    Guid ToStateId,
    string ToStateName);
