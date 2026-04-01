namespace Aurora.Flowboard.Domain.WorkItems.Events;

public sealed class WorkItemTimeLoggedDomainEvent(Guid workItemId, Guid timeEntryId, decimal hours) : DomainEvent
{
    public Guid WorkItemId { get; init; } = workItemId;
    public Guid TimeEntryId { get; init; } = timeEntryId;
    public decimal Hours { get; init; } = hours;
}
