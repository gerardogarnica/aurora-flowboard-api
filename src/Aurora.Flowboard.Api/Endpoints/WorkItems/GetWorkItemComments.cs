using Aurora.Flowboard.Application.Abstractions.Pagination;
using Aurora.Flowboard.Application.WorkItems.GetComments;

namespace Aurora.Flowboard.Api.Endpoints.WorkItems;

public sealed class GetWorkItemComments : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "work-items/{id:guid}/comments",
            async (
                Guid id,
                IQueryHandler<GetWorkItemCommentsQuery, PagedResponse<WorkItemCommentResponse>> handler,
                CancellationToken cancellationToken,
                int page = PaginationDefaults.DefaultPage,
                int pageSize = PaginationDefaults.DefaultPageSize) =>
            {
                var query = new GetWorkItemCommentsQuery(id, page, pageSize);

                Result<PagedResponse<WorkItemCommentResponse>> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("GetWorkItemComments")
            .WithTags(EndpointTags.WorkItems)
            .Produces<PagedResponse<WorkItemCommentResponse>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
