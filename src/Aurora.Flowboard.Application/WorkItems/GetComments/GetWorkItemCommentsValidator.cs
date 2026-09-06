namespace Aurora.Flowboard.Application.WorkItems.GetComments;

internal sealed class GetWorkItemCommentsValidator : AbstractValidator<GetWorkItemCommentsQuery>
{
    public GetWorkItemCommentsValidator()
    {
        RuleFor(x => x.WorkItemId)
            .NotEmpty();

        RuleFor(x => x.Page)
            .MustBeValidPage();

        RuleFor(x => x.PageSize)
            .MustBeValidPageSize();
    }
}
