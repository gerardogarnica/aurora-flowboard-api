using Aurora.Flowboard.Application.WorkItems.GetById;

namespace Aurora.Flowboard.Api.Endpoints.WorkItems;

public sealed class GetWorkItemById : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "work-items/{id:guid}",
            async (
                Guid id,
                IQueryHandler<GetWorkItemByIdQuery, WorkItemResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetWorkItemByIdQuery(id);

                Result<WorkItemResponse> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("GetWorkItemById")
            .WithTags(EndpointTags.WorkItems)
            .Produces<WorkItemResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
