namespace Aurora.Flowboard.Domain.Flows;

public sealed class FlowState
{
    private const int MaxNameLength = 50;

    public Guid Id { get; private set; }
    public Guid FlowId { get; private set; }
    public string Name { get; private set; }
    public int SortOrder { get; private set; }
    public FlowStateCategory Category { get; private set; }

    private FlowState() { } // EF Core

    private FlowState(Guid id, Guid flowId, string name, int sortOrder, FlowStateCategory category)
    {
        Id = id;
        FlowId = flowId;
        Name = name;
        SortOrder = sortOrder;
        Category = category;
    }

    internal static Result<FlowState> Create(Flow flow, string name, int sortOrder, FlowStateCategory category)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Fail<FlowState>(FlowErrors.StateNameRequired);
        }

        if (name.Length > MaxNameLength)
        {
            return Result.Fail<FlowState>(FlowErrors.StateNameTooLong);
        }

        return new FlowState(
            Guid.NewGuid(),
            flow.Id,
            name.Trim(),
            sortOrder,
            category);
    }

    internal void IncrementSortOrder() => SortOrder++;

    internal void DecrementSortOrder() => SortOrder--;
}
