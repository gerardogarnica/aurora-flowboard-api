namespace Aurora.Flowboard.Application.Projects.Deactivate;

public sealed record DeactivateProjectCommand(Guid Id) : ICommand;
