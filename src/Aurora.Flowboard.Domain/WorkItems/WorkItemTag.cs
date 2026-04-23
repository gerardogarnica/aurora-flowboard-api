namespace Aurora.Flowboard.Domain.WorkItems;

public sealed class WorkItemTag
{
    public const int MaxNameLength = 50;

    public Guid Id { get; private set; }
    public Guid WorkItemId { get; private set; }
    public string Name { get; private set; }

    private WorkItemTag() { } // EF Core

    private WorkItemTag(Guid id, Guid workItemId, string name)
    {
        Id = id;
        WorkItemId = workItemId;
        Name = name;
    }

    internal static WorkItemTag Create(WorkItem workItem, string name) =>
        new(Guid.NewGuid(), workItem.Id, name);
}
