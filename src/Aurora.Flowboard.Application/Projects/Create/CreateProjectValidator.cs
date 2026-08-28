namespace Aurora.Flowboard.Application.Projects.Create;

internal sealed class CreateProjectValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(Project.MaxNameLength);

        RuleFor(x => x.Prefix)
            .NotEmpty()
            .MaximumLength(ProjectCode.MaxLength)
            .Matches("^[A-Za-z]+$");

        RuleFor(x => x.Kind)
            .IsInEnum();

        RuleFor(x => x.Description)
            .MaximumLength(Project.MaxDescriptionLength);

        RuleFor(x => x.Color)
            .NotEmpty()
            .MaximumLength(Color.MaxLength);

        RuleForEach(x => x.FlowStates).ChildRules(state =>
        {
            state.RuleFor(s => s.Name)
                .NotEmpty()
                .MaximumLength(FlowState.MaxNameLength);

            state.RuleFor(s => s.Category)
                .IsInEnum();

            state.RuleFor(s => s.Color)
                .NotEmpty()
                .MaximumLength(Color.MaxLength);

            state.RuleFor(s => s.AllowedRoles)
                .NotEmpty();

            state.RuleForEach(s => s.AllowedRoles)
                .IsInEnum();
        });
    }
}
