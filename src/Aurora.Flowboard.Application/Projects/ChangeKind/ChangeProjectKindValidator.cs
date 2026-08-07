namespace Aurora.Flowboard.Application.Projects.ChangeKind;

internal sealed class ChangeProjectKindValidator : AbstractValidator<ChangeProjectKindCommand>
{
    public ChangeProjectKindValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.NewKind)
            .IsInEnum();
    }
}
