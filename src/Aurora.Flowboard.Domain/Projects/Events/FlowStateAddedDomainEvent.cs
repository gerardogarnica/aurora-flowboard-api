namespace Aurora.Flowboard.Domain.Projects.Events;

public sealed class FlowStateAddedDomainEvent(Guid projectId, Guid flowStateId) : DomainEvent
{
    public Guid ProjectId { get; init; } = projectId;
    public Guid FlowStateId { get; init; } = flowStateId;
}
