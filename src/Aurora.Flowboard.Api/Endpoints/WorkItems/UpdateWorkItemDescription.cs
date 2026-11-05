using Aurora.Flowboard.Application.WorkItems.UpdateDescription;

namespace Aurora.Flowboard.Api.Endpoints.WorkItems;

public sealed class UpdateWorkItemDescription : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch(
            "work-items/{id:guid}/description",
            async (
                Guid id,
                [FromBody] UpdateWorkItemDescriptionRequest request,
                ICommandHandler<UpdateWorkItemDescriptionCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateWorkItemDescriptionCommand(id, request.Description);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(ApiResponses.Accepted, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("UpdateWorkItemDescription")
            .WithTags(EndpointTags.WorkItems)
            .Produces(StatusCodes.Status202Accepted)
            .ProducesStandardProblems(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status403Forbidden,
                StatusCodes.Status404NotFound,
                StatusCodes.Status409Conflict);
    }

    internal sealed record UpdateWorkItemDescriptionRequest(string? Description);
}
