using Aurora.Flowboard.Application.WorkItems.UpdateTitle;

namespace Aurora.Flowboard.Api.Endpoints.WorkItems;

public sealed class UpdateWorkItemTitle : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch(
            "work-items/{id:guid}/title",
            async (
                Guid id,
                [FromBody] UpdateWorkItemTitleRequest request,
                ICommandHandler<UpdateWorkItemTitleCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateWorkItemTitleCommand(id, request.Title);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    () => Results.Accepted(string.Empty),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("UpdateWorkItemTitle")
            .WithTags(EndpointTags.WorkItems)
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record UpdateWorkItemTitleRequest(string Title);
}
