using Aurora.Flowboard.Application.Projects.AddMember;
using Aurora.Flowboard.Domain.Projects;

namespace Aurora.Flowboard.Api.Endpoints.Projects;

public sealed class AddProjectMember : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "projects/{id:guid}/members",
            async (
                Guid id,
                [FromBody] AddProjectMemberRequest request,
                ICommandHandler<AddProjectMemberCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new AddProjectMemberCommand(id, request.UserId, request.Role);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    () => Results.Accepted(string.Empty),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("AddProjectMember")
            .WithTags(EndpointTags.Projects)
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record AddProjectMemberRequest(
        Guid UserId,
        ProjectRole Role);
}
