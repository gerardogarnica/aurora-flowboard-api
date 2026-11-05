using Aurora.Flowboard.Application.Abstractions.Pagination;
using Aurora.Flowboard.Application.WorkItems.GetTimeEntries;

namespace Aurora.Flowboard.Api.Endpoints.WorkItems;

public sealed class GetWorkItemTimeEntries : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "work-items/{id:guid}/time-entries",
            async (
                Guid id,
                IQueryHandler<GetWorkItemTimeEntriesQuery, PagedResponse<WorkItemTimeEntryResponse>> handler,
                CancellationToken cancellationToken,
                int page = PaginationDefaults.DefaultPage,
                int pageSize = PaginationDefaults.DefaultPageSize) =>
            {
                var query = new GetWorkItemTimeEntriesQuery(id, page, pageSize);

                Result<PagedResponse<WorkItemTimeEntryResponse>> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("GetWorkItemTimeEntries")
            .WithTags(EndpointTags.WorkItems)
            .Produces<PagedResponse<WorkItemTimeEntryResponse>>(StatusCodes.Status200OK)
            .ProducesStandardProblems(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status404NotFound);
    }
}
