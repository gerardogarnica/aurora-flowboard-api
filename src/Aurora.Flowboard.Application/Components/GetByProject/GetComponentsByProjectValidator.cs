namespace Aurora.Flowboard.Application.Components.GetByProject;

internal sealed class GetComponentsByProjectValidator : AbstractValidator<GetComponentsByProjectQuery>
{
    public GetComponentsByProjectValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty();
    }
}
