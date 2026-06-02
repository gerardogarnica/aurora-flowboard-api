namespace Aurora.Flowboard.Application.Projects.Setup;

internal sealed class SetupProjectValidator : AbstractValidator<SetupProjectCommand>
{
    public SetupProjectValidator(IDateTimeProvider dateTimeProvider)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(Project.MaxNameLength);

        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(ProjectCode.MaxLength)
            .Matches("^[A-Za-z]+$");

        RuleFor(x => x.Description)
            .MaximumLength(Project.MaxDescriptionLength);

        RuleFor(x => x.Color)
            .NotEmpty()
            .MaximumLength(Color.MaxLength);

        RuleFor(x => x.EstimatedCompletionDate)
            .GreaterThanOrEqualTo(_ => dateTimeProvider.Today)
            .When(x => x.EstimatedCompletionDate.HasValue);

        RuleFor(x => x.Flow)
            .NotNull();

        RuleFor(x => x.Flow.Name)
            .NotEmpty()
            .MaximumLength(Flow.MaxNameLength)
            .When(x => x.Flow is not null);

        RuleFor(x => x.Flow.Description)
            .MaximumLength(Flow.MaxDescriptionLength)
            .When(x => x.Flow is not null);

        RuleFor(x => x.Flow.States)
            .NotEmpty()
            .When(x => x.Flow is not null);

        RuleFor(x => x.Flow.States)
            .Must(states => states.Any(s => s.Category == FlowStateCategory.Completed))
            .WithMessage("Flow must include at least one Completed state.")
            .When(x => x.Flow?.States is { Count: > 0 });

        RuleFor(x => x.Flow.States)
            .Must(states => states.Any(s => s.Category == FlowStateCategory.Cancelled))
            .WithMessage("Flow must include at least one Cancelled state.")
            .When(x => x.Flow?.States is { Count: > 0 });

        RuleFor(x => x.Flow.States)
            .Must(StatesAreInCategoryOrder)
            .WithMessage("Active states must appear before Completed and Cancelled states, and Completed states must appear before Cancelled states.")
            .When(x => x.Flow?.States is { Count: > 0 });

        RuleForEach(x => x.Flow.States)
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
            .When(x => x.Flow?.States is not null);
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
