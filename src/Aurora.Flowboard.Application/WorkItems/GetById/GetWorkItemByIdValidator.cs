namespace Aurora.Flowboard.Application.WorkItems.GetById;

internal sealed class GetWorkItemByIdValidator : AbstractValidator<GetWorkItemByIdQuery>
{
    public GetWorkItemByIdValidator()
    {
        RuleFor(x => x.WorkItemId)
            .NotEmpty();
    }
}
