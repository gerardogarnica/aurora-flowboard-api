namespace Aurora.Flowboard.Domain.UnitTests.Users;

internal static class UserData
{
    public const string FirstName = "John";
    public const string LastName = "Doe";
    public const string EmailAddress = "john.doe@example.com";
    public const string AccessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.access";
    public const string RefreshToken = "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4";
    public static readonly Password Password = Password.Create("hashed_password_123").Value;
    public static readonly Password NewPassword = Password.Create("new_hashed_password_456").Value;
    public static readonly DateTime CreatedOnUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime UpdatedOnUtc = new(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime AccessTokenExpiresOnUtc = new(2026, 1, 1, 1, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime RefreshTokenExpiresOnUtc = new(2026, 1, 8, 0, 0, 0, DateTimeKind.Utc);

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

    public static User GetUserWithToken(out Guid tokenId)
    {
        User user = GetActiveUser();
        Result<UserToken> result = user.IssueToken(
            AccessToken,
            RefreshToken,
            AccessTokenExpiresOnUtc,
            RefreshTokenExpiresOnUtc,
            CreatedOnUtc);
        tokenId = result.Value.UserTokenId;
        return user;
    }

    public static User GetUserWithRole(Role role)
    {
        User user = GetActiveUser();
        user.AssignRole(role);
        return user;
    }

    public static User GetUserWithTwoTokens(out Guid firstTokenId, out Guid secondTokenId)
    {
        User user = GetActiveUser();

        firstTokenId = user.IssueToken(
            AccessToken,
            RefreshToken,
            AccessTokenExpiresOnUtc,
            RefreshTokenExpiresOnUtc,
            CreatedOnUtc).Value.UserTokenId;

        secondTokenId = user.IssueToken(
            $"{AccessToken}-2",
            $"{RefreshToken}-2",
            AccessTokenExpiresOnUtc,
            RefreshTokenExpiresOnUtc,
            CreatedOnUtc).Value.UserTokenId;

        return user;
    }
}
