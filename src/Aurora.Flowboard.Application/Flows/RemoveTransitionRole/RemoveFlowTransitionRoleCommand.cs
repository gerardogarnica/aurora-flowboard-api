namespace Aurora.Flowboard.Application.Flows.RemoveTransitionRole;

public sealed record RemoveFlowTransitionRoleCommand(
    Guid FlowId,
    Guid TransitionId,
    ProjectRole Role) : ICommand;
