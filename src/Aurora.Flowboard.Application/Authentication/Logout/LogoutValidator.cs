namespace Aurora.Flowboard.Application.Authentication.Logout;

internal sealed class LogoutValidator : AbstractValidator<LogoutCommand>
{
    public LogoutValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .MaximumLength(UserToken.MaxRefreshTokenLength);
    }
}
