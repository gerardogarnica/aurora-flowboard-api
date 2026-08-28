namespace Aurora.Flowboard.Domain.Users;

public sealed class UserToken
{
    public const int MaxAccessTokenLength = 2000;
    public const int MaxRefreshTokenLength = 500;

    public Guid UserTokenId { get; private set; }
    public Guid UserId { get; private set; }
    public string AccessToken { get; private set; }
    public string RefreshToken { get; private set; }
    public DateTime AccessTokenExpiresOnUtc { get; private set; }
    public DateTime RefreshTokenExpiresOnUtc { get; private set; }
    public DateTime IssuedOnUtc { get; private set; }
    public bool IsRevoked { get; private set; }

    public User User { get; init; } = null!;

    private UserToken() { } // EF Core

    private UserToken(
        Guid userTokenId,
        Guid userId,
        string accessToken,
        string refreshToken,
        DateTime accessTokenExpiresOnUtc,
        DateTime refreshTokenExpiresOnUtc,
        DateTime issuedOnUtc)
    {
        UserTokenId = userTokenId;
        UserId = userId;
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        AccessTokenExpiresOnUtc = accessTokenExpiresOnUtc;
        RefreshTokenExpiresOnUtc = refreshTokenExpiresOnUtc;
        IssuedOnUtc = issuedOnUtc;
        IsRevoked = false;
    }

    internal static UserToken Create(
        Guid userId,
        string accessToken,
        string refreshToken,
        DateTime accessTokenExpiresOnUtc,
        DateTime refreshTokenExpiresOnUtc,
        DateTime issuedOnUtc)
    {
        return new UserToken(
            Guid.NewGuid(),
            userId,
            accessToken,
            refreshToken,
            accessTokenExpiresOnUtc,
            refreshTokenExpiresOnUtc,
            issuedOnUtc);
    }

    internal Result Revoke()
    {
        if (IsRevoked)
        {
            return Result.Fail(UserTokenErrors.AlreadyRevoked);
        }

        IsRevoked = true;

        return Result.Ok();
    }

    public bool IsRefreshTokenValid(DateTime utcNow) =>
        !IsRevoked && RefreshTokenExpiresOnUtc > utcNow;
}
