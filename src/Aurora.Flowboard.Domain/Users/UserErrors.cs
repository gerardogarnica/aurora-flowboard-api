namespace Aurora.Flowboard.Domain.Users;

public static class UserErrors
{
    public static readonly BaseError NotFound = BaseError.NotFound(
        "User.NotFound",
        "The user with the specified identifier was not found");

    public static readonly BaseError FirstNameRequired = BaseError.Validation(
        "User.FirstNameRequired",
        "First name is required");

    public static readonly BaseError LastNameRequired = BaseError.Validation(
        "User.LastNameRequired",
        "Last name is required");

    public static readonly BaseError PasswordHashRequired = BaseError.Validation(
        "User.PasswordHashRequired",
        "Password hash is required");

    public static readonly BaseError AlreadyDeactivated = BaseError.Validation(
        "User.AlreadyDeactivated",
        "The user is already deactivated");

    public static readonly BaseError Inactive = BaseError.Validation(
        "User.Inactive",
        "Cannot perform this operation on an inactive user");

    public static readonly BaseError EmailAlreadyInUse = BaseError.Conflict(
        "User.EmailAlreadyInUse",
        "A user with this email address already exists");
}
