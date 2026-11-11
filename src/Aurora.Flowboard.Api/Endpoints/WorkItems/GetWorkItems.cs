using Aurora.Flowboard.Application.Abstractions.Pagination;
using Aurora.Flowboard.Application.WorkItems.GetWorkItems;

namespace Aurora.Flowboard.Api.Endpoints.WorkItems;

public sealed class GetWorkItems : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "work-items",
            async (
                [AsParameters] GetWorkItemsRequest request,
                IQueryHandler<GetWorkItemsQuery, PagedResponse<WorkItemListItemResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetWorkItemsQuery(
                    request.Assignee,
                    request.CreatedBy,
                    request.Stage,
                    request.Stalled,
                    request.Priority,
                    request.Sort,
                    request.Page,
                    request.PageSize);

                Result<PagedResponse<WorkItemListItemResponse>> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("GetWorkItems")
            .WithTags(EndpointTags.WorkItems)
            .Produces<PagedResponse<WorkItemListItemResponse>>(StatusCodes.Status200OK)
            .ProducesStandardProblems(
                StatusCodes.Status400BadRequest);
    }

    // Every filter is optional; GetWorkItemsValidator answers 400 for any value it does not accept.
    internal sealed record GetWorkItemsRequest(
        string? Assignee = null,
        string? CreatedBy = null,
        string? Stage = null,
        string? Stalled = null,
        string? Priority = null,
        string? Sort = null,
        int Page = PaginationDefaults.DefaultPage,
        int PageSize = PaginationDefaults.DefaultPageSize);
}
