namespace Aurora.Flowboard.Application.WorkItems.GetStateHistory;

internal sealed class GetWorkItemStateHistoryValidator : AbstractValidator<GetWorkItemStateHistoryQuery>
{
    public GetWorkItemStateHistoryValidator()
    {
        RuleFor(x => x.WorkItemId)
            .NotEmpty();

        Include(new PagedQueryValidator());
    }
}
