namespace Aurora.Flowboard.Application.Components.Create;

internal sealed class CreateComponentValidator : AbstractValidator<CreateComponentCommand>
{
    public CreateComponentValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(Component.MaxNameLength);
    }
}
