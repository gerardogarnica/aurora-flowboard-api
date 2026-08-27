namespace Aurora.Flowboard.Domain.Components.Events;

public sealed class ComponentCreatedDomainEvent(
    Guid componentId,
    Guid projectId,
    string name) : DomainEvent
{
    public Guid ComponentId { get; init; } = componentId;
    public Guid ProjectId { get; init; } = projectId;
    public string Name { get; init; } = name;
}
