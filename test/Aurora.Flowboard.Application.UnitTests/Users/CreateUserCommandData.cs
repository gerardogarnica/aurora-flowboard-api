using Aurora.Flowboard.Application.Users.CreateUser;

namespace Aurora.Flowboard.Application.UnitTests.Users;

internal static class CreateUserCommandData
{
    internal const string FirstName = "Jane";
    internal const string LastName = "Doe";
    internal const string EmailAddress = "jane.doe@example.com";
    internal const string PlainPassword = "P@ssw0rd!";
    internal const string PasswordHash = "hashed_password_123";

    internal static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    internal static CreateUserCommand GetCreateCommand(string? role = null) =>
        new(FirstName, LastName, EmailAddress, PlainPassword, role);

    internal static User GetExistingUser(string email = EmailAddress)
    {
        Email emailValue = Email.Create(email).Value;
        Password password = Password.Create(PasswordHash).Value;

        return User.Create(FirstName, LastName, emailValue, password, UtcNow).Value;
    }
}
