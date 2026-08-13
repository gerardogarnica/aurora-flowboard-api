namespace Aurora.Flowboard.Application.Projects.AddFlowTransitionRole;

public sealed record AddFlowTransitionRoleCommand(
    Guid ProjectId,
    Guid TransitionId,
    ProjectRole Role) : ICommand;
