namespace Aurora.Flowboard.Application.WorkItems.AddTag;

internal sealed class AddWorkItemTagValidator : AbstractValidator<AddWorkItemTagCommand>
{
    public AddWorkItemTagValidator()
    {
        RuleFor(x => x.WorkItemId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(WorkItemTag.MaxNameLength);
    }
}
