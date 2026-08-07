namespace Aurora.Flowboard.Application.Projects.GetAll;

public sealed record ProjectSummaryResponse(
    Guid ProjectId,
    string Name,
    string? Description,
    string Code,
    string Color,
    ProjectStatus Status,
    int OpenWorkItems,
    int ClosedWorkItems,
    bool CanAddOrUpdateFlows,
    bool CanAddOrUpdateWorkItems,
    IReadOnlyCollection<ProjectMemberSummaryResponse> Members,
    IReadOnlyCollection<ProjectFlowSummaryResponse> Flows);

public sealed record ProjectMemberSummaryResponse(
    Guid UserId,
    string FullName,
    string Initials,
    ProjectRole Role);

public sealed record ProjectFlowSummaryResponse(
    Guid FlowId,
    string Name,
    string? Description,
    bool IsDefault,
    bool IsActive);
