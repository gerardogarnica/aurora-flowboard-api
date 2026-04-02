namespace Aurora.Flowboard.Application.Abstractions.Authentication;

public sealed record IdentityToken(
    string AccessToken,
    DateTime AccessTokenExpiresOn,
    string RefreshToken,
    DateTime RefreshTokenExpiresOn);
