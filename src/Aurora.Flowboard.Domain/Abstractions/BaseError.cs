namespace Aurora.Flowboard.Domain.Abstractions;

public record BaseError(string Code, string Message, BaseErrorType ErrorType)
{
    public static readonly BaseError None = new(string.Empty, string.Empty, BaseErrorType.Failure);
    public static readonly BaseError NullValue = new("Error.NullValue", "The result value is null.", BaseErrorType.Failure);

    public static implicit operator Result(BaseError error) => Result.Fail(error);

    public static BaseError Failure(string code, string message) => new(code, message, BaseErrorType.Failure);

    public static BaseError Validation(string code, string message) => new(code, message, BaseErrorType.Validation);

    public static BaseError NotFound(string code, string message) => new(code, message, BaseErrorType.NotFound);

    public static BaseError Conflict(string code, string message) => new(code, message, BaseErrorType.Conflict);
}