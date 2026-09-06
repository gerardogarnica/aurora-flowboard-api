using Aurora.Flowboard.Application.Abstractions.Pagination;
using Aurora.Flowboard.Application.WorkItems.GetStateHistory;

namespace Aurora.Flowboard.Api.Endpoints.WorkItems;

public sealed class GetWorkItemStateHistory : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "work-items/{id:guid}/state-history",
            async (
                Guid id,
                IQueryHandler<GetWorkItemStateHistoryQuery, PagedResponse<WorkItemStateTransitionResponse>> handler,
                CancellationToken cancellationToken,
                int page = PaginationDefaults.DefaultPage,
                int pageSize = PaginationDefaults.DefaultPageSize) =>
            {
                var query = new GetWorkItemStateHistoryQuery(id, page, pageSize);

                Result<PagedResponse<WorkItemStateTransitionResponse>> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("GetWorkItemStateHistory")
            .WithTags(EndpointTags.WorkItems)
            .Produces<PagedResponse<WorkItemStateTransitionResponse>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
