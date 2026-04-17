namespace Aurora.Flowboard.Domain.WorkItems.Events;

public sealed class WorkItemDeactivatedDomainEvent(Guid workItemId) : DomainEvent
{
    public Guid WorkItemId { get; init; } = workItemId;
}
