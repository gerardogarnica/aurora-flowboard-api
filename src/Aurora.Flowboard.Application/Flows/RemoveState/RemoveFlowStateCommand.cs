namespace Aurora.Flowboard.Application.Flows.RemoveState;

public sealed record RemoveFlowStateCommand(
    Guid FlowId,
    Guid StateId) : ICommand;
