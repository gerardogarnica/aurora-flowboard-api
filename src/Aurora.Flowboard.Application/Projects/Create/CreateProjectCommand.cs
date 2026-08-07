namespace Aurora.Flowboard.Application.Projects.Create;

public sealed record CreateProjectCommand(
    string Name,
    string? Description,
    string Code,
    ProjectKind Kind,
    string Color) : ICommand<Guid>;
