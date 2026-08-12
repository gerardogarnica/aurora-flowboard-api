namespace Aurora.Flowboard.Domain.Components.Events;

public sealed class ComponentRenamedDomainEvent(
    Guid componentId,
    Guid projectId,
    string oldName,
    string newName) : DomainEvent
{
    public Guid ComponentId { get; init; } = componentId;
    public Guid ProjectId { get; init; } = projectId;
    public string OldName { get; init; } = oldName;
    public string NewName { get; init; } = newName;
}
