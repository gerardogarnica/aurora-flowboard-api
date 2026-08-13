namespace Aurora.Flowboard.Application.Projects.AddFlowTransitionRole;

internal sealed class AddFlowTransitionRoleValidator : AbstractValidator<AddFlowTransitionRoleCommand>
{
    public AddFlowTransitionRoleValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty();

        RuleFor(x => x.TransitionId)
            .NotEmpty();

        RuleFor(x => x.Role)
            .IsInEnum();
    }
}
