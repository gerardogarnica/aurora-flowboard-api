namespace Aurora.Flowboard.Domain.Users;

public static class UserTokenErrors
{
    public static readonly BaseError NotFound = BaseError.NotFound(
        "UserToken.NotFound",
        "The user token with the specified identifier was not found");

    public static readonly BaseError AlreadyRevoked = BaseError.Validation(
        "UserToken.AlreadyRevoked",
        "The token has already been revoked");

    public static readonly BaseError AccessTokenIdRequired = BaseError.Validation(
        "UserToken.AccessTokenIdRequired",
        "Access token identifier is required");

    public static readonly BaseError RefreshTokenHashRequired = BaseError.Validation(
        "UserToken.RefreshTokenHashRequired",
        "Refresh token hash is required");

    public static readonly BaseError InvalidExpiration = BaseError.Validation(
        "UserToken.InvalidExpiration",
        "Token expiration must be after the issued date");
}
