namespace Aurora.Flowboard.Domain.TemplateFlows.Events;

public sealed class TemplateFlowStateUpdatedDomainEvent(
    Guid templateFlowId,
    Guid templateFlowStateId) : DomainEvent
{
    public Guid TemplateFlowId { get; init; } = templateFlowId;
    public Guid TemplateFlowStateId { get; init; } = templateFlowStateId;
}
