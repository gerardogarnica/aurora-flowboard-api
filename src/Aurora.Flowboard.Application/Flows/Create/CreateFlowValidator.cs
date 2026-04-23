namespace Aurora.Flowboard.Application.Flows.Create;

internal sealed class CreateFlowValidator : AbstractValidator<CreateFlowCommand>
{
    public CreateFlowValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(Flow.MaxNameLength);

        RuleFor(x => x.Description)
            .MaximumLength(Flow.MaxDescriptionLength);
    }
}
