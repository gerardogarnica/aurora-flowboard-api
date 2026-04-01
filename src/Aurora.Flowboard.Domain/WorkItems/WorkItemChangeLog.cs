using Aurora.Flowboard.Domain.Users;

namespace Aurora.Flowboard.Domain.WorkItems;

public sealed class WorkItemChangeLog
{
    public Guid Id { get; private set; }
    public Guid WorkItemId { get; private set; }
    public Guid ChangedById { get; private set; }
    public WorkItemChangeType ChangeType { get; private set; }
    public Guid? AffectedEntityId { get; private set; }
    public DateTime ChangedOnUtc { get; private set; }

    private WorkItemChangeLog() { } // EF Core

    private WorkItemChangeLog(
        Guid id,
        Guid workItemId,
        Guid changedById,
        WorkItemChangeType changeType,
        Guid? affectedEntityId,
        DateTime changedOnUtc)
    {
        Id = id;
        WorkItemId = workItemId;
        ChangedById = changedById;
        ChangeType = changeType;
        AffectedEntityId = affectedEntityId;
        ChangedOnUtc = changedOnUtc;
    }

    internal static WorkItemChangeLog Create(
        WorkItem workItem,
        User changedBy,
        WorkItemChangeType changeType,
        Guid? affectedEntityId,
        DateTime changedOnUtc) =>
        new(Guid.NewGuid(), workItem.Id, changedBy.Id, changeType, affectedEntityId, changedOnUtc);
}
