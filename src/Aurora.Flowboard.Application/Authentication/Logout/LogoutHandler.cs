namespace Aurora.Flowboard.Application.Authentication.Logout;

internal sealed class LogoutHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext) : ICommandHandler<LogoutCommand>
{
    public async Task<Result> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        UserToken? userToken = await dbContext
            .UserTokens
            .SingleOrDefaultAsync(t => t.RefreshToken == command.RefreshToken, cancellationToken);

        if (userToken is null || userToken.UserId != userContext.UserId)
        {
            return Result.Ok();
        }

        User? user = await dbContext
            .Users
            .Include(u => u.Tokens)
            .SingleOrDefaultAsync(u => u.Id == userToken.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Ok();
        }

        user.RevokeToken(userToken.UserTokenId);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
