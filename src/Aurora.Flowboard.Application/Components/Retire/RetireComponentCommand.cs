namespace Aurora.Flowboard.Application.Components.Retire;

public sealed record RetireComponentCommand(Guid ComponentId) : ICommand;
