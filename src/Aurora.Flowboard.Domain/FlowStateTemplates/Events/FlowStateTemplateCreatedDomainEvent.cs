using Aurora.Flowboard.Domain.Projects;

namespace Aurora.Flowboard.Domain.FlowStateTemplates.Events;

public sealed class FlowStateTemplateCreatedDomainEvent(
    Guid flowStateTemplateId,
    ProjectKind kind) : DomainEvent
{
    public Guid FlowStateTemplateId { get; init; } = flowStateTemplateId;
    public ProjectKind Kind { get; init; } = kind;
}
