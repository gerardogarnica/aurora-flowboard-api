namespace Aurora.Flowboard.Domain.WorkItems;

public enum WorkItemChangeType
{
    Created = 0,
    Updated = 1, // legacy — no longer written; historical rows hold the string 'Updated'
                 // (WorkItemChangeLogConfiguration maps this enum via .HasConversion<string>().HasMaxLength(40);
                 // the longest current value, EstimatedCompletionDateUpdated, is 30 chars, so a future
                 // value over 40 chars would fail at insert time, not at build time)
    Moved = 2,
    Assigned = 3,
    Unassigned = 4,
    CommentAdded = 5,
    CommentUpdated = 6,
    CommentRemoved = 7,
    TimeLogged = 8,
    TagAdded = 9,
    TagRemoved = 10,
    TitleUpdated = 11,
    DescriptionUpdated = 12,
    TypeUpdated = 13,
    PriorityUpdated = 14,
    EstimatedPointsUpdated = 15,
    EstimatedCompletionDateUpdated = 16,
    ComponentChanged = 17,
    MilestoneChanged = 18
}
