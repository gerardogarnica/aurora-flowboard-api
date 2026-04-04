namespace Aurora.Flowboard.Application.Projects.DeactivateProject;

internal sealed class DeactivateProjectValidator : AbstractValidator<DeactivateProjectCommand>
{
    public DeactivateProjectValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
