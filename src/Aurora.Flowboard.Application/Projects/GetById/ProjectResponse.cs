namespace Aurora.Flowboard.Application.Projects.GetById;

public sealed record ProjectResponse(
    Guid ProjectId,
    string Name,
    string? Description,
    DateOnly? EstimatedCompletionDate,
    ProjectStatus Status,
    Guid CreatedById,
    string CreatedByFullName,
    DateTime CreatedOnUtc,
    DateTime? UpdatedOnUtc,
    IReadOnlyCollection<ProjectMemberResponse> Members);

public sealed record ProjectMemberResponse(
    Guid UserId,
    string FullName,
    ProjectRole Role,
    DateTime JoinedOnUtc);
