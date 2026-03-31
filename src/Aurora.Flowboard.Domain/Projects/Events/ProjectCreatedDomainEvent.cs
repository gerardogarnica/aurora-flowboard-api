namespace Aurora.Flowboard.Domain.Projects.Events;

public sealed class ProjectCreatedDomainEvent(Guid projectId) : DomainEvent
{
    public Guid ProjectId { get; init; } = projectId;
}
