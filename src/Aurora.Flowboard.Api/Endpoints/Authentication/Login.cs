using Aurora.Flowboard.Application.Abstractions.Authentication;
using Aurora.Flowboard.Application.Authentication.Login;

namespace Aurora.Flowboard.Api.Endpoints.Authentication;

public sealed class Login : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "auth/login",
            async (
                [FromBody] LoginRequest request,
                ICommandHandler<LoginCommand, IdentityToken> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new LoginCommand(request.Email, request.Password);

                Result<IdentityToken> result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.Ok, ApiResponses.Problem);
            })
            .AllowAnonymous()
            .WithName("Login")
            .WithTags(EndpointTags.Authentication)
            .Produces<IdentityToken>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record LoginRequest(string Email, string Password);
}
