namespace Aurora.Flowboard.Application.WorkItems.Create;

internal sealed class CreateWorkItemValidator : AbstractValidator<CreateWorkItemCommand>
{
    public CreateWorkItemValidator(IDateTimeProvider dateTimeProvider)
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(4000);

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
