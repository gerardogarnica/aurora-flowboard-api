namespace Aurora.Flowboard.Domain.UnitTests.Users;

internal static class UserData
{
    public const string FirstName = "John";
    public const string LastName = "Doe";
    public const string EmailAddress = "john.doe@example.com";
    public const string AccessTokenId = "0f8fad5bd9cb469fa16570867728950e";
    public const string RefreshTokenHash = "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08";
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
            AccessTokenId,
            RefreshTokenHash,
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
            AccessTokenId,
            RefreshTokenHash,
            AccessTokenExpiresOnUtc,
            RefreshTokenExpiresOnUtc,
            CreatedOnUtc).Value.UserTokenId;

        secondTokenId = user.IssueToken(
            $"{AccessTokenId}-2",
            $"{RefreshTokenHash}-2",
            AccessTokenExpiresOnUtc,
            RefreshTokenExpiresOnUtc,
            CreatedOnUtc).Value.UserTokenId;

        return user;
    }

    // The second token's refresh expiry (CreatedOnUtc - 3 days) is already past at CreatedOnUtc.
    public static User GetUserWithActiveAndExpiredToken(out Guid activeTokenId, out Guid expiredTokenId)
    {
        User user = GetActiveUser();

        activeTokenId = user.IssueToken(
            AccessTokenId,
            RefreshTokenHash,
            AccessTokenExpiresOnUtc,
            RefreshTokenExpiresOnUtc,
            CreatedOnUtc).Value.UserTokenId;

        expiredTokenId = user.IssueToken(
            $"{AccessTokenId}-expired",
            $"{RefreshTokenHash}-expired",
            CreatedOnUtc.AddDays(-9),
            CreatedOnUtc.AddDays(-3),
            CreatedOnUtc.AddDays(-10)).Value.UserTokenId;

        return user;
    }
}
