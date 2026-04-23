namespace Aurora.Flowboard.Domain.WorkItems.Events;

public sealed class WorkItemTagRemovedDomainEvent(Guid workItemId, Guid tagId) : DomainEvent
{
    public Guid WorkItemId { get; init; } = workItemId;
    public Guid TagId { get; init; } = tagId;
}
