namespace Aurora.Flowboard.Application.WorkItems.GetComments;

internal sealed class GetWorkItemCommentsValidator : AbstractValidator<GetWorkItemCommentsQuery>
{
    public GetWorkItemCommentsValidator()
    {
        RuleFor(x => x.WorkItemId)
            .NotEmpty();

        Include(new PagedQueryValidator());
    }
}
