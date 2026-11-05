using Aurora.Flowboard.Application.Projects.Update;

namespace Aurora.Flowboard.Api.Endpoints.Projects;

public sealed class UpdateProject : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(
            "projects/{id:guid}",
            async (
                Guid id,
                [FromBody] UpdateProjectRequest request,
                ICommandHandler<UpdateProjectCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateProjectCommand(
                    id,
                    request.Name,
                    request.Description,
                    request.Color);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(ApiResponses.Accepted, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("UpdateProject")
            .WithTags(EndpointTags.Projects)
            .Produces(StatusCodes.Status202Accepted)
            .ProducesStandardProblems(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status404NotFound,
                StatusCodes.Status409Conflict);
    }

    internal sealed record UpdateProjectRequest(
        string Name,
        string? Description,
        string Color);
}
