namespace Aurora.Flowboard.Application.Projects.GetBoard;

internal sealed class GetProjectBoardValidator : AbstractValidator<GetProjectBoardQuery>
{
    public GetProjectBoardValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty();
    }
}
