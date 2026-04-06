namespace Aurora.Flowboard.Domain.WorkItems;

public static class WorkItemErrors
{
    public static readonly BaseError NotFound = BaseError.NotFound(
        "WorkItem.NotFound",
        "The work item with the specified identifier was not found");

    public static readonly BaseError TitleRequired = BaseError.Validation(
        "WorkItem.TitleRequired",
        "Work item title is required");

    public static readonly BaseError TitleTooLong = BaseError.Validation(
        "WorkItem.TitleTooLong",
        "Work item title cannot exceed 200 characters");

    public static readonly BaseError AlreadyDeactivated = BaseError.Validation(
        "WorkItem.AlreadyDeactivated",
        "The work item is already deactivated");

    public static readonly BaseError Deactivated = BaseError.Validation(
        "WorkItem.Deactivated",
        "Cannot perform this operation on a deactivated work item");

    public static readonly BaseError AssigneeNotFound = BaseError.NotFound(
        "WorkItem.AssigneeNotFound",
        "The assignee user was not found");

    public static readonly BaseError CommentNotFound = BaseError.NotFound(
        "WorkItem.CommentNotFound",
        "The comment with the specified identifier was not found");

    public static readonly BaseError CommentContentRequired = BaseError.Validation(
        "WorkItem.CommentContentRequired",
        "Comment content is required");

    public static readonly BaseError TimeEntryHoursInvalid = BaseError.Validation(
        "WorkItem.TimeEntryHoursInvalid",
        "Logged hours must be greater than zero");
}
