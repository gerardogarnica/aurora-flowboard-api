namespace Aurora.Flowboard.Application.WorkItems.UpdateEstimatedPoints;

internal sealed class UpdateWorkItemEstimatedPointsValidator : AbstractValidator<UpdateWorkItemEstimatedPointsCommand>
{
    public UpdateWorkItemEstimatedPointsValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.EstimatedPoints)
            .GreaterThan(0)
            .When(x => x.EstimatedPoints.HasValue);
    }
}
