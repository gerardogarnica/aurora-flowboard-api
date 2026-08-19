namespace Aurora.Flowboard.Application.Projects.GetAll;

public sealed record ProjectSummaryResponse(
    Guid ProjectId,
    string Name,
    string? Description,
    string Prefix,
    string Color,
    ProjectKind Kind,
    ProjectStatus Status,
    int OpenWorkItems,
    int ClosedWorkItems,
    bool CanModifyFlowStates,
    bool CanAddOrUpdateWorkItems,
    IReadOnlyCollection<ProjectMemberSummaryResponse> Members);

public sealed record ProjectMemberSummaryResponse(
    Guid UserId,
    string FullName,
    string Initials,
    ProjectRole Role);
