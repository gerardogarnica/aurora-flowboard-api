namespace Aurora.Flowboard.Domain.FlowStateTemplates.Events;

public sealed class TemplateFlowStatesReorderedDomainEvent(Guid flowStateTemplateId) : DomainEvent
{
    public Guid FlowStateTemplateId { get; init; } = flowStateTemplateId;
}
