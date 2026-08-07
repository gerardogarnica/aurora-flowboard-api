namespace Aurora.Flowboard.Domain.Projects.Events;

public sealed class ProjectKindChangedDomainEvent(
    Guid projectId,
    ProjectKind oldKind,
    ProjectKind newKind) : DomainEvent
{
    public Guid ProjectId { get; init; } = projectId;
    public ProjectKind OldKind { get; init; } = oldKind;
    public ProjectKind NewKind { get; init; } = newKind;
}
