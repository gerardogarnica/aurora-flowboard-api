namespace Aurora.Flowboard.Application.Projects.Update;

internal sealed class UpdateProjectValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectValidator(IDateTimeProvider dateTimeProvider)
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500);

        RuleFor(x => x.EstimatedCompletionDate)
            .GreaterThanOrEqualTo(dateTimeProvider.Today)
            .When(x => x.EstimatedCompletionDate.HasValue);
    }
}
