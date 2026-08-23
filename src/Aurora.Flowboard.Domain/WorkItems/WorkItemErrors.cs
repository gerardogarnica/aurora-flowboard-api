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

    public static readonly BaseError DescriptionTooLong = BaseError.Validation(
        "WorkItem.DescriptionTooLong",
        "Work item description cannot exceed 4000 characters");

    public static readonly BaseError EstimatedPointsInvalid = BaseError.Validation(
        "WorkItem.EstimatedPointsInvalid",
        "Estimated points must be greater than zero");

    public static readonly BaseError AssigneeNotProjectMember = BaseError.Validation(
        "WorkItem.AssigneeNotProjectMember",
        "The assignee must be a member of the project");

    public static readonly BaseError AssigneeNotFound = BaseError.NotFound(
        "WorkItem.AssigneeNotFound",
        "The assignee user was not found");

    public static readonly BaseError AssigneeInactive = BaseError.Validation(
        "WorkItem.AssigneeInactive",
        "The assignee user is inactive");

    public static readonly BaseError CommentNotFound = BaseError.NotFound(
        "WorkItem.CommentNotFound",
        "The comment with the specified identifier was not found");

    public static readonly BaseError CommentContentRequired = BaseError.Validation(
        "WorkItem.CommentContentRequired",
        "Comment content is required");

    public static readonly BaseError TimeEntryHoursInvalid = BaseError.Validation(
        "WorkItem.TimeEntryHoursInvalid",
        "Logged hours must be greater than zero");

    public static readonly BaseError TargetStateNotInProject = BaseError.Validation(
        "WorkItem.TargetStateNotInProject",
        "The target state does not belong to the work item's project");

    public static readonly BaseError TransitionNotAllowed = BaseError.Validation(
        "WorkItem.TransitionNotAllowed",
        "No transition is defined between the current state and the target state");

    public static readonly BaseError TransitionRoleNotAllowed = BaseError.Forbidden(
        "WorkItem.TransitionRoleNotAllowed",
        "The user's project role is not allowed to perform this transition");

    public static readonly BaseError CommentNotOwnedByUser = BaseError.Forbidden(
        "WorkItem.CommentNotOwnedByUser",
        "Only the comment author can edit this comment");

    public static readonly BaseError NotAssigned = BaseError.Conflict(
        "WorkItem.NotAssigned",
        "The work item does not have an assignee");

    public static readonly BaseError TagNotFound = BaseError.NotFound(
        "WorkItem.TagNotFound",
        "The tag with the specified identifier was not found on this work item");

    public static readonly BaseError TagNameRequired = BaseError.Validation(
        "WorkItem.TagNameRequired",
        "Tag name is required");

    public static readonly BaseError TagNameTooLong = BaseError.Validation(
        "WorkItem.TagNameTooLong",
        $"Tag name cannot exceed {WorkItemTag.MaxNameLength} characters");

    public static readonly BaseError DuplicateTagName = BaseError.Conflict(
        "WorkItem.DuplicateTagName",
        "A tag with this name already exists on this work item");

    public static readonly BaseError CancelledWorkItemCannotBeModified = BaseError.Validation(
        "WorkItem.CancelledWorkItemCannotBeModified",
        "A work item in a cancelled state cannot be assigned or unassigned");

    public static readonly BaseError MilestoneNotInProject = BaseError.Validation(
        "WorkItem.MilestoneNotInProject",
        "The milestone must belong to the work item's project");

    public static readonly BaseError MilestoneNotAcceptingAssignments = BaseError.Validation(
        "WorkItem.MilestoneNotAcceptingAssignments",
        "The milestone is completed or archived and does not accept new work item assignments");

    public static readonly BaseError ComponentNotInProject = BaseError.Validation(
        "WorkItem.ComponentNotInProject",
        "The component must belong to the work item's project");

    public static readonly BaseError ComponentRetired = BaseError.Validation(
        "WorkItem.ComponentRetired",
        "The component is retired and cannot be assigned to new work items");
}
