namespace Aurora.Flowboard.Domain.Milestones.Events;

public sealed class MilestoneUpdatedDomainEvent(Guid milestoneId) : DomainEvent
{
    public Guid MilestoneId { get; init; } = milestoneId;
}
