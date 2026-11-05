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
            .ProducesStandardProblems(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status401Unauthorized,
                StatusCodes.Status404NotFound,
                StatusCodes.Status409Conflict);
    }

    internal sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
}
