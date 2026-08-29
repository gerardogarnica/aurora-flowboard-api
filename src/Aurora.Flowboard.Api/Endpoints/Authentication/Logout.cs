using Aurora.Flowboard.Application.Authentication.Logout;

namespace Aurora.Flowboard.Api.Endpoints.Authentication;

public sealed class Logout : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "auth/logout",
            async (
                [FromBody] LogoutRequest request,
                ICommandHandler<LogoutCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new LogoutCommand(request.RefreshToken);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.NoContent, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("Logout")
            .WithTags(EndpointTags.Authentication)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record LogoutRequest(string RefreshToken);
}
