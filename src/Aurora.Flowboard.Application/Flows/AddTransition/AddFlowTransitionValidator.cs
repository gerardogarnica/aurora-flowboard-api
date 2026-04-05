namespace Aurora.Flowboard.Application.Flows.AddTransition;

internal sealed class AddFlowTransitionValidator : AbstractValidator<AddFlowTransitionCommand>
{
    private const string SameStateTransitionMessage = "Transition cannot point to the same state";

    public AddFlowTransitionValidator()
    {
        RuleFor(x => x.FlowId)
            .NotEmpty();

        RuleFor(x => x.FromStateId)
            .NotEmpty();

        RuleFor(x => x.ToStateId)
            .NotEmpty()
            .NotEqual(x => x.FromStateId)
            .WithMessage(SameStateTransitionMessage);
    }
}
