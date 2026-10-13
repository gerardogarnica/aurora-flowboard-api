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

        Role newRole = roleResult.Value;

        bool isDemotingActiveAdministrator = user.IsActive
            && user.IsAdministrator
            && newRole.Name != Role.Administrator.Name;

        // Only demoting an active Administrator can leave the system without one, so only then is the
        // query needed. IsAdministrator is not translatable to SQL, hence the explicit Roles.Any below.
        bool hasOtherActiveAdministrator = !isDemotingActiveAdministrator || await dbContext
            .Users
            .AnyAsync(
                u => u.Id != user.Id && u.IsActive && u.Roles.Any(r => r.Name == Role.Administrator.Name),
                cancellationToken);

        Result changeRoleResult = user.ChangeRole(newRole, hasOtherActiveAdministrator, dateTimeProvider.UtcNow);

        if (!changeRoleResult.IsSuccessful)
        {
            return changeRoleResult;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
