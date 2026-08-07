namespace Aurora.Flowboard.Application.Projects.Update;

public sealed record UpdateProjectCommand(
    Guid Id,
    string Name,
    string? Description,
    string Color) : ICommand;
