namespace Aurora.Flowboard.Domain.Projects.Events;

public sealed class ComponentCreatedDomainEvent(
    Guid projectId,
    Guid componentId,
    string name) : DomainEvent
{
    public Guid ProjectId { get; init; } = projectId;
    public Guid ComponentId { get; init; } = componentId;
    public string Name { get; init; } = name;
}
