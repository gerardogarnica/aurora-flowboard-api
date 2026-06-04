namespace Aurora.Flowboard.Application.Projects.GetAll;

public sealed record ProjectSummaryResponse(
    Guid ProjectId,
    string Name,
    string? Description,
    string Code,
    string Color,
    DateOnly? EstimatedCompletionDate,
    ProjectStatus Status,
    int OpenWorkItems,
    int ClosedWorkItems,
    IReadOnlyCollection<ProjectMemberSummaryResponse> Members,
    IReadOnlyCollection<ProjectFlowSummaryResponse> Flows);

public sealed record ProjectMemberSummaryResponse(
    Guid UserId,
    string FullName,
    string Initials);

public sealed record ProjectFlowSummaryResponse(
    Guid FlowId,
    string Name,
    string? Description,
    bool IsDefault,
    bool IsActive);
