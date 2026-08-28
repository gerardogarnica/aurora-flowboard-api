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

                return result.Match(
                    () => Results.Accepted(string.Empty),
                    ApiResponses.Problem);
            })
            .RequireAuthorization(policy => policy.RequireRole(Role.Administrator.Name))
            .WithName("ChangeUserRole")
            .WithTags(EndpointTags.Users)
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record ChangeUserRoleRequest(string Role);
}
