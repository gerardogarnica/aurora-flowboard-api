namespace Aurora.Flowboard.Api.Responses;

internal static class ApiResponses
{
    internal static IResult Problem(Result result)
    {
        if (result.IsSuccessful)
        {
            throw new InvalidOperationException("The result is successful. Can't convert succesful result to a problem.");
        }

        return Results.Problem(
            type: GetTypeFromError(result.Error.ErrorType),
            title: $"The result of the request is a {result.Error.Code} failure",
            statusCode: GetStatusCodeFromError(result.Error.ErrorType),
            detail: result.Error.Message,
            extensions: GetErrors(result)
        );
    }

    private static string GetTypeFromError(BaseErrorType errorType) => errorType switch
    {
        BaseErrorType.Failure => "https://tools.ietf.org/html/rfc7231#section-6.6.1",
        BaseErrorType.Validation => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        BaseErrorType.NotFound => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
        BaseErrorType.Conflict => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
        BaseErrorType.Forbidden => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
        _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
    };

    private static int GetStatusCodeFromError(BaseErrorType errorType) => errorType switch
    {
        BaseErrorType.Failure => StatusCodes.Status500InternalServerError,
        BaseErrorType.Validation => StatusCodes.Status400BadRequest,
        BaseErrorType.NotFound => StatusCodes.Status404NotFound,
        BaseErrorType.Conflict => StatusCodes.Status409Conflict,
        BaseErrorType.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status500InternalServerError
    };

    private static Dictionary<string, object?> GetErrors(Result result)
    {
        if (result.Error is not Application.Abstractions.Validations.ValidationError validationError)
        {
            return [];
        }

        return new Dictionary<string, object?>
        {
            { "errors", validationError.Errors }
        };
    }
}
