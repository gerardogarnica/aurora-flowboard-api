namespace Aurora.Flowboard.Application.Projects.AddFlowState;

public sealed record AddFlowStateCommand(
    Guid ProjectId,
    string Name,
    FlowStateCategory Category,
    string Color,
    IReadOnlyCollection<ProjectRole> AllowedRoles) : ICommand;
