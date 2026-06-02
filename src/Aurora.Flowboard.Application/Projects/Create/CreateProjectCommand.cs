namespace Aurora.Flowboard.Application.Projects.Create;

public sealed record CreateProjectCommand(
    string Name,
    string? Description,
    string Code,
    string Color,
    DateOnly? EstimatedCompletionDate) : ICommand<Guid>;
