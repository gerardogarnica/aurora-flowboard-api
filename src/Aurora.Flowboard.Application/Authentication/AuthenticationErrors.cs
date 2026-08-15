namespace Aurora.Flowboard.Application.Authentication;

public static class AuthenticationErrors
{
    public static readonly BaseError InvalidCredentials = BaseError.Forbidden(
        "Auth.InvalidCredentials",
        "Invalid email or password.");

    public static readonly BaseError InvalidRefreshToken = BaseError.Forbidden(
        "Auth.InvalidRefreshToken",
        "Invalid or expired refresh token.");

    public static readonly BaseError NewPasswordMustDiffer = BaseError.Validation(
        "Auth.NewPasswordMustDiffer",
        "The new password must be different from the current password.");
}
