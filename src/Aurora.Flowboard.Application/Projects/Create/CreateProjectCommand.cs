namespace Aurora.Flowboard.Application.Projects.Create;

public sealed record CreateProjectCommand(
    string Name,
    string? Description,
    string Code,
    DateOnly? EstimatedCompletionDate) : ICommand<Guid>;
