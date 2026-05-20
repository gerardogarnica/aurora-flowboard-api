using Aurora.Flowboard.Application.Abstractions.Authentication;
using Aurora.Flowboard.Application.Users.GetUserById;

namespace Aurora.Flowboard.Api.Endpoints.Users;

public sealed class GetMyProfile : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "users/me",
            async (
                IUserContext userContext,
                IQueryHandler<GetUserByIdQuery, UserProfileResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetUserByIdQuery(userContext.UserId);

                Result<UserProfileResponse> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("GetMyProfile")
            .WithTags(EndpointTags.Users)
            .Produces<UserProfileResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
