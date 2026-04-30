using Aurora.Flowboard.Application.Flows.GetAll;

namespace Aurora.Flowboard.Api.Endpoints.Flows;

public sealed class GetFlows : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "flows",
            async (
                [FromQuery(Name = "projectId")] Guid? projectId,
                [FromQuery(Name = "includeDeactivated")] bool includeDeactivated,
                IQueryHandler<GetAllFlowsQuery, IReadOnlyCollection<FlowSummaryResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetAllFlowsQuery(includeDeactivated, projectId);

                Result<IReadOnlyCollection<FlowSummaryResponse>> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("GetFlows")
            .WithTags(EndpointTags.Flows)
            .Produces<IReadOnlyCollection<FlowSummaryResponse>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
