namespace Aurora.Flowboard.Application.Projects.Create;

public sealed record CreateProjectCommand(
    string Name,
    string? Description,
    string Prefix,
    ProjectKind Kind,
    string Color,
    IReadOnlyCollection<CreateProjectState> FlowStates) : ICommand<Guid>;

public sealed record CreateProjectState(
    string Name,
    FlowStateCategory Category,
    string Color,
    IReadOnlyCollection<ProjectRole> AllowedRoles);
