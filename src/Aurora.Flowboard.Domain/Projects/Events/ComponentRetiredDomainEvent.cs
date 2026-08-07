namespace Aurora.Flowboard.Domain.Projects.Events;

public sealed class ComponentRetiredDomainEvent(
    Guid projectId,
    Guid componentId) : DomainEvent
{
    public Guid ProjectId { get; init; } = projectId;
    public Guid ComponentId { get; init; } = componentId;
}
