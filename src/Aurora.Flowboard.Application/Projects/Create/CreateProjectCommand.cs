namespace Aurora.Flowboard.Application.Projects.Create;

public sealed record CreateProjectCommand(
    string Name,
    string? Description,
    DateOnly? EstimatedCompletionDate,
    IReadOnlyCollection<ProjectMemberRequest> Members) : ICommand<Guid>;

public sealed record ProjectMemberRequest(Guid UserId, ProjectRole Role);
