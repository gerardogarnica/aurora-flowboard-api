namespace Aurora.Flowboard.Application.Projects.Setup;

internal sealed class SetupProjectValidator : AbstractValidator<SetupProjectCommand>
{
    public SetupProjectValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(Project.MaxNameLength);

        RuleFor(x => x.Prefix)
            .NotEmpty()
            .MaximumLength(ProjectCode.MaxLength)
            .Matches("^[A-Za-z]+$");

        RuleFor(x => x.Kind)
            .IsInEnum();

        RuleFor(x => x.Description)
            .MaximumLength(Project.MaxDescriptionLength);

        RuleFor(x => x.Color)
            .NotEmpty()
            .MaximumLength(Color.MaxLength);

        RuleFor(x => x.FlowStates)
            .NotEmpty();

        RuleFor(x => x.FlowStates)
            .Must(states => states.Any(s => s.Category == FlowStateCategory.Completed))
            .WithMessage("Flow must include at least one Completed state.")
            .When(x => x.FlowStates is { Count: > 0 });

        RuleFor(x => x.FlowStates)
            .Must(states => states.Any(s => s.Category == FlowStateCategory.Cancelled))
            .WithMessage("Flow must include at least one Cancelled state.")
            .When(x => x.FlowStates is { Count: > 0 });

        RuleFor(x => x.FlowStates)
            .Must(StatesAreInCategoryOrder)
            .WithMessage("Active states must appear before Completed and Cancelled states, and Completed states must appear before Cancelled states.")
            .When(x => x.FlowStates is { Count: > 0 });

        RuleForEach(x => x.FlowStates)
            .ChildRules(state =>
            {
                state.RuleFor(s => s.Name)
                    .NotEmpty()
                    .MaximumLength(FlowState.MaxNameLength);

                state.RuleFor(s => s.Category)
                    .IsInEnum();

                state.RuleFor(s => s.Color)
                    .NotEmpty()
                    .MaximumLength(Color.MaxLength);

                state.RuleFor(s => s.Roles)
                    .NotEmpty();
            })
            .When(x => x.FlowStates is not null);
    }

    private static bool StatesAreInCategoryOrder(IReadOnlyCollection<SetupProjectFlowStateDto> states)
    {
        bool seenCompleted = false;
        bool seenCancelled = false;

        foreach (SetupProjectFlowStateDto state in states)
        {
            switch (state.Category)
            {
                case FlowStateCategory.Active:
                    if (seenCompleted || seenCancelled)
                    {
                        return false;
                    }

                    break;

                case FlowStateCategory.Completed:
                    if (seenCancelled)
                    {
                        return false;
                    }

                    seenCompleted = true;
                    break;

                case FlowStateCategory.Cancelled:
                    seenCancelled = true;
                    break;

                default:
                    return false;
            }
        }

        return true;
    }
}
