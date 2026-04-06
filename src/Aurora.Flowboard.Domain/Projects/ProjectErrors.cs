namespace Aurora.Flowboard.Domain.Projects;

public static class ProjectErrors
{
    public static readonly BaseError NotFound = BaseError.NotFound(
        "Project.NotFound",
        "The project with the specified identifier was not found");

    public static readonly BaseError NameRequired = BaseError.Validation(
        "Project.NameRequired",
        "Project name is required");

    public static readonly BaseError NameTooLong = BaseError.Validation(
        "Project.NameTooLong",
        "Project name cannot exceed 100 characters");

    public static readonly BaseError InvalidStatusTransition = BaseError.Validation(
        "Project.InvalidStatusTransition",
        "The requested status transition is not allowed");

    public static readonly BaseError OperationNotAllowedInCurrentStatus = BaseError.Validation(
        "Project.OperationNotAllowedInCurrentStatus",
        "This operation is not allowed in the project's current status");

    public static readonly BaseError MemberAlreadyExists = BaseError.Conflict(
        "Project.MemberAlreadyExists",
        "The user is already a member of this project");

    public static readonly BaseError MemberNotFound = BaseError.NotFound(
        "Project.MemberNotFound",
        "The user is not a member of this project");
}
