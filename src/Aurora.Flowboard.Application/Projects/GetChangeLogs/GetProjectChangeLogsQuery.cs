namespace Aurora.Flowboard.Application.Projects.GetChangeLogs;

public sealed record GetProjectChangeLogsQuery(
    Guid ProjectId,
    int Page,
    int PageSize) : IQuery<PagedResponse<ProjectChangeLogResponse>>, IPagedQuery;
