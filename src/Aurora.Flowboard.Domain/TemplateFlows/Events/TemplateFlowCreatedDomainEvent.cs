using Aurora.Flowboard.Domain.Projects;

namespace Aurora.Flowboard.Domain.TemplateFlows.Events;

public sealed class TemplateFlowCreatedDomainEvent(
    Guid templateFlowId,
    ProjectKind kind) : DomainEvent
{
    public Guid TemplateFlowId { get; init; } = templateFlowId;
    public ProjectKind Kind { get; init; } = kind;
}
