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

                return result.Match(ApiResponses.Accepted, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("UpdateWorkItemEstimatedPoints")
            .WithTags(EndpointTags.WorkItems)
            .Produces(StatusCodes.Status202Accepted)
            .ProducesStandardProblems(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status404NotFound,
                StatusCodes.Status409Conflict);
    }

    internal sealed record UpdateWorkItemEstimatedPointsRequest(int? EstimatedPoints);
}
