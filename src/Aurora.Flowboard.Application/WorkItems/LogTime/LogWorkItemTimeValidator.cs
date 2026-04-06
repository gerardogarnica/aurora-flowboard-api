namespace Aurora.Flowboard.Application.WorkItems.LogTime;

internal sealed class LogWorkItemTimeValidator : AbstractValidator<LogWorkItemTimeCommand>
{
    public LogWorkItemTimeValidator(IDateTimeProvider dateTimeProvider)
    {
        RuleFor(x => x.WorkItemId)
            .NotEmpty();

        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Hours)
            .GreaterThan(0)
            .PrecisionScale(9, 2, true);

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .When(x => x.Description is not null);

        RuleFor(x => x.LoggedOnUtc)
            .NotEmpty()
            .LessThanOrEqualTo(_ => dateTimeProvider.UtcNow);
    }
}
