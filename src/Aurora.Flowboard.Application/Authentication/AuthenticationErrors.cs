namespace Aurora.Flowboard.Application.Authentication;

public static class AuthenticationErrors
{
    public static readonly BaseError InvalidCredentials = BaseError.Unauthorized(
        "Auth.InvalidCredentials",
        "Invalid email or password.");

    public static readonly BaseError InvalidRefreshToken = BaseError.Unauthorized(
        "Auth.InvalidRefreshToken",
        "Invalid or expired refresh token.");

    public static readonly BaseError InvalidCurrentPassword = BaseError.Validation(
        "Auth.InvalidCurrentPassword",
        "The current password is incorrect.");

    public static readonly BaseError SessionChangedConcurrently = BaseError.Conflict(
        "Auth.SessionChangedConcurrently",
        "The user's sessions changed while the request was processed. Try again.");

    public static readonly BaseError NewPasswordMustDiffer = BaseError.Validation(
        "Auth.NewPasswordMustDiffer",
        "The new password must be different from the current password.");
}
