namespace Aurora.Flowboard.Application.Projects.Deactivate;

internal sealed class DeactivateProjectValidator : AbstractValidator<DeactivateProjectCommand>
{
    public DeactivateProjectValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
