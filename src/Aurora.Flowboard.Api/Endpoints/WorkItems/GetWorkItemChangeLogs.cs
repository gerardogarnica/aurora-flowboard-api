using Aurora.Flowboard.Application.Abstractions.Pagination;
using Aurora.Flowboard.Application.WorkItems.GetChangeLogs;

namespace Aurora.Flowboard.Api.Endpoints.WorkItems;

public sealed class GetWorkItemChangeLogs : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "work-items/{id:guid}/change-logs",
            async (
                Guid id,
                IQueryHandler<GetWorkItemChangeLogsQuery, PagedResponse<WorkItemChangeLogResponse>> handler,
                CancellationToken cancellationToken,
                int page = PaginationDefaults.DefaultPage,
                int pageSize = PaginationDefaults.DefaultPageSize) =>
            {
                var query = new GetWorkItemChangeLogsQuery(id, page, pageSize);

                Result<PagedResponse<WorkItemChangeLogResponse>> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("GetWorkItemChangeLogs")
            .WithTags(EndpointTags.WorkItems)
            .Produces<PagedResponse<WorkItemChangeLogResponse>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
