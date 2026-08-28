using Aurora.Flowboard.Application.Abstractions.Authentication;
using Aurora.Flowboard.Application.Authentication.RefreshToken;

namespace Aurora.Flowboard.Api.Endpoints.Authentication;

public sealed class RefreshToken : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "auth/refresh-token",
            async (
                [FromBody] RefreshTokenRequest request,
                ICommandHandler<RefreshTokenCommand, IdentityToken> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new RefreshTokenCommand(request.RefreshToken);

                Result<IdentityToken> result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.Ok, ApiResponses.Problem);
            })
            .AllowAnonymous()
            .WithName("RefreshToken")
            .WithTags(EndpointTags.Authentication)
            .Produces<IdentityToken>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record RefreshTokenRequest(string RefreshToken);
}
