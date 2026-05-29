using Aurora.Flowboard.Application.Users.CreateUser;
using Aurora.Flowboard.Domain.Users;

namespace Aurora.Flowboard.Api.Endpoints.Users;

public sealed class CreateUser : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "users",
            async (
                [FromBody] CreateUserRequest request,
                ICommandHandler<CreateUserCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateUserCommand(
                    request.FirstName,
                    request.LastName,
                    request.Email,
                    request.Password,
                    request.Role);

                Result<Guid> result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    () => Results.Created(string.Empty, result.Value),
                    ApiResponses.Problem);
            })
            .RequireAuthorization(policy => policy.RequireRole(Role.Administrator.Name))
            .WithName("CreateUser")
            .WithTags(EndpointTags.Users)
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record CreateUserRequest(
        string FirstName,
        string LastName,
        string Email,
        string Password,
        string? Role);
}
