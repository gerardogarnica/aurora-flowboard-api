namespace Aurora.Flowboard.Application.WorkItems.UpdateTitle;

internal sealed class UpdateWorkItemTitleValidator : AbstractValidator<UpdateWorkItemTitleCommand>
{
    public UpdateWorkItemTitleValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(WorkItem.MaxTitleLength);
    }
}
