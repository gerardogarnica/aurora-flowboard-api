namespace Aurora.Flowboard.Application.Projects.UpdateProject;

public sealed record UpdateProjectCommand(
    Guid Id,
    string Name,
    string? Description,
    DateOnly? EstimatedCompletionDate) : ICommand;
