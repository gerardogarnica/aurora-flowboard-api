using Aurora.Flowboard.Application.Authentication.ChangePassword;

namespace Aurora.Flowboard.Api.Endpoints.Authentication;

public sealed class ChangePassword : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(
            "auth/change-password",
            async (
                [FromBody] ChangePasswordRequest request,
                ICommandHandler<ChangePasswordCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new ChangePasswordCommand(request.CurrentPassword, request.NewPassword);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.NoContent, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("ChangePassword")
            .WithTags(EndpointTags.Authentication)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
}
