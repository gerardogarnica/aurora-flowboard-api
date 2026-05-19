namespace Aurora.Flowboard.Domain.Users;

public sealed record Password
{
    public const int MinHashLength = 8;
    public const int MaxHashLength = 500;

    public string Hash { get; init; }

    private Password(string hash)
    {
        Hash = hash;
    }

    public static Result<Password> Create(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
        {
            return Result.Fail<Password>(PasswordErrors.HashRequired);
        }

        if (hash.Length < MinHashLength)
        {
            return Result.Fail<Password>(PasswordErrors.HashTooShort);
        }

        if (hash.Length > MaxHashLength)
        {
            return Result.Fail<Password>(PasswordErrors.HashTooLong);
        }

        return new Password(hash);
    }

    public override string ToString() => Hash;
}

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
