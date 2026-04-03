namespace Aurora.Flowboard.Application.Abstractions.Authentication;

public sealed record IdentityToken(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresOn,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresOn);
