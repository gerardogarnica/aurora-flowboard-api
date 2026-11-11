namespace Aurora.Flowboard.Application.WorkItems.GetWorkItems;

internal sealed class GetWorkItemsValidator : AbstractValidator<GetWorkItemsQuery>
{
    public GetWorkItemsValidator()
    {
        RuleFor(x => x.Assignee)
            .Must(value => value == WorkItemSearchFilters.MeValue)
            .WithMessage($"Assignee only accepts '{WorkItemSearchFilters.MeValue}'")
            .When(x => x.Assignee is not null);

        RuleFor(x => x.CreatedBy)
            .Must(value => value == WorkItemSearchFilters.MeValue)
            .WithMessage($"CreatedBy only accepts '{WorkItemSearchFilters.MeValue}'")
            .When(x => x.CreatedBy is not null);

        RuleFor(x => x.Stage)
            .Must(value => WorkItemSearchFilters.IsStage(value!))
            .WithMessage($"Stage must be {nameof(FlowStateStage.Backlog)} or {nameof(FlowStateStage.InFlight)}")
            .When(x => x.Stage is not null);

        RuleFor(x => x.Stalled)
            .Must(value => value == WorkItemSearchFilters.TrueValue)
            .WithMessage($"Stalled only accepts '{WorkItemSearchFilters.TrueValue}'")
            .When(x => x.Stalled is not null);

        RuleFor(x => x.Priority)
            .Must(value => WorkItemSearchFilters.IsPriorityList(value!))
            .WithMessage("Priority must be a comma-separated list of Low, Medium, High or Critical")
            .When(x => x.Priority is not null);

        RuleFor(x => x.Sort)
            .Must(value => value == WorkItemSearchFilters.DefaultSort)
            .WithMessage($"Sort only accepts '{WorkItemSearchFilters.DefaultSort}'")
            .When(x => x.Sort is not null);

        Include(new PagedQueryValidator());
    }
}
