namespace Aurora.Flowboard.Application.WorkItems.UpdateEstimatedCompletionDate;

internal sealed class UpdateWorkItemEstimatedCompletionDateValidator
    : AbstractValidator<UpdateWorkItemEstimatedCompletionDateCommand>
{
    public UpdateWorkItemEstimatedCompletionDateValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
