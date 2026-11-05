using Aurora.Flowboard.Application.Components.Create;

namespace Aurora.Flowboard.Api.Endpoints.Components;

public sealed class CreateComponent : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "projects/{id:guid}/components",
            async (
                Guid id,
                [FromBody] CreateComponentRequest request,
                ICommandHandler<CreateComponentCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateComponentCommand(id, request.Name);

                Result<Guid> result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    () => Results.Created(string.Empty, result.Value),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("CreateComponent")
            .WithTags(EndpointTags.Components)
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesStandardProblems(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status403Forbidden,
                StatusCodes.Status404NotFound,
                StatusCodes.Status409Conflict);
    }

    internal sealed record CreateComponentRequest(string Name);
}
