namespace Aurora.Flowboard.Application.Flows.AddTransitionRole;

internal sealed class AddFlowTransitionRoleValidator : AbstractValidator<AddFlowTransitionRoleCommand>
{
    public AddFlowTransitionRoleValidator()
    {
        RuleFor(x => x.FlowId)
            .NotEmpty();

        RuleFor(x => x.TransitionId)
            .NotEmpty();

        RuleFor(x => x.Role)
            .IsInEnum();
    }
}
