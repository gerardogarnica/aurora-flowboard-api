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

                return result.Match(ApiResponses.Accepted, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("ChangeProjectKind")
            .WithTags(EndpointTags.Projects)
            .Produces(StatusCodes.Status202Accepted)
            .ProducesStandardProblems(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status403Forbidden,
                StatusCodes.Status404NotFound,
                StatusCodes.Status409Conflict);
    }

    internal sealed record ChangeProjectKindRequest(ProjectKind NewKind);
}
