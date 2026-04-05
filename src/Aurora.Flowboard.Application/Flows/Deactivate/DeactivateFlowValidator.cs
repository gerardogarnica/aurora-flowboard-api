namespace Aurora.Flowboard.Application.Flows.Deactivate;

internal sealed class DeactivateFlowValidator : AbstractValidator<DeactivateFlowCommand>
{
    public DeactivateFlowValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
