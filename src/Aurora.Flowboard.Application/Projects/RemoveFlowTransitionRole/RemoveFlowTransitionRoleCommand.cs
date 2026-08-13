namespace Aurora.Flowboard.Application.Projects.RemoveFlowTransitionRole;

public sealed record RemoveFlowTransitionRoleCommand(
    Guid ProjectId,
    Guid TransitionId,
    ProjectRole Role) : ICommand;
