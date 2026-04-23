namespace Aurora.Flowboard.Application.Flows.AddState;

internal sealed class AddFlowStateValidator : AbstractValidator<AddFlowStateCommand>
{
    public AddFlowStateValidator()
    {
        RuleFor(x => x.FlowId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(FlowState.MaxNameLength);
    }
}
