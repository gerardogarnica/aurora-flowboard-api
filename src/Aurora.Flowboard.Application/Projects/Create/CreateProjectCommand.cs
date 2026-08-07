namespace Aurora.Flowboard.Application.Projects.Create;

public sealed record CreateProjectCommand(
    string Name,
    string? Description,
    string Prefix,
    ProjectKind Kind,
    string Color) : ICommand<Guid>;
