namespace Aurora.Flowboard.Application.WorkItems.GetWorkItems;

// Cross-project search for the Home and My Issues: the open work items of my active projects.
// The filters arrive as raw strings so that every invalid value gets the same 400 from GetWorkItemsValidator.
public sealed record GetWorkItemsQuery(
    string? Assignee,
    string? CreatedBy,
    string? Stage,
    string? Stalled,
    string? Priority,
    string? Sort,
    int Page,
    int PageSize) : IQuery<PagedResponse<WorkItemListItemResponse>>, IPagedQuery;
