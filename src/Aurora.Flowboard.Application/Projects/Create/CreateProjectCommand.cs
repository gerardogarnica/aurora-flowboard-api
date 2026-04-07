namespace Aurora.Flowboard.Application.Projects.Create;

public sealed record CreateProjectCommand(
    string Name,
    string? Description,
    DateOnly? EstimatedCompletionDate) : ICommand<Guid>;
