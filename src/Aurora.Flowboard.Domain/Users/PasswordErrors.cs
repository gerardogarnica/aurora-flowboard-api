namespace Aurora.Flowboard.Domain.Users;

public static class PasswordErrors
{
    public static readonly BaseError HashRequired = BaseError.Validation(
        "Password.HashRequired",
        "Password hash is required");

    public static readonly BaseError HashTooShort = BaseError.Validation(
        "Password.HashTooShort",
        $"Password hash must be at least {Password.MinHashLength} characters");

    public static readonly BaseError HashTooLong = BaseError.Validation(
        "Password.HashTooLong",
        $"Password hash cannot exceed {Password.MaxHashLength} characters");
}
