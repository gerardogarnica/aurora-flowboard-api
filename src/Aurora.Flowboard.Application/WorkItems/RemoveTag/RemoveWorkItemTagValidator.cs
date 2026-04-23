namespace Aurora.Flowboard.Application.WorkItems.RemoveTag;

internal sealed class RemoveWorkItemTagValidator : AbstractValidator<RemoveWorkItemTagCommand>
{
    public RemoveWorkItemTagValidator()
    {
        RuleFor(x => x.WorkItemId)
            .NotEmpty();

        RuleFor(x => x.TagId)
            .NotEmpty();
    }
}
