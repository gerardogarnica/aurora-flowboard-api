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

                return result.Match(
                    () => Results.Accepted(string.Empty),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("UpdateWorkItemEstimatedCompletionDate")
            .WithTags(EndpointTags.WorkItems)
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record UpdateWorkItemEstimatedCompletionDateRequest(DateOnly? EstimatedCompletionDate);
}
