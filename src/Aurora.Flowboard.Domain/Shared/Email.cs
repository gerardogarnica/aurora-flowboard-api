namespace Aurora.Flowboard.Domain.Shared;

public sealed record Email
{
    private const int MaxLength = 255;

    public string Value { get; init; }

    private Email(string value)
    {
        Value = value;
    }

    public static Result<Email> Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Result.Fail<Email>(EmailErrors.Empty);
        }

        email = email.Trim().ToLowerInvariant();

        if (email.Length > MaxLength)
        {
            return Result.Fail<Email>(EmailErrors.TooLong);
        }

        if (!IsValidFormat(email))
        {
            return Result.Fail<Email>(EmailErrors.InvalidFormat);
        }

        return new Email(email);
    }

    private static bool IsValidFormat(string email)
    {
        int atIndex = email.IndexOf('@');
        int dotIndex = email.LastIndexOf('.');

        return atIndex > 0
            && dotIndex > atIndex + 1
            && dotIndex < email.Length - 1;
    }

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;
}
