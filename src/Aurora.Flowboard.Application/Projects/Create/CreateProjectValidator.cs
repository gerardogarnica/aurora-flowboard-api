namespace Aurora.Flowboard.Application.Projects.Create;

internal sealed class CreateProjectValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectValidator(IDateTimeProvider dateTimeProvider)
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
    }
}
