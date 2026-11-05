using Aurora.Flowboard.Application.Projects.RemoveMember;

namespace Aurora.Flowboard.Api.Endpoints.Projects;

public sealed class RemoveProjectMember : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete(
            "projects/{id:guid}/members/{userId:guid}",
            async (
                Guid id,
                Guid userId,
                ICommandHandler<RemoveProjectMemberCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new RemoveProjectMemberCommand(id, userId);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(ApiResponses.Accepted, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("RemoveProjectMember")
            .WithTags(EndpointTags.Projects)
            .Produces(StatusCodes.Status202Accepted)
            .ProducesStandardProblems(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status403Forbidden,
                StatusCodes.Status404NotFound,
                StatusCodes.Status409Conflict);
    }
}
