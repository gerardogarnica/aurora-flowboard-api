namespace Aurora.Flowboard.Application.Projects.GetById;

public sealed record ProjectResponse(
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
    Guid CreatedById,
    string CreatedByFullName,
    DateTime CreatedOnUtc,
    DateTime? UpdatedOnUtc,
    IReadOnlyCollection<ProjectMemberResponse> Members);

public sealed record ProjectMemberResponse(
    Guid UserId,
    string FirstName,
    string LastName,
    string FullName,
    string Initials,
    ProjectRole Role,
    DateTime JoinedOnUtc);
