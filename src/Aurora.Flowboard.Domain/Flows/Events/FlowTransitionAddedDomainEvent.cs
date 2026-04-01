namespace Aurora.Flowboard.Domain.Flows.Events;

public sealed class FlowTransitionAddedDomainEvent(Guid flowId, Guid fromStateId, Guid toStateId) : DomainEvent
{
    public Guid FlowId { get; init; } = flowId;
    public Guid FromStateId { get; init; } = fromStateId;
    public Guid ToStateId { get; init; } = toStateId;
}
