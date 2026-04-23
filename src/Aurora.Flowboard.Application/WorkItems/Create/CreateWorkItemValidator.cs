namespace Aurora.Flowboard.Application.WorkItems.Create;

internal sealed class CreateWorkItemValidator : AbstractValidator<CreateWorkItemCommand>
{
    public CreateWorkItemValidator(IDateTimeProvider dateTimeProvider)
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(WorkItem.MaxTitleLength);

        RuleFor(x => x.Description)
            .MaximumLength(WorkItem.MaxDescriptionLength)
            .When(x => x.Description is not null);

        RuleFor(x => x.ProjectId)
            .NotEmpty();

        RuleFor(x => x.FlowId)
            .NotEmpty();

        RuleFor(x => x.AssigneeId)
            .NotEmpty()
            .When(x => x.AssigneeId.HasValue);

        RuleFor(x => x.EstimatedPoints)
            .GreaterThan(0)
            .When(x => x.EstimatedPoints.HasValue);

        RuleFor(x => x.EstimatedCompletionDate)
            .GreaterThanOrEqualTo(_ => dateTimeProvider.Today)
            .When(x => x.EstimatedCompletionDate.HasValue);
    }
}
