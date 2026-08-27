namespace Aurora.Flowboard.Application.WorkItems.UpdateDescription;

internal sealed class UpdateWorkItemDescriptionValidator : AbstractValidator<UpdateWorkItemDescriptionCommand>
{
    public UpdateWorkItemDescriptionValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Description)
            .MaximumLength(WorkItem.MaxDescriptionLength)
            .When(x => x.Description is not null);
    }
}
