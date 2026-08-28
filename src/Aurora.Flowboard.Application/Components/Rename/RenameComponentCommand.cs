namespace Aurora.Flowboard.Application.Components.Rename;

public sealed record RenameComponentCommand(
    Guid ComponentId,
    string Name) : ICommand;
