using Aurora.Flowboard.Application.Flows.GetById;

namespace Aurora.Flowboard.Api.Endpoints.Flows;

public sealed class GetFlowById : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "flows/{id:guid}",
            async (
                Guid id,
                IQueryHandler<GetFlowByIdQuery, FlowResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetFlowByIdQuery(id);

                Result<FlowResponse> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("GetFlowById")
            .WithTags(EndpointTags.Flows)
            .Produces<FlowResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
