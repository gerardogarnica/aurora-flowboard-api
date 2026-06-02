namespace Aurora.Flowboard.Application.Flows.AddState;

public sealed record AddFlowStateCommand(
    Guid FlowId,
    string Name,
    FlowStateCategory Category,
    string Color,
    IReadOnlyCollection<ProjectRole> AllowedRoles) : ICommand;
