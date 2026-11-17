namespace Aurora.Flowboard.Infrastructure.Authentication;

public sealed class JwtAuthOptions
{
    public const string SectionName = "Jwt";

    // HMAC-SHA256 needs a key of at least 256 bits; a shorter one fails only when a token is signed.
    public const int MinimumKeyLengthInBytes = 32;

    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public required string Key { get; init; }
    public int LifeTimeInMinutes { get; init; }
    public int RefreshTokenExpirationDays { get; init; }
}
