namespace Aurora.Flowboard.Application.Projects.RemoveFlowState;

public sealed record RemoveFlowStateCommand(
    Guid ProjectId,
    Guid StateId) : ICommand;
