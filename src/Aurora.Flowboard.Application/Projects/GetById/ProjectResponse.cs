namespace Aurora.Flowboard.Application.Projects.GetById;

public sealed record ProjectResponse(
    Guid ProjectId,
    string Name,
    string? Description,
    DateOnly? EstimatedCompletionDate,
    ProjectStatus Status,
    DateTime CreatedOnUtc,
    DateTime? UpdatedOnUtc,
    IReadOnlyCollection<ProjectMemberResponse> Members);

public sealed record ProjectMemberResponse(
    Guid UserId,
    ProjectRole Role,
    DateTime JoinedOnUtc);
