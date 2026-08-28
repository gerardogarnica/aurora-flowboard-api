namespace Aurora.Flowboard.Application.WorkItems.UpdatePriority;

internal sealed class UpdateWorkItemPriorityValidator : AbstractValidator<UpdateWorkItemPriorityCommand>
{
    public UpdateWorkItemPriorityValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Priority)
            .IsInEnum();
    }
}
