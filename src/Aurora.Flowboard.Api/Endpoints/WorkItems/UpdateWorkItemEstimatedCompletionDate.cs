using Aurora.Flowboard.Application.WorkItems.UpdateEstimatedCompletionDate;

namespace Aurora.Flowboard.Api.Endpoints.WorkItems;

public sealed class UpdateWorkItemEstimatedCompletionDate : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch(
            "work-items/{id:guid}/estimated-completion-date",
            async (
                Guid id,
                [FromBody] UpdateWorkItemEstimatedCompletionDateRequest request,
                ICommandHandler<UpdateWorkItemEstimatedCompletionDateCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateWorkItemEstimatedCompletionDateCommand(
                    id,
                    request.EstimatedCompletionDate);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(ApiResponses.Accepted, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("UpdateWorkItemEstimatedCompletionDate")
            .WithTags(EndpointTags.WorkItems)
            .Produces(StatusCodes.Status202Accepted)
            .ProducesStandardProblems(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status403Forbidden,
                StatusCodes.Status404NotFound,
                StatusCodes.Status409Conflict);
    }

    internal sealed record UpdateWorkItemEstimatedCompletionDateRequest(DateOnly? EstimatedCompletionDate);
}
