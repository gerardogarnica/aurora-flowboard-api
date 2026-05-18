namespace Aurora.Flowboard.Domain.Users;

public static class UserTokenErrors
{
    public static readonly BaseError NotFound = BaseError.NotFound(
        "UserToken.NotFound",
        "The user token with the specified identifier was not found");

    public static readonly BaseError AlreadyRevoked = BaseError.Validation(
        "UserToken.AlreadyRevoked",
        "The token has already been revoked");

    public static readonly BaseError AccessTokenRequired = BaseError.Validation(
        "UserToken.AccessTokenRequired",
        "Access token is required");

    public static readonly BaseError RefreshTokenRequired = BaseError.Validation(
        "UserToken.RefreshTokenRequired",
        "Refresh token is required");

    public static readonly BaseError InvalidExpiration = BaseError.Validation(
        "UserToken.InvalidExpiration",
        "Token expiration must be after the issued date");
}
