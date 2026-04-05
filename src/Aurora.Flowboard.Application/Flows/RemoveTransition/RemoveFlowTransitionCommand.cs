namespace Aurora.Flowboard.Application.Flows.RemoveTransition;

public sealed record RemoveFlowTransitionCommand(
    Guid FlowId,
    Guid TransitionId) : ICommand;
