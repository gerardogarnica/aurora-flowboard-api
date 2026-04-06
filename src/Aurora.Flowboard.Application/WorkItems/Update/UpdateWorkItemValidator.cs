namespace Aurora.Flowboard.Application.WorkItems.Update;

internal sealed class UpdateWorkItemValidator : AbstractValidator<UpdateWorkItemCommand>
{
    public UpdateWorkItemValidator(IDateTimeProvider dateTimeProvider)
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.ChangedById)
            .NotEmpty();

        RuleFor(x => x.EstimatedPoints)
            .GreaterThan(0)
            .When(x => x.EstimatedPoints.HasValue);

        RuleFor(x => x.EstimatedCompletionDate)
            .GreaterThanOrEqualTo(_ => dateTimeProvider.Today)
            .When(x => x.EstimatedCompletionDate.HasValue);
    }
}
