namespace Aurora.Flowboard.Domain.Projects.Events;

public sealed class FlowTransitionAddedDomainEvent(Guid projectId, Guid fromStateId, Guid toStateId) : DomainEvent
{
    public Guid ProjectId { get; init; } = projectId;
    public Guid FromStateId { get; init; } = fromStateId;
    public Guid ToStateId { get; init; } = toStateId;
}
