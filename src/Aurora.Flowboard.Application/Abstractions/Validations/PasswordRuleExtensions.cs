namespace Aurora.Flowboard.Application.Abstractions.Validations;

internal static class PasswordRuleExtensions
{
    internal const int MinPasswordLength = 8;
    internal const int MaxPasswordLength = 128;

    internal static IRuleBuilderOptions<T, string> MustBeStrongPassword<T>(
        this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty()
            .MinimumLength(MinPasswordLength)
            .MaximumLength(MaxPasswordLength)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
}
