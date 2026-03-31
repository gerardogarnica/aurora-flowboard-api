namespace Aurora.Flowboard.Domain.Projects.Events;

public sealed class ProjectDeactivatedDomainEvent(Guid projectId) : DomainEvent
{
    public Guid ProjectId { get; init; } = projectId;
}
