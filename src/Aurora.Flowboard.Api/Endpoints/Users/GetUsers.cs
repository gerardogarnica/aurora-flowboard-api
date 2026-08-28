using Aurora.Flowboard.Application.Users.GetAll;

namespace Aurora.Flowboard.Api.Endpoints.Users;

public sealed class GetUsers : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "users",
            async (
                IQueryHandler<GetAllUsersQuery, IReadOnlyCollection<UserSummaryResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetAllUsersQuery();

                Result<IReadOnlyCollection<UserSummaryResponse>> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("GetUsers")
            .WithTags(EndpointTags.Users)
            .Produces<IReadOnlyCollection<UserSummaryResponse>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
