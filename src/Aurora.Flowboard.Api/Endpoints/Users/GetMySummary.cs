using Aurora.Flowboard.Application.Abstractions.Authentication;
using Aurora.Flowboard.Application.Users.GetMySummary;

namespace Aurora.Flowboard.Api.Endpoints.Users;

public sealed class GetMySummary : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "users/my-summary",
            async (
                IUserContext userContext,
                IQueryHandler<GetMySummaryQuery, MySummaryResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetMySummaryQuery(userContext.UserId);

                Result<MySummaryResponse> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("GetMySummary")
            .WithTags(EndpointTags.Users)
            .Produces<MySummaryResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
