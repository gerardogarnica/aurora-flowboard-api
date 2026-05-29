namespace Aurora.Flowboard.Application.Users.CreateUser;

internal sealed class CreateUserHandler(
    IApplicationDbContext dbContext,
    IPasswordHasher passwordHasher,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CreateUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        Result<Email> emailResult = Email.Create(command.Email);
        if (!emailResult.IsSuccessful)
        {
            return Result.Fail<Guid>(emailResult.Error);
        }

        Email email = emailResult.Value;

        Result<Role> roleResult = ResolveRole(command.Role);
        if (!roleResult.IsSuccessful)
        {
            return Result.Fail<Guid>(roleResult.Error);
        }

        bool emailInUse = await dbContext
            .Users
            .AnyAsync(u => u.Email.Value == email.Value, cancellationToken);

        if (emailInUse)
        {
            return Result.Fail<Guid>(UserErrors.EmailAlreadyInUse);
        }

        string passwordHash = passwordHasher.HashPassword(command.Password);

        Result<Password> passwordResult = Password.Create(passwordHash);
        if (!passwordResult.IsSuccessful)
        {
            return Result.Fail<Guid>(passwordResult.Error);
        }

        Result<User> userResult = User.Create(
            command.FirstName,
            command.LastName,
            email,
            passwordResult.Value,
            dateTimeProvider.UtcNow);

        if (!userResult.IsSuccessful)
        {
            return Result.Fail<Guid>(userResult.Error);
        }

        User user = userResult.Value;

        Result assignRoleResult = user.AssignRole(roleResult.Value);
        if (!assignRoleResult.IsSuccessful)
        {
            return Result.Fail<Guid>(assignRoleResult.Error);
        }

        dbContext.Users.Add(user);

        await dbContext.SaveChangesAsync(cancellationToken);

        return user.Id;
    }

    private static Result<Role> ResolveRole(string? role) =>
        string.IsNullOrWhiteSpace(role)
            ? Result.Ok(Role.Member)
            : Role.FromName(role.Trim());
}
