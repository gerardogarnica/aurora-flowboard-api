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

                return result.Match(ApiResponses.Accepted, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("AddProjectMember")
            .WithTags(EndpointTags.Projects)
            .Produces(StatusCodes.Status202Accepted)
            .ProducesStandardProblems(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status403Forbidden,
                StatusCodes.Status404NotFound,
                StatusCodes.Status409Conflict);
    }

    internal sealed record AddProjectMemberRequest(
        Guid UserId,
        ProjectRole Role);
}
