namespace Aurora.Flowboard.Domain.Shared;

public static class EmailErrors
{
    public static readonly BaseError Empty = BaseError.Validation(
        "Email.Empty",
        "Email cannot be empty");

    public static readonly BaseError TooLong = BaseError.Validation(
        "Email.TooLong",
        "Email cannot exceed 255 characters");

    public static readonly BaseError InvalidFormat = BaseError.Validation(
        "Email.InvalidFormat",
        "Email format is invalid");
}