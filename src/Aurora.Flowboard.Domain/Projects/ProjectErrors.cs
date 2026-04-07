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

    public static readonly BaseError CannotRemoveLastAdmin = BaseError.Validation(
        "Project.CannotRemoveLastAdmin",
        "Cannot remove the only admin member of the project");

    public static readonly BaseError OnlyAdminCanRemoveMembers = BaseError.Forbidden(
        "Project.OnlyAdminCanRemoveMembers",
        "Only admin members can remove other members from the project");

    public static readonly BaseError OnlyAdminCanAddMembers = BaseError.Forbidden(
        "Project.OnlyAdminCanAddMembers",
        "Only admin members can add members to the project");

    public static readonly BaseError OnlyAdminCanUpdateProject = BaseError.Forbidden(
        "Project.OnlyAdminCanUpdateProject",
        "Only admin members can update the project");

    public static readonly BaseError OnlyAdminCanChangeStatus = BaseError.Forbidden(
        "Project.OnlyAdminCanChangeStatus",
        "Only admin members can change the project status");

    public static readonly BaseError CodeRequired = BaseError.Validation(
        "Project.CodeRequired",
        "Project code is required");

    public static readonly BaseError CodeTooLong = BaseError.Validation(
        "Project.CodeTooLong",
        "Project code cannot exceed 3 characters");

    public static readonly BaseError CodeInvalidCharacters = BaseError.Validation(
        "Project.CodeInvalidCharacters",
        "Project code must contain only alphabetic characters");
}
