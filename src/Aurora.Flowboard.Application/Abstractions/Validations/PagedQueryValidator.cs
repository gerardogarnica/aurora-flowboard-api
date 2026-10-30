namespace Aurora.Flowboard.Application.Abstractions.Validations;

internal sealed class PagedQueryValidator : AbstractValidator<IPagedQuery>
{
    public PagedQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(PaginationDefaults.MaxPageSize);
    }
}
