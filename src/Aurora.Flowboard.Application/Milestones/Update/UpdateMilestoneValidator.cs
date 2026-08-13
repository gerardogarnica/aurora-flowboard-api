namespace Aurora.Flowboard.Application.Milestones.Update;

internal sealed class UpdateMilestoneValidator : AbstractValidator<UpdateMilestoneCommand>
{
    public UpdateMilestoneValidator()
    {
        RuleFor(x => x.MilestoneId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(Milestone.MaxNameLength);

        RuleFor(x => x.Description)
            .MaximumLength(Milestone.MaxDescriptionLength);

        RuleFor(x => x.TargetEndDate)
            .Must((command, targetEndDate) =>
                targetEndDate is null ||
                command.TargetStartDate is null ||
                targetEndDate >= command.TargetStartDate)
            .WithMessage("Target end date cannot be earlier than target start date");
    }
}
