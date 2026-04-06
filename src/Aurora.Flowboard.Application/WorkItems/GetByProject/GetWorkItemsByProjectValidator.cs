namespace Aurora.Flowboard.Application.WorkItems.GetByProject;

internal sealed class GetWorkItemsByProjectValidator : AbstractValidator<GetWorkItemsByProjectQuery>
{
    public GetWorkItemsByProjectValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty();
    }
}
