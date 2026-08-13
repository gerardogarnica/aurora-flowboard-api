namespace Aurora.Flowboard.Application.Projects.Setup;

public sealed record SetupProjectCommand(
    string Name,
    string? Description,
    string Prefix,
    ProjectKind Kind,
    string Color,
    IReadOnlyCollection<SetupProjectFlowStateDto> FlowStates) : ICommand<Guid>;

public sealed record SetupProjectFlowStateDto(
    string Name,
    FlowStateCategory Category,
    string Color,
    IReadOnlyCollection<ProjectRole> Roles);
