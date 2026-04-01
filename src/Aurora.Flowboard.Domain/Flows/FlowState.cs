namespace Aurora.Flowboard.Domain.Flows;

public sealed class FlowState
{
    private const int MaxNameLength = 100;

    public Guid Id { get; private set; }
    public Guid FlowId { get; private set; }
    public string Name { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsTerminal { get; private set; }

    private FlowState() { } // EF Core

    private FlowState(Guid id, Guid flowId, string name, int sortOrder, bool isTerminal)
    {
        Id = id;
        FlowId = flowId;
        Name = name;
        SortOrder = sortOrder;
        IsTerminal = isTerminal;
    }

    internal static Result<FlowState> Create(Flow flow, string name, int sortOrder, bool isTerminal)
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
            isTerminal);
    }
}
