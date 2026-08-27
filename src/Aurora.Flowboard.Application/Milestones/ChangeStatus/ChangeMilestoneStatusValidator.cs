namespace Aurora.Flowboard.Application.Milestones.ChangeStatus;

internal sealed class ChangeMilestoneStatusValidator : AbstractValidator<ChangeMilestoneStatusCommand>
{
    public ChangeMilestoneStatusValidator()
    {
        RuleFor(x => x.MilestoneId)
            .NotEmpty();

        RuleFor(x => x.NewStatus)
            .IsInEnum();
    }
}
