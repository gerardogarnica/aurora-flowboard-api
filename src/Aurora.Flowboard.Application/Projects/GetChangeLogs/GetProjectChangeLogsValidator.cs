namespace Aurora.Flowboard.Application.Projects.GetChangeLogs;

internal sealed class GetProjectChangeLogsValidator : AbstractValidator<GetProjectChangeLogsQuery>
{
    public GetProjectChangeLogsValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty();

        RuleFor(x => x.Page)
            .MustBeValidPage();

        RuleFor(x => x.PageSize)
            .MustBeValidPageSize();
    }
}
