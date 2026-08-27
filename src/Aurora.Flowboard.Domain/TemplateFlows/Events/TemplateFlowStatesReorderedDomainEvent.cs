namespace Aurora.Flowboard.Domain.TemplateFlows.Events;

public sealed class TemplateFlowStatesReorderedDomainEvent(Guid templateFlowId) : DomainEvent
{
    public Guid TemplateFlowId { get; init; } = templateFlowId;
}
