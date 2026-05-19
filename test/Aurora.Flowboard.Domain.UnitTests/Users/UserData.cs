namespace Aurora.Flowboard.Domain.UnitTests.Users;

internal static class UserData
{
    public const string FirstName = "John";
    public const string LastName = "Doe";
    public const string EmailAddress = "john.doe@example.com";
    public static readonly Password Password = Password.Create("hashed_password_123").Value;
    public static readonly DateTime CreatedOnUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static User GetActiveUser()
    {
        Email email = Email.Create(EmailAddress).Value;
        return User.Create(FirstName, LastName, email, Password, CreatedOnUtc).Value;
    }

    public static User GetInactiveUser()
    {
        User user = GetActiveUser();
        user.Deactivate(CreatedOnUtc);
        return user;
    }
}
