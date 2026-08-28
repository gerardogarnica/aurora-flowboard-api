namespace Aurora.Flowboard.Application.Authentication.RefreshToken;

internal sealed class RefreshTokenValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .MaximumLength(UserToken.MaxRefreshTokenLength);
    }
}
