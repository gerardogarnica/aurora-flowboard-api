namespace Aurora.Flowboard.Application.Components.Rename;

internal sealed class RenameComponentValidator : AbstractValidator<RenameComponentCommand>
{
    public RenameComponentValidator()
    {
        RuleFor(x => x.ComponentId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(Component.MaxNameLength);
    }
}
