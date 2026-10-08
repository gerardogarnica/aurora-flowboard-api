namespace Aurora.Flowboard.Application.Abstractions.Authentication;

/// <summary>
/// What <see cref="ITokenProvider"/> issues: the pair returned to the client, plus the values that
/// are persisted instead of the secrets (the JWT id and the refresh token hash).
/// </summary>
public sealed record IssuedToken(
    IdentityToken Identity,
    string AccessTokenId,
    string RefreshTokenHash);
