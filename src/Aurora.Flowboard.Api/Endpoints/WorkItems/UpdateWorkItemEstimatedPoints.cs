using Aurora.Flowboard.Application.WorkItems.UpdateEstimatedPoints;

namespace Aurora.Flowboard.Api.Endpoints.WorkItems;

public sealed class UpdateWorkItemEstimatedPoints : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch(
            "work-items/{id:guid}/estimated-points",
            async (
                Guid id,
                [FromBody] UpdateWorkItemEstimatedPointsRequest request,
                ICommandHandler<UpdateWorkItemEstimatedPointsCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateWorkItemEstimatedPointsCommand(id, request.EstimatedPoints);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    () => Results.Accepted(string.Empty),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("UpdateWorkItemEstimatedPoints")
            .WithTags(EndpointTags.WorkItems)
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record UpdateWorkItemEstimatedPointsRequest(int? EstimatedPoints);
}
