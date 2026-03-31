namespace Aurora.Flowboard.Domain.Projects.Events;

public sealed class ProjectMemberAddedDomainEvent(Guid projectId, Guid userId, ProjectRole role) : DomainEvent
{
    public Guid ProjectId { get; init; } = projectId;
    public Guid UserId { get; init; } = userId;
    public ProjectRole Role { get; init; } = role;
}
