using Aurora.Flowboard.Application.WorkItems.GetByCode;

namespace Aurora.Flowboard.Api.Endpoints.WorkItems;

public sealed class GetWorkItemByCode : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "work-items/{code}",
            async (
                string code,
                IQueryHandler<GetWorkItemByCodeQuery, WorkItemResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetWorkItemByCodeQuery(code);

                Result<WorkItemResponse> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("GetWorkItemByCode")
            .WithTags(EndpointTags.WorkItems)
            .Produces<WorkItemResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
