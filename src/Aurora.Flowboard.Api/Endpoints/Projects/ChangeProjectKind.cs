using Aurora.Flowboard.Application.Projects.ChangeKind;
using Aurora.Flowboard.Domain.Projects;

namespace Aurora.Flowboard.Api.Endpoints.Projects;

public sealed class ChangeProjectKind : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch(
            "projects/{id:guid}/kind",
            async (
                Guid id,
                [FromBody] ChangeProjectKindRequest request,
                ICommandHandler<ChangeProjectKindCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new ChangeProjectKindCommand(id, request.NewKind);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    () => Results.Accepted(string.Empty),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("ChangeProjectKind")
            .WithTags(EndpointTags.Projects)
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record ChangeProjectKindRequest(ProjectKind NewKind);
}
