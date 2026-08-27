namespace Aurora.Flowboard.Application.Components.Create;

public sealed record CreateComponentCommand(
    Guid ProjectId,
    string Name) : ICommand<Guid>;
