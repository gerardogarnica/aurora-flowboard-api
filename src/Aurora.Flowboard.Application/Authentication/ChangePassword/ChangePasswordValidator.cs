namespace Aurora.Flowboard.Application.Authentication.ChangePassword;

internal sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty();

        RuleFor(x => x.NewPassword)
            .MustBeStrongPassword()
            .NotEqual(x => x.CurrentPassword)
            .WithBaseError(AuthenticationErrors.NewPasswordMustDiffer);
    }
}
