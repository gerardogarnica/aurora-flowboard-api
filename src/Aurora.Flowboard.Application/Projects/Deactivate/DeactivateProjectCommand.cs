namespace Aurora.Flowboard.Application.Projects.DeactivateProject;

public sealed record DeactivateProjectCommand(Guid Id) : ICommand;
