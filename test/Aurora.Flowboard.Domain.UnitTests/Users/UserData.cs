namespace Aurora.Flowboard.Domain.UnitTests.Users;

internal static class UserData
{
    public const string FirstName = "John";
    public const string LastName = "Doe";
    public const string EmailAddress = "john.doe@example.com";
    public const string PasswordHash = "hashed_password_123";
    public static readonly DateTime CreatedOnUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static User GetActiveUser()
    {
        Email email = Email.Create(EmailAddress).Value;
        return User.Create(FirstName, LastName, email, PasswordHash, CreatedOnUtc).Value;
    }

    public static User GetInactiveUser()
    {
        User user = GetActiveUser();
        user.Deactivate(CreatedOnUtc);
        return user;
    }
}
