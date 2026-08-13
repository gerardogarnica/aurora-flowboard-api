namespace Aurora.Flowboard.Application.Projects.GetFlow;

internal sealed class GetProjectFlowValidator : AbstractValidator<GetProjectFlowQuery>
{
    public GetProjectFlowValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty();
    }
}
