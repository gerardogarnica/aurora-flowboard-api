namespace Aurora.Flowboard.Application.Authentication.Logout;

internal sealed class LogoutHandler(
    IApplicationDbContext dbContext,
    ITokenProvider tokenProvider,
    IUserContext userContext) : ICommandHandler<LogoutCommand>
{
    public async Task<Result> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        string refreshTokenHash = tokenProvider.HashRefreshToken(command.RefreshToken);
        Guid currentUserId = userContext.UserId;

        // Loads only the presented token, and only if it belongs to the caller.
        User? user = await dbContext
            .Users
            .Include(u => u.Tokens.Where(t => t.RefreshTokenHash == refreshTokenHash))
            .SingleOrDefaultAsync(
                u => u.Id == currentUserId && u.Tokens.Any(t => t.RefreshTokenHash == refreshTokenHash),
                cancellationToken);

        UserToken? userToken = user?.Tokens.SingleOrDefault(t => t.RefreshTokenHash == refreshTokenHash);

        if (user is null || userToken is null)
        {
            return Result.Ok();
        }

        // Logout is idempotent: an already revoked token is not an error and needs no write.
        Result revokeResult = user.RevokeToken(userToken.UserTokenId);

        if (!revokeResult.IsSuccessful)
        {
            return Result.Ok();
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // A concurrent refresh or logout changed the row first: the session is closed either way.
            return Result.Ok();
        }

        return Result.Ok();
    }
}
