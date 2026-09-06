namespace Aurora.Flowboard.Application.WorkItems.GetChangeLogs;

internal sealed class GetWorkItemChangeLogsValidator : AbstractValidator<GetWorkItemChangeLogsQuery>
{
    public GetWorkItemChangeLogsValidator()
    {
        RuleFor(x => x.WorkItemId)
            .NotEmpty();

        RuleFor(x => x.Page)
            .MustBeValidPage();

        RuleFor(x => x.PageSize)
            .MustBeValidPageSize();
    }
}
