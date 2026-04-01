namespace Aurora.Flowboard.Domain.WorkItems.Events;

public sealed class WorkItemAssignedDomainEvent(Guid workItemId, Guid? assigneeId) : DomainEvent
{
    public Guid WorkItemId { get; init; } = workItemId;
    public Guid? AssigneeId { get; init; } = assigneeId;
}
