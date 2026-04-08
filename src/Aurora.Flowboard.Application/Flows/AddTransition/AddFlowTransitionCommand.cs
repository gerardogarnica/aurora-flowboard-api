namespace Aurora.Flowboard.Application.Flows.AddTransition;

public sealed record AddFlowTransitionCommand(
    Guid FlowId,
    Guid FromStateId,
    Guid ToStateId,
    bool IsGlobal,
    IReadOnlyCollection<ProjectRole> AllowedRoles) : ICommand;
