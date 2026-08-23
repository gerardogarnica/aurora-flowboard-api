namespace Aurora.Flowboard.Application.WorkItems.UpdateEstimatedCompletionDate;

internal sealed class UpdateWorkItemEstimatedCompletionDateValidator
    : AbstractValidator<UpdateWorkItemEstimatedCompletionDateCommand>
{
    public UpdateWorkItemEstimatedCompletionDateValidator(IDateTimeProvider dateTimeProvider)
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.EstimatedCompletionDate)
            .GreaterThanOrEqualTo(_ => dateTimeProvider.Today)
            .When(x => x.EstimatedCompletionDate.HasValue);
    }
}
