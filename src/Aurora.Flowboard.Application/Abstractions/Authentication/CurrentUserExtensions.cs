namespace Aurora.Flowboard.Application.Abstractions.Authentication;

internal static class CurrentUserExtensions
{
    public static async Task<Result<User>> GetCurrentUserAsync(
        this IApplicationDbContext dbContext,
        IUserContext userContext,
        CancellationToken cancellationToken)
    {
        User? user = await dbContext
            .Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);

        return user is null ? Result.Fail<User>(UserErrors.NotFound) : user;
    }
}
