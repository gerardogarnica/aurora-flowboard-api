namespace Aurora.Flowboard.Domain.WorkItems;

public enum WorkItemChangeType
{
    Created = 0,
    Updated = 1,
    Moved = 2,
    Assigned = 3,
    Unassigned = 4,
    CommentAdded = 5,
    TimeLogged = 6,
    Deactivated = 7
}
