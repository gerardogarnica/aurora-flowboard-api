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

                return result.Match(
                    () => Results.Accepted(string.Empty),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("UpdateWorkItemDescription")
            .WithTags(EndpointTags.WorkItems)
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record UpdateWorkItemDescriptionRequest(string? Description);
}
