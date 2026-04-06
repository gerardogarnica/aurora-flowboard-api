namespace Aurora.Flowboard.Application.Projects.ChangeStatus;

public sealed record ChangeProjectStatusCommand(Guid Id, ProjectStatus NewStatus) : ICommand;
