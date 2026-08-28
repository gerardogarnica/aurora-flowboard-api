namespace Aurora.Flowboard.Application.Projects.AddFlowState;

internal sealed class AddFlowStateValidator : AbstractValidator<AddFlowStateCommand>
{
    public AddFlowStateValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(FlowState.MaxNameLength);

        RuleFor(x => x.Color)
            .NotEmpty()
            .MaximumLength(Color.MaxLength);
    }
}
