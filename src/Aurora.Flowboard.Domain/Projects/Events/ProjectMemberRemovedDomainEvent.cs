namespace Aurora.Flowboard.Domain.Projects.Events;

public sealed class ProjectMemberRemovedDomainEvent(Guid projectId, Guid userId) : DomainEvent
{
    public Guid ProjectId { get; init; } = projectId;
    public Guid UserId { get; init; } = userId;
}
