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
        string refreshTokenHash = tokenProvider.HashRefreshToken(command.RefreshToken);
        DateTime utcNow = dateTimeProvider.UtcNow;

        // Loads only the presented token, not the user's whole token history.
        User? user = await dbContext
            .Users
            .Include(u => u.Roles)
            .Include(u => u.Tokens.Where(t => t.RefreshTokenHash == refreshTokenHash))
            .SingleOrDefaultAsync(
                u => u.Tokens.Any(t => t.RefreshTokenHash == refreshTokenHash),
                cancellationToken);

        UserToken? userToken = user?.Tokens.SingleOrDefault(t => t.RefreshTokenHash == refreshTokenHash);

        // A revoked token here is a replay of an already rotated token: rejected like any other.
        if (user is null || userToken is null || !user.IsActive || !userToken.IsRefreshTokenValid(utcNow))
        {
            return Result.Fail<IdentityToken>(AuthenticationErrors.InvalidRefreshToken);
        }

        Result revokeResult = user.RevokeToken(userToken.UserTokenId);

        if (!revokeResult.IsSuccessful)
        {
            return Result.Fail<IdentityToken>(revokeResult.Error);
        }

        List<string> roles = [.. user.Roles.Select(r => r.Name)];

        IssuedToken issuedToken = tokenProvider.CreateToken(new TokenRequest(
            user.Id,
            user.Email.Value,
            user.FirstName,
            user.LastName,
            roles));

        Result<UserToken> issueResult = user.IssueToken(
            issuedToken.AccessTokenId,
            issuedToken.RefreshTokenHash,
            issuedToken.Identity.AccessTokenExpiresOn.UtcDateTime,
            issuedToken.Identity.RefreshTokenExpiresOn.UtcDateTime,
            utcNow);

        if (!issueResult.IsSuccessful)
        {
            return Result.Fail<IdentityToken>(issueResult.Error);
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another request redeemed the same token between our read and our write (xmin changed).
            // SaveChanges' transaction also rolled back the insert of the new token.
            return Result.Fail<IdentityToken>(AuthenticationErrors.InvalidRefreshToken);
        }

        return issuedToken.Identity;
    }
}
