namespace Aurora.Flowboard.Domain.Milestones.Events;

public sealed class MilestoneStatusChangedDomainEvent(
    Guid milestoneId,
    MilestoneStatus oldStatus,
    MilestoneStatus newStatus) : DomainEvent
{
    public Guid MilestoneId { get; init; } = milestoneId;
    public MilestoneStatus OldStatus { get; init; } = oldStatus;
    public MilestoneStatus NewStatus { get; init; } = newStatus;
}
