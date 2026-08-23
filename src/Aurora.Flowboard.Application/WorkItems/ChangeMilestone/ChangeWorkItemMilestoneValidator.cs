namespace Aurora.Flowboard.Application.WorkItems.ChangeMilestone;

internal sealed class ChangeWorkItemMilestoneValidator : AbstractValidator<ChangeWorkItemMilestoneCommand>
{
    public ChangeWorkItemMilestoneValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
