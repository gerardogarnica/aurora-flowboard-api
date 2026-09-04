namespace Aurora.Flowboard.Application.WorkItems.GetTimeEntries;

internal sealed class GetWorkItemTimeEntriesValidator : AbstractValidator<GetWorkItemTimeEntriesQuery>
{
    public GetWorkItemTimeEntriesValidator()
    {
        RuleFor(x => x.WorkItemId)
            .NotEmpty();

        RuleFor(x => x.Page)
            .MustBeValidPage();

        RuleFor(x => x.PageSize)
            .MustBeValidPageSize();
    }
}
