namespace Aurora.Flowboard.Application.Projects.CreateProject;

public sealed record CreateProjectCommand(
    string Name,
    string? Description,
    DateOnly? EstimatedCompletionDate) : ICommand<Guid>;
