namespace Aurora.Flowboard.Application.Projects.CreateProject;

internal sealed class CreateProjectValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectValidator(IDateTimeProvider dateTimeProvider)
    {
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
