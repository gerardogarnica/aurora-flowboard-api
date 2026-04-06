namespace Aurora.Flowboard.Domain.Projects.Events;

public sealed class ProjectStatusChangedDomainEvent(
    Guid projectId,
    ProjectStatus oldStatus,
    ProjectStatus newStatus) : DomainEvent
{
    public Guid ProjectId { get; init; } = projectId;
    public ProjectStatus OldStatus { get; init; } = oldStatus;
    public ProjectStatus NewStatus { get; init; } = newStatus;
}
