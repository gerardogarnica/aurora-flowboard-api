namespace Aurora.Flowboard.Application.Users.ChangeRole;

internal sealed class ChangeUserRoleHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<ChangeUserRoleCommand>
{
    public async Task<Result> Handle(
        ChangeUserRoleCommand command,
        CancellationToken cancellationToken)
    {
        Result<Role> roleResult = Role.FromName(command.Role.Trim());
        if (!roleResult.IsSuccessful)
        {
            return Result.Fail(roleResult.Error);
        }

        User? user = await dbContext
            .Users
            .Include(u => u.Roles)
            .SingleOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Fail(UserErrors.NotFound);
        }

        Result changeRoleResult = user.ChangeRole(roleResult.Value, dateTimeProvider.UtcNow);

        if (!changeRoleResult.IsSuccessful)
        {
            return changeRoleResult;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
