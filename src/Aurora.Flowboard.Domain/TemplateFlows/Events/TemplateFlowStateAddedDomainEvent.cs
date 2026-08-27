namespace Aurora.Flowboard.Domain.TemplateFlows.Events;

public sealed class TemplateFlowStateAddedDomainEvent(
    Guid templateFlowId,
    Guid templateFlowStateId) : DomainEvent
{
    public Guid TemplateFlowId { get; init; } = templateFlowId;
    public Guid TemplateFlowStateId { get; init; } = templateFlowStateId;
}
