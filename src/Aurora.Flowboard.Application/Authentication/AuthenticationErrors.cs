namespace Aurora.Flowboard.Application.Authentication;

public static class AuthenticationErrors
{
    public static readonly BaseError InvalidCredentials = BaseError.Forbidden(
        "Auth.InvalidCredentials",
        "Invalid email or password.");

    public static readonly BaseError InvalidRefreshToken = BaseError.Forbidden(
        "Auth.InvalidRefreshToken",
        "Invalid or expired refresh token.");
}
