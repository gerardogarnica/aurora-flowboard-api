namespace Aurora.Flowboard.Application.Projects.Setup;

public sealed record SetupProjectCommand(
    string Name,
    string? Description,
    string Code,
    string Color,
    DateOnly? EstimatedCompletionDate,
    SetupProjectFlowDto Flow) : ICommand<Guid>;

public sealed record SetupProjectFlowDto(
    string Name,
    string? Description,
    IReadOnlyCollection<SetupProjectFlowStateDto> States);

public sealed record SetupProjectFlowStateDto(
    string Name,
    FlowStateCategory Category,
    IReadOnlyCollection<ProjectRole> Roles);
