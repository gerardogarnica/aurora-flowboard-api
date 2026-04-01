namespace Aurora.Flowboard.Domain.Flows.Events;

public sealed class FlowCreatedDomainEvent(Guid flowId) : DomainEvent
{
    public Guid FlowId { get; init; } = flowId;
}
