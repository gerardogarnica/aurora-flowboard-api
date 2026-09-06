namespace Aurora.Flowboard.Application.Abstractions.Validations;

internal static class PaginationRuleExtensions
{
    internal static IRuleBuilderOptions<T, int> MustBeValidPage<T>(
        this IRuleBuilder<T, int> ruleBuilder) =>
        ruleBuilder.GreaterThan(0);

    internal static IRuleBuilderOptions<T, int> MustBeValidPageSize<T>(
        this IRuleBuilder<T, int> ruleBuilder) =>
        ruleBuilder
            .GreaterThan(0)
            .LessThanOrEqualTo(PaginationDefaults.MaxPageSize);
}
