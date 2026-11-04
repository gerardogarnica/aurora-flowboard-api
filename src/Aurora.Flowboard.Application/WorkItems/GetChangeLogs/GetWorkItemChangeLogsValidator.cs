namespace Aurora.Flowboard.Application.WorkItems.GetChangeLogs;

internal sealed class GetWorkItemChangeLogsValidator : AbstractValidator<GetWorkItemChangeLogsQuery>
{
    public GetWorkItemChangeLogsValidator()
    {
        RuleFor(x => x.WorkItemId)
            .NotEmpty();

        Include(new PagedQueryValidator());
    }
}
