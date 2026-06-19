using Aurora.Flowboard.Application.WorkItems.GetByProject;

namespace Aurora.Flowboard.Api.Endpoints.WorkItems;

public sealed class GetWorkItemsByProject : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "projects/{projectId:guid}/work-items",
            async (
                Guid projectId,
                IQueryHandler<GetWorkItemsByProjectQuery, IReadOnlyCollection<FlowStateBoardResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetWorkItemsByProjectQuery(projectId);

                Result<IReadOnlyCollection<FlowStateBoardResponse>> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("GetWorkItemsByProject")
            .WithTags(EndpointTags.WorkItems)
            .Produces<IReadOnlyCollection<FlowStateBoardResponse>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
