namespace Aurora.Flowboard.Application.Projects.GetById;

public sealed record ProjectResponse(
    Guid ProjectId,
    string Name,
    string? Description,
    string Code,
    string Color,
    DateOnly? EstimatedCompletionDate,
    ProjectStatus Status,
    int OpenWorkItems,
    int ClosedWorkItems,
    Guid CreatedById,
    string CreatedByFullName,
    DateTime CreatedOnUtc,
    DateTime? UpdatedOnUtc,
    IReadOnlyCollection<ProjectMemberResponse> Members,
    IReadOnlyCollection<ProjectChangeLogResponse> ChangeLogs);

public sealed record ProjectMemberResponse(
    Guid UserId,
    string FirstName,
    string LastName,
    string FullName,
    string Initials,
    ProjectRole Role,
    DateTime JoinedOnUtc);

public sealed record ProjectChangeLogResponse(
    Guid Id,
    Guid ChangedById,
    string ChangedByFullName,
    ProjectChangeType ChangeType,
    Guid? AffectedEntityId,
    ProjectStatus? NewStatus,
    DateTime ChangedOnUtc);
