namespace Aurora.Flowboard.Application.Flows.Create;

public sealed record CreateFlowCommand(
    string Name,
    string? Description,
    Guid ProjectId,
    bool IsDefault) : ICommand<Guid>;
