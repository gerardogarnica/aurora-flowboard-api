namespace Aurora.Flowboard.Domain.Milestones.Events;

public sealed class MilestoneCreatedDomainEvent(
    Guid milestoneId,
    Guid projectId) : DomainEvent
{
    public Guid MilestoneId { get; init; } = milestoneId;
    public Guid ProjectId { get; init; } = projectId;
}
