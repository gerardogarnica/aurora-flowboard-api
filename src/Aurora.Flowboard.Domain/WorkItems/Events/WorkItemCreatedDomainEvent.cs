namespace Aurora.Flowboard.Domain.WorkItems.Events;

public sealed class WorkItemCreatedDomainEvent(Guid workItemId) : DomainEvent
{
    public Guid WorkItemId { get; init; } = workItemId;
}
