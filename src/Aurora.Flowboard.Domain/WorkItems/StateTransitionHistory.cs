using Aurora.Flowboard.Domain.Flows;
using Aurora.Flowboard.Domain.Users;

namespace Aurora.Flowboard.Domain.WorkItems;

public sealed class StateTransitionHistory
{
    public Guid Id { get; private set; }
    public Guid WorkItemId { get; private set; }
    public Guid? FromStateId { get; private set; }
    public Guid ToStateId { get; private set; }
    public Guid ChangedById { get; private set; }
    public string? Reason { get; private set; }
    public DateTime ChangedOnUtc { get; private set; }

    private StateTransitionHistory() { } // EF Core

    private StateTransitionHistory(
        Guid id,
        Guid workItemId,
        Guid? fromStateId,
        Guid toStateId,
        Guid changedById,
        string? reason,
        DateTime changedOnUtc)
    {
        Id = id;
        WorkItemId = workItemId;
        FromStateId = fromStateId;
        ToStateId = toStateId;
        ChangedById = changedById;
        Reason = reason;
        ChangedOnUtc = changedOnUtc;
    }

    internal static StateTransitionHistory Create(
        WorkItem workItem,
        FlowState? fromState,
        FlowState toState,
        User changedBy,
        string? reason,
        DateTime changedOnUtc) =>
        new(Guid.NewGuid(), workItem.Id, fromState?.Id, toState.Id, changedBy.Id, reason, changedOnUtc);
}
