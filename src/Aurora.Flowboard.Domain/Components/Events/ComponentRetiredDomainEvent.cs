namespace Aurora.Flowboard.Domain.Components.Events;

public sealed class ComponentRetiredDomainEvent(
    Guid componentId,
    Guid projectId) : DomainEvent
{
    public Guid ComponentId { get; init; } = componentId;
    public Guid ProjectId { get; init; } = projectId;
}
