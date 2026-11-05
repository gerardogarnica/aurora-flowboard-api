namespace Aurora.Flowboard.Api.Endpoints;

internal static class RouteHandlerBuilderExtensions
{
    // Every route can fail with a 500 through GlobalExceptionHandler, so it is always
    // declared; callers list only the codes their handler can return as a Result.
    internal static RouteHandlerBuilder ProducesStandardProblems(
        this RouteHandlerBuilder builder,
        params int[] statusCodes)
    {
        foreach (int statusCode in statusCodes)
        {
            builder.Produces<ProblemDetails>(statusCode);
        }

        return builder.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
