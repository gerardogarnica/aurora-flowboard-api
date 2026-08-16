namespace Aurora.Flowboard.Domain.FlowStateTemplates.Events;

public sealed class TemplateFlowStateUpdatedDomainEvent(
    Guid flowStateTemplateId,
    Guid templateFlowStateId) : DomainEvent
{
    public Guid FlowStateTemplateId { get; init; } = flowStateTemplateId;
    public Guid TemplateFlowStateId { get; init; } = templateFlowStateId;
}
