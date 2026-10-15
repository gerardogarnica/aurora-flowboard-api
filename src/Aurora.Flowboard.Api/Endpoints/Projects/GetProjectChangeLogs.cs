using Aurora.Flowboard.Application.Abstractions.Pagination;
using Aurora.Flowboard.Application.Projects.GetChangeLogs;

namespace Aurora.Flowboard.Api.Endpoints.Projects;

public sealed class GetProjectChangeLogs : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "projects/{id:guid}/change-logs",
            async (
                Guid id,
                IQueryHandler<GetProjectChangeLogsQuery, PagedResponse<ProjectChangeLogResponse>> handler,
                CancellationToken cancellationToken,
                int page = PaginationDefaults.DefaultPage,
                int pageSize = PaginationDefaults.DefaultPageSize) =>
            {
                var query = new GetProjectChangeLogsQuery(id, page, pageSize);

                Result<PagedResponse<ProjectChangeLogResponse>> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("GetProjectChangeLogs")
            .WithTags(EndpointTags.Projects)
            .Produces<PagedResponse<ProjectChangeLogResponse>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
