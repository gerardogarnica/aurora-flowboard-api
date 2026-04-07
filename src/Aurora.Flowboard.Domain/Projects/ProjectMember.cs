using Aurora.Flowboard.Domain.Users;

namespace Aurora.Flowboard.Domain.Projects;

public sealed class ProjectMember
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid UserId { get; private set; }
    public ProjectRole Role { get; private set; }
    public DateTime JoinedOnUtc { get; private set; }

    public User User { get; init; } = null!; // Navigation property

    private ProjectMember() { } // EF Core

    private ProjectMember(Guid id, Guid projectId, Guid userId, ProjectRole role, DateTime joinedOnUtc)
    {
        Id = id;
        ProjectId = projectId;
        UserId = userId;
        Role = role;
        JoinedOnUtc = joinedOnUtc;
    }

    internal static ProjectMember Create(Guid projectId, Guid userId, ProjectRole role, DateTime joinedOnUtc) =>
        new(Guid.NewGuid(), projectId, userId, role, joinedOnUtc);
}
