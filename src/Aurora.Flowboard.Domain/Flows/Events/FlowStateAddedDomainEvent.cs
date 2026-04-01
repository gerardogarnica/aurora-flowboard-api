namespace Aurora.Flowboard.Domain.Flows.Events;

public sealed class FlowStateAddedDomainEvent(Guid flowId, Guid stateId) : DomainEvent
{
    public Guid FlowId { get; init; } = flowId;
    public Guid StateId { get; init; } = stateId;
}
