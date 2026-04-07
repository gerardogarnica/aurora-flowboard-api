using Aurora.Flowboard.Domain.Users;

namespace Aurora.Flowboard.Domain.Projects;

public sealed class ProjectChangeLog
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid ChangedById { get; private set; }
    public ProjectChangeType ChangeType { get; private set; }
    public Guid? AffectedEntityId { get; private set; }
    public DateTime ChangedOnUtc { get; private set; }

    private ProjectChangeLog() { } // EF Core

    private ProjectChangeLog(
        Guid id,
        Guid projectId,
        Guid changedById,
        ProjectChangeType changeType,
        Guid? affectedEntityId,
        DateTime changedOnUtc)
    {
        Id = id;
        ProjectId = projectId;
        ChangedById = changedById;
        ChangeType = changeType;
        AffectedEntityId = affectedEntityId;
        ChangedOnUtc = changedOnUtc;
    }

    internal static ProjectChangeLog Create(
        Project project,
        User changedBy,
        ProjectChangeType changeType,
        Guid? affectedEntityId,
        DateTime changedOnUtc) =>
        new(Guid.NewGuid(), project.Id, changedBy.Id, changeType, affectedEntityId, changedOnUtc);
}
