using Aurora.Flowboard.Domain.Shared;

namespace Aurora.Flowboard.Application.Authentication.RefreshToken;

internal sealed class RefreshTokenHandler(
    IApplicationDbContext dbContext,
    ITokenProvider tokenProvider,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<RefreshTokenCommand, IdentityToken>
{
    public async Task<Result<IdentityToken>> Handle(
        RefreshTokenCommand command,
        CancellationToken cancellationToken)
    {
        UserToken? userToken = await dbContext
            .UserTokens
            .SingleOrDefaultAsync(t => t.RefreshToken == command.RefreshToken, cancellationToken);

        if (userToken is null || !userToken.IsRefreshTokenValid(dateTimeProvider.UtcNow))
        {
            return Result.Fail<IdentityToken>(AuthenticationErrors.InvalidRefreshToken);
        }

        User? user = await dbContext
            .Users
            .Include(u => u.Roles)
            .Include(u => u.Tokens)
            .SingleOrDefaultAsync(u => u.Id == userToken.UserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return Result.Fail<IdentityToken>(AuthenticationErrors.InvalidRefreshToken);
        }

        Result revokeResult = user.RevokeToken(userToken.UserTokenId);

        if (!revokeResult.IsSuccessful)
        {
            return Result.Fail<IdentityToken>(revokeResult.Error);
        }

        List<string> roles = [.. user.Roles.Select(r => r.Name)];

        IdentityToken identityToken = tokenProvider.CreateToken(new TokenRequest(
            user.Id,
            user.Email.Value,
            user.FirstName,
            user.LastName,
            roles));

        Result<UserToken> issueResult = user.IssueToken(
            identityToken.AccessToken,
            identityToken.RefreshToken,
            identityToken.AccessTokenExpiresOn.UtcDateTime,
            identityToken.RefreshTokenExpiresOn.UtcDateTime,
            dateTimeProvider.UtcNow);

        if (!issueResult.IsSuccessful)
        {
            return Result.Fail<IdentityToken>(issueResult.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return identityToken;
    }
}
