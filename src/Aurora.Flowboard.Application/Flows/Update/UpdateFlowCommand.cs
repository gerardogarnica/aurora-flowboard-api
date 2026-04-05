namespace Aurora.Flowboard.Application.Flows.Update;

public sealed record UpdateFlowCommand(
    Guid Id,
    string Name,
    string? Description) : ICommand;
