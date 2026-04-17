namespace Aurora.Flowboard.Domain.WorkItems.Events;

public sealed class WorkItemUnassignedDomainEvent(Guid workItemId) : DomainEvent
{
    public Guid WorkItemId { get; init; } = workItemId;
}
