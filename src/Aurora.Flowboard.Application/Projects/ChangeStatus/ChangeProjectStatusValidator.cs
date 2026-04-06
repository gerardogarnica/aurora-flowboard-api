namespace Aurora.Flowboard.Application.Projects.ChangeStatus;

internal sealed class ChangeProjectStatusValidator : AbstractValidator<ChangeProjectStatusCommand>
{
    public ChangeProjectStatusValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.NewStatus)
            .IsInEnum();
    }
}
