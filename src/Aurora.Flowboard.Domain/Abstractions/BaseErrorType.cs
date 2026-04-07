namespace Aurora.Flowboard.Domain.Abstractions;

public enum BaseErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Forbidden = 4
}