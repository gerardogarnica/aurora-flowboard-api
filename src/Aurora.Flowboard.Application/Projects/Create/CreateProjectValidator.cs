namespace Aurora.Flowboard.Application.Projects.Create;

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
            .GreaterThanOrEqualTo(_ => dateTimeProvider.Today)
            .When(x => x.EstimatedCompletionDate.HasValue);

        RuleFor(x => x.Members)
            .NotNull();

        RuleForEach(x => x.Members)
            .ChildRules(member =>
            {
                member.RuleFor(m => m.UserId).NotEmpty();
                member.RuleFor(m => m.Role).IsInEnum();
            });
    }
}
