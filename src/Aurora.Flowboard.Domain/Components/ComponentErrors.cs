namespace Aurora.Flowboard.Domain.Components;

public static class ComponentErrors
{
    public static readonly BaseError NotFound = BaseError.NotFound(
        "Component.NotFound",
        "The component with the specified identifier was not found");

    public static readonly BaseError NameRequired = BaseError.Validation(
        "Component.NameRequired",
        "Component name is required");

    public static readonly BaseError NameTooLong = BaseError.Validation(
        "Component.NameTooLong",
        "Component name cannot exceed 50 characters");

    public static readonly BaseError AlreadyRetired = BaseError.Validation(
        "Component.AlreadyRetired",
        "The component is already retired");

    public static readonly BaseError DuplicateName = BaseError.Conflict(
        "Component.DuplicateName",
        "A component with this name already exists in this project");

    public static readonly BaseError OnlyAdminCanManageComponent = BaseError.Forbidden(
        "Component.OnlyAdminCanManageComponent",
        "Only admin members can manage components in the project");
}
