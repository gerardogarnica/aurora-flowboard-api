namespace Aurora.Flowboard.Application.Flows.Update;

internal sealed class UpdateFlowValidator : AbstractValidator<UpdateFlowCommand>
{
    public UpdateFlowValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(Flow.MaxNameLength);

        RuleFor(x => x.Description)
            .MaximumLength(Flow.MaxDescriptionLength);
    }
}
