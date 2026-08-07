namespace Aurora.Flowboard.Domain.Projects;

public enum ProjectChangeType
{
    Created = 0,
    Updated = 1,
    KindChanged = 2,
    StatusChanged = 3,
    MemberAdded = 4,
    MemberRemoved = 5,
    ComponentAdded = 6,
    ComponentRenamed = 7,
    ComponentRetired = 8
}
