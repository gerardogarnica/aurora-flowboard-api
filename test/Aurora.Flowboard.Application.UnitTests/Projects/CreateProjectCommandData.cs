namespace Aurora.Flowboard.Application.UnitTests.Projects;

internal static class CreateProjectCommandData
{
    public const string Name = "Aurora Flowboard";
    public const string? Description = "Project management API";
    public const string Code = "AFB";
    public static readonly DateOnly EstimatedCompletionDate = new(2026, 12, 31);
    public static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateOnly Today = DateOnly.FromDateTime(UtcNow);

    public static User GetUser()
    {
        Email email = Email.Create("john.doe@example.com").Value;
        return User.Create("John", "Doe", email, "hashed_password_123", UtcNow).Value;
    }

    public static CreateProjectCommand GetValidCommand() =>
        new(Name, Description, Code, EstimatedCompletionDate);
}
