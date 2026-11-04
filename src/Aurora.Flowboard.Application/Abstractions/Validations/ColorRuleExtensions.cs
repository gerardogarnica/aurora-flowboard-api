namespace Aurora.Flowboard.Application.Abstractions.Validations;

internal static class ColorRuleExtensions
{
    internal static IRuleBuilderOptions<T, string> MustBeValidColor<T>(
        this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty()
            .MaximumLength(Color.MaxLength);
}
