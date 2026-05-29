namespace Aurora.Flowboard.Domain.Users;

public static class RoleErrors
{
    public static readonly BaseError NotFound = BaseError.Validation(
        "Role.NotFound",
        "The specified role does not exist");
}
