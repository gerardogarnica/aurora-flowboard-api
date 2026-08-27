namespace Aurora.Flowboard.Application.Users.GetMySummary;

internal sealed class GetMySummaryValidator : AbstractValidator<GetMySummaryQuery>
{
    public GetMySummaryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");
    }
}
