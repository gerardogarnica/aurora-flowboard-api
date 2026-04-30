using Aurora.Flowboard.Application.Flows.Update;

namespace Aurora.Flowboard.Api.Endpoints.Flows;

public sealed class UpdateFlow : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(
            "flows/{id:guid}",
            async (
                Guid id,
                [FromBody] UpdateFlowRequest request,
                ICommandHandler<UpdateFlowCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateFlowCommand(
                    id,
                    request.Name,
                    request.Description);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    () => Results.Accepted(string.Empty),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("UpdateFlow")
            .WithTags(EndpointTags.Flows)
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record UpdateFlowRequest(
        string Name,
        string? Description);
}
