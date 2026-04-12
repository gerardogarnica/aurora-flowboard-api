namespace Aurora.Flowboard.Application.Flows.AddTransitionRole;

public sealed record AddFlowTransitionRoleCommand(
    Guid FlowId,
    Guid TransitionId,
    ProjectRole Role) : ICommand;
