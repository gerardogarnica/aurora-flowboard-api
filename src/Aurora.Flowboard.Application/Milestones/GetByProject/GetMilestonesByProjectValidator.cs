namespace Aurora.Flowboard.Application.Milestones.GetByProject;

internal sealed class GetMilestonesByProjectValidator : AbstractValidator<GetMilestonesByProjectQuery>
{
    public GetMilestonesByProjectValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty();
    }
}
