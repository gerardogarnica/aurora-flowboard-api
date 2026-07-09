namespace Aurora.Flowboard.Domain.WorkItems.Events;

public sealed class WorkItemTitleUpdatedDomainEvent(Guid workItemId, string newTitle) : DomainEvent
{
    public Guid WorkItemId { get; init; } = workItemId;
    public string NewTitle { get; init; } = newTitle;
}
