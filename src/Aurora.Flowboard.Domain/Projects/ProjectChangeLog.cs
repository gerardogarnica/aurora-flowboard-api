using Aurora.Flowboard.Domain.Users;

namespace Aurora.Flowboard.Domain.Projects;

public sealed class ProjectChangeLog
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public ProjectChangeType ChangeType { get; private set; }
    public Guid? AffectedEntityId { get; private set; }
    public ProjectStatus? NewStatus { get; private set; }
    public ProjectKind? NewKind { get; private set; }
    public Guid ChangedById { get; private set; }
    public DateTime ChangedOnUtc { get; private set; }

    public User ChangedBy { get; init; } = null!; // Navigation property

    private ProjectChangeLog() { } // EF Core

    private ProjectChangeLog(
        Guid id,
        Guid projectId,
        ProjectChangeType changeType,
        Guid? affectedEntityId,
        ProjectStatus? newStatus,
        ProjectKind? newKind,
        Guid changedById,
        DateTime changedOnUtc)
    {
        Id = id;
        ProjectId = projectId;
        ChangeType = changeType;
        AffectedEntityId = affectedEntityId;
        NewStatus = newStatus;
        NewKind = newKind;
        ChangedById = changedById;
        ChangedOnUtc = changedOnUtc;
    }

    internal static ProjectChangeLog Create(
        Project project,
        User changedBy,
        ProjectChangeType changeType,
        Guid? affectedEntityId,
        DateTime changedOnUtc,
        ProjectStatus? newStatus = null,
        ProjectKind? newKind = null) =>
        new(Guid.NewGuid(), project.Id, changeType, affectedEntityId, newStatus, newKind, changedBy.Id, changedOnUtc);
}
