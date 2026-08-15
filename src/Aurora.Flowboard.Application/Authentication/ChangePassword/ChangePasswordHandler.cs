namespace Aurora.Flowboard.Application.Authentication.ChangePassword;

internal sealed class ChangePasswordHandler(
    IApplicationDbContext dbContext,
    IPasswordHasher passwordHasher,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ICommandHandler<ChangePasswordCommand>
{
    public async Task<Result> Handle(
        ChangePasswordCommand command,
        CancellationToken cancellationToken)
    {
        User? user = await dbContext
            .Users
            .Include(u => u.Tokens)
            .SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Fail(UserErrors.NotFound);
        }

        if (!user.IsActive)
        {
            return Result.Fail(UserErrors.Inactive);
        }

        if (!user.VerifyPassword(passwordHasher, command.CurrentPassword))
        {
            return Result.Fail(AuthenticationErrors.InvalidCredentials);
        }

        string passwordHash = passwordHasher.HashPassword(command.NewPassword);

        Result<Password> passwordResult = Password.Create(passwordHash);
        if (!passwordResult.IsSuccessful)
        {
            return Result.Fail(passwordResult.Error);
        }

        Result changePasswordResult = user.ChangePassword(passwordResult.Value, dateTimeProvider.UtcNow);
        if (!changePasswordResult.IsSuccessful)
        {
            return Result.Fail(changePasswordResult.Error);
        }

        Result revokeResult = user.RevokeAllActiveTokens();
        if (!revokeResult.IsSuccessful)
        {
            return Result.Fail(revokeResult.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
