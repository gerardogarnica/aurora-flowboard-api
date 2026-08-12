namespace Aurora.Flowboard.Domain.Milestones;

public static class MilestoneErrors
{
    public static readonly BaseError NotFound = BaseError.NotFound(
        "Milestone.NotFound",
        "The milestone with the specified identifier was not found");

    public static readonly BaseError NameRequired = BaseError.Validation(
        "Milestone.NameRequired",
        "Milestone name is required");

    public static readonly BaseError NameTooLong = BaseError.Validation(
        "Milestone.NameTooLong",
        "Milestone name cannot exceed 100 characters");

    public static readonly BaseError DescriptionTooLong = BaseError.Validation(
        "Milestone.DescriptionTooLong",
        "Milestone description cannot exceed 500 characters");

    public static readonly BaseError InvalidDateRange = BaseError.Validation(
        "Milestone.InvalidDateRange",
        "Target end date cannot be earlier than target start date");

    public static readonly BaseError DuplicateName = BaseError.Conflict(
        "Milestone.DuplicateName",
        "A milestone with this name already exists in this project");

    public static readonly BaseError InvalidStatusTransition = BaseError.Validation(
        "Milestone.InvalidStatusTransition",
        "The requested status transition is not allowed");

    public static readonly BaseError OperationNotAllowedInCurrentStatus = BaseError.Validation(
        "Milestone.OperationNotAllowedInCurrentStatus",
        "This operation is not allowed in the milestone's current status");

    public static readonly BaseError CannotCloseWithOpenWorkItems = BaseError.Validation(
        "Milestone.CannotCloseWithOpenWorkItems",
        "The milestone still holds open work items and cannot be completed or archived");

    public static readonly BaseError OnlyAdminCanManageMilestone = BaseError.Forbidden(
        "Milestone.OnlyAdminCanManageMilestone",
        "Only admin members can manage milestones in the project");
}
