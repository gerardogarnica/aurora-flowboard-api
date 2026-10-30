namespace Aurora.Flowboard.Application.Projects.GetChangeLogs;

internal sealed class GetProjectChangeLogsValidator : AbstractValidator<GetProjectChangeLogsQuery>
{
    public GetProjectChangeLogsValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty();

        Include(new PagedQueryValidator());
    }
}
