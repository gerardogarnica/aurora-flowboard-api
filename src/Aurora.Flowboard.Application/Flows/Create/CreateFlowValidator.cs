namespace Aurora.Flowboard.Application.Flows.Create;

internal sealed class CreateFlowValidator : AbstractValidator<CreateFlowCommand>
{
    public CreateFlowValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500);
    }
}
