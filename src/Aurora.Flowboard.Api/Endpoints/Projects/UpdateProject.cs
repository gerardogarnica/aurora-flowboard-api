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
                    request.EstimatedCompletionDate);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    () => Results.Accepted(string.Empty),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("UpdateProject")
            .WithTags(EndpointTags.Projects)
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record UpdateProjectRequest(
        string Name,
        string? Description,
        DateOnly? EstimatedCompletionDate);
}
