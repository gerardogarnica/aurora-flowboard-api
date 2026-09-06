namespace Aurora.Flowboard.Application.WorkItems.GetComments;

public sealed record GetWorkItemCommentsQuery(
    Guid WorkItemId,
    int Page,
    int PageSize) : IQuery<PagedResponse<WorkItemCommentResponse>>;
