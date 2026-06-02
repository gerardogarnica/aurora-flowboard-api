namespace Aurora.Flowboard.Domain.Flows;

public sealed class FlowState
{
    public const int MaxNameLength = 50;

    public Guid Id { get; private set; }
    public Guid FlowId { get; private set; }
    public string Name { get; private set; }
    public int SortOrder { get; private set; }
    public FlowStateCategory Category { get; private set; }
    public Color Color { get; private set; }

    public Flow Flow { get; init; } = null!;

    private FlowState() { } // EF Core

    private FlowState(Guid id, Guid flowId, string name, int sortOrder, FlowStateCategory category, Color color)
    {
        Id = id;
        FlowId = flowId;
        Name = name;
        SortOrder = sortOrder;
        Category = category;
        Color = color;
    }

    internal static Result<FlowState> Create(Flow flow, string name, int sortOrder, FlowStateCategory category, Color color)
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
            category,
            color);
    }

    internal void IncrementSortOrder() => SortOrder++;

    internal void DecrementSortOrder() => SortOrder--;
}
