using Aurora.Flowboard.Application.Projects.GetFlow;

namespace Aurora.Flowboard.Api.Endpoints.Flows;

public sealed class GetProjectFlow : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "projects/{id:guid}/flow",
            async (
                Guid id,
                IQueryHandler<GetProjectFlowQuery, ProjectFlowResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetProjectFlowQuery(id);

                Result<ProjectFlowResponse> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("GetProjectFlow")
            .WithTags(EndpointTags.Flows)
            .Produces<ProjectFlowResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
