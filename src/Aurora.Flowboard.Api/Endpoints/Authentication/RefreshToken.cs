using Aurora.Flowboard.Api.Extensions;
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
            .RequireRateLimiting(RateLimitingExtensions.AuthPolicy)
            .WithName("RefreshToken")
            .WithTags(EndpointTags.Authentication)
            .Produces<IdentityToken>(StatusCodes.Status200OK)
            .ProducesStandardProblems(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status401Unauthorized,
                StatusCodes.Status429TooManyRequests);
    }

    internal sealed record RefreshTokenRequest(string RefreshToken);
}
