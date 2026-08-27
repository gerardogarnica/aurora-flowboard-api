namespace Aurora.Flowboard.Application.Components.Retire;

internal sealed class RetireComponentValidator : AbstractValidator<RetireComponentCommand>
{
    public RetireComponentValidator()
    {
        RuleFor(x => x.ComponentId)
            .NotEmpty();
    }
}
