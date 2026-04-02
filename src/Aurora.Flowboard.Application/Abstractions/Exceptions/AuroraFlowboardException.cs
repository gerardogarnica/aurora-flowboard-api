namespace Aurora.Flowboard.Application.Abstractions.Exceptions;

public sealed class AuroraFlowboardException : Exception
{
    public AuroraFlowboardException(
        string requestName,
        BaseError? error = default,
        Exception? innerException = default) : base($"Error processing request {requestName}.", innerException)
    {
        RequestName = requestName;
        Error = error;
    }

    public string RequestName { get; }

    public BaseError? Error { get; }
}
