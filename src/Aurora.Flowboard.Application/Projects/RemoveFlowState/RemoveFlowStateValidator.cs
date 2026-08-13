namespace Aurora.Flowboard.Application.Projects.RemoveFlowState;

internal sealed class RemoveFlowStateValidator : AbstractValidator<RemoveFlowStateCommand>
{
    public RemoveFlowStateValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty();

        RuleFor(x => x.StateId)
            .NotEmpty();
    }
}
