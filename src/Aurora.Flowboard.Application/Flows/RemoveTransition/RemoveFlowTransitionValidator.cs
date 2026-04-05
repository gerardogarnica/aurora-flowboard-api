namespace Aurora.Flowboard.Application.Flows.RemoveTransition;

internal sealed class RemoveFlowTransitionValidator : AbstractValidator<RemoveFlowTransitionCommand>
{
    public RemoveFlowTransitionValidator()
    {
        RuleFor(x => x.FlowId)
            .NotEmpty();

        RuleFor(x => x.TransitionId)
            .NotEmpty();
    }
}
