using Aurora.Flowboard.Application.Flows.Create;

namespace Aurora.Flowboard.Api.Endpoints.Flows;

public sealed class CreateFlow : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "flows",
            async (
                [FromBody] CreateFlowRequest request,
                ICommandHandler<CreateFlowCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateFlowCommand(
                    request.Name,
                    request.Description,
                    request.ProjectId);

                Result<Guid> result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    () => Results.Created(string.Empty, result.Value),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("CreateFlow")
            .WithTags(EndpointTags.Flows)
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record CreateFlowRequest(
        string Name,
        string? Description,
        Guid ProjectId);
}
