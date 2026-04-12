namespace Aurora.Flowboard.Application.Flows.RemoveTransitionRole;

internal sealed class RemoveFlowTransitionRoleValidator : AbstractValidator<RemoveFlowTransitionRoleCommand>
{
    public RemoveFlowTransitionRoleValidator()
    {
        RuleFor(x => x.FlowId)
            .NotEmpty();

        RuleFor(x => x.TransitionId)
            .NotEmpty();

        RuleFor(x => x.Role)
            .IsInEnum();
    }
}
