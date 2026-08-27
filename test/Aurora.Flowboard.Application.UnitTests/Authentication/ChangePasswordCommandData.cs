using Aurora.Flowboard.Application.Authentication.ChangePassword;

namespace Aurora.Flowboard.Application.UnitTests.Authentication;

internal static class ChangePasswordCommandData
{
    internal const string CurrentPlainPassword = "P@ssw0rd!";
    internal const string CurrentPasswordHash = "hashed_current_123";
    internal const string NewPlainPassword = "N3wP@ssword!";
    internal const string NewPasswordHash = "hashed_new_456";

    internal static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    internal static ChangePasswordCommand GetCommand(
        string currentPassword = CurrentPlainPassword,
        string newPassword = NewPlainPassword) =>
        new(currentPassword, newPassword);

    internal static User GetUser()
    {
        Email email = Email.Create("john.doe@example.com").Value;
        Password password = Password.Create(CurrentPasswordHash).Value;
        return User.Create("John", "Doe", email, password, UtcNow).Value;
    }
}
