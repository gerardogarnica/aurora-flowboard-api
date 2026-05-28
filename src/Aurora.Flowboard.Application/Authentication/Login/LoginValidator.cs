namespace Aurora.Flowboard.Application.Authentication.Login;

internal sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(Email.MaxLength);

        RuleFor(x => x.Password)
            .NotEmpty();
    }
}
