namespace Aurora.Flowboard.Application.WorkItems.GetByCode;

internal sealed class GetWorkItemByCodeValidator : AbstractValidator<GetWorkItemByCodeQuery>
{
    public GetWorkItemByCodeValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(WorkItem.MaxCodeLength);
    }
}
