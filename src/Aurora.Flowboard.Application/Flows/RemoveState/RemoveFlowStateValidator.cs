namespace Aurora.Flowboard.Application.Flows.RemoveState;

internal sealed class RemoveFlowStateValidator : AbstractValidator<RemoveFlowStateCommand>
{
    public RemoveFlowStateValidator()
    {
        RuleFor(x => x.FlowId)
            .NotEmpty();

        RuleFor(x => x.StateId)
            .NotEmpty();
    }
}
