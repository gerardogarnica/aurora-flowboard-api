namespace Aurora.Flowboard.Domain.WorkItems.Events;

public sealed class WorkItemMovedDomainEvent(Guid workItemId, Guid? fromStateId, Guid toStateId) : DomainEvent
{
    public Guid WorkItemId { get; init; } = workItemId;
    public Guid? FromStateId { get; init; } = fromStateId;
    public Guid ToStateId { get; init; } = toStateId;
}
