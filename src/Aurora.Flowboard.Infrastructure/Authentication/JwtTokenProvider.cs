using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Aurora.Flowboard.Infrastructure.Authentication;

internal sealed class JwtTokenProvider(
    IOptions<JwtAuthOptions> jwtAuthOptions,
    IDateTimeProvider dateTimeProvider) : ITokenProvider
{
    private const int RefreshTokenByteLength = 64;

    private readonly JwtAuthOptions _options = jwtAuthOptions.Value;
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;

    public IdentityToken CreateToken(TokenRequest tokenRequest)
    {
        DateTime utcNow = _dateTimeProvider.UtcNow;

        (string accessToken, DateTimeOffset accessTokenExpiresOn) = GenerateAccessToken(tokenRequest, utcNow);
        (string refreshToken, DateTimeOffset refreshTokenExpiresOn) = GenerateRefreshToken(utcNow);

        return new IdentityToken(
            accessToken,
            accessTokenExpiresOn,
            refreshToken,
            refreshTokenExpiresOn);
    }

    private (string Token, DateTimeOffset ExpiresOn) GenerateAccessToken(TokenRequest tokenRequest, DateTime utcNow)
    {
        DateTime expiresOn = utcNow.AddMinutes(_options.LifeTimeInMinutes);

        // Create security key from the JWT key
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));

        // Create signing credentials using the security key and HMAC SHA256 algorithm
        var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        string userId = tokenRequest.UserId.ToString();
        string fullName = $"{tokenRequest.FirstName} {tokenRequest.LastName}";

        // Set list of claims
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Sid, userId),
            new(ClaimTypes.NameIdentifier, userId),
            new(JwtRegisteredClaimNames.Email, tokenRequest.Email),
            new(JwtRegisteredClaimNames.GivenName, tokenRequest.FirstName),
            new(JwtRegisteredClaimNames.FamilyName, tokenRequest.LastName),
            new(JwtRegisteredClaimNames.Name, fullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(JwtRegisteredClaimNames.Typ, "Bearer"),
            new(
                JwtRegisteredClaimNames.Iat,
                EpochTime.GetIntDate(utcNow).ToString(CultureInfo.InvariantCulture),
                ClaimValueTypes.Integer64),
        };

        claims.AddRange(tokenRequest.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: utcNow,
            expires: expiresOn,
            signingCredentials: signingCredentials);

        string serializedToken = new JwtSecurityTokenHandler().WriteToken(token);

        return (serializedToken, new DateTimeOffset(expiresOn, TimeSpan.Zero));
    }

    private (string Token, DateTimeOffset ExpiresOn) GenerateRefreshToken(DateTime utcNow)
    {
        Span<byte> bytes = stackalloc byte[RefreshTokenByteLength];
        RandomNumberGenerator.Fill(bytes);

        string token = Convert.ToBase64String(bytes);
        DateTime expiresOn = utcNow.AddDays(_options.RefreshTokenExpirationDays);

        return (token, new DateTimeOffset(expiresOn, TimeSpan.Zero));
    }
}
