using Aurora.Flowboard.Application.Users.ChangeRole;
using Aurora.Flowboard.Domain.Users;

namespace Aurora.Flowboard.Api.Endpoints.Users;

public sealed class ChangeUserRole : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch(
            "users/{id:guid}/role",
            async (
                Guid id,
                [FromBody] ChangeUserRoleRequest request,
                ICommandHandler<ChangeUserRoleCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new ChangeUserRoleCommand(id, request.Role);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(ApiResponses.Accepted, ApiResponses.Problem);
            })
            .RequireAuthorization(policy => policy.RequireRole(Role.Administrator.Name))
            .WithName("ChangeUserRole")
            .WithTags(EndpointTags.Users)
            .Produces(StatusCodes.Status202Accepted)
            .ProducesStandardProblems(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status403Forbidden,
                StatusCodes.Status404NotFound,
                StatusCodes.Status409Conflict);
    }

    internal sealed record ChangeUserRoleRequest(string Role);
}
