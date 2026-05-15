namespace Aurora.Flowboard.Domain.Flows.Events;

public sealed class FlowDeactivatedDomainEvent(Guid flowId) : DomainEvent
{
    public Guid FlowId { get; init; } = flowId;
}
