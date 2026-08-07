namespace Aurora.Flowboard.Domain.Projects.Events;

public sealed class ComponentRenamedDomainEvent(
    Guid projectId,
    Guid componentId,
    string oldName,
    string newName) : DomainEvent
{
    public Guid ProjectId { get; init; } = projectId;
    public Guid ComponentId { get; init; } = componentId;
    public string OldName { get; init; } = oldName;
    public string NewName { get; init; } = newName;
}
