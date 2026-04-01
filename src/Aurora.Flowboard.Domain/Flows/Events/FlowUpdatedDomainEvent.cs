namespace Aurora.Flowboard.Domain.Flows.Events;

public sealed class FlowUpdatedDomainEvent(Guid flowId) : DomainEvent
{
    public Guid FlowId { get; init; } = flowId;
}
