namespace Aurora.Flowboard.Domain.WorkItems;

public enum WorkItemChangeType
{
    Created = 0,
    Updated = 1,
    Moved = 2,
    Assigned = 3,
    Unassigned = 4,
    CommentAdded = 5,
    CommentUpdated = 6,
    CommentRemoved = 7,
    TimeLogged = 8,
    TagAdded = 9,
    TagRemoved = 10,
    TitleUpdated = 11
}
