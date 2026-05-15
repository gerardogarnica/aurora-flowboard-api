namespace Aurora.Flowboard.Application.UnitTests.Projects;

internal static class UpdateProjectCommandData
{
    public const string UpdatedName = "Updated Flowboard";
    public const string? UpdatedDescription = "Updated project description";
    public static readonly DateOnly EstimatedCompletionDate = new(2026, 12, 31);
    public static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateOnly Today = DateOnly.FromDateTime(UtcNow);

    public static User GetAdmin()
    {
        Email email = Email.Create("admin@example.com").Value;
        return User.Create("Admin", "User", email, "hashed_password_123", UtcNow).Value;
    }

    public static User GetNonAdmin()
    {
        Email email = Email.Create("nonadmin@example.com").Value;
        return User.Create("Non", "Admin", email, "hashed_password_456", UtcNow).Value;
    }

    public static Project GetDraftProject(User admin) =>
        Project.Create("Original Name", "Original Description", "OPN", null, admin, UtcNow).Value;

    public static UpdateProjectCommand GetValidCommand(Guid projectId) =>
        new(projectId, UpdatedName, UpdatedDescription, EstimatedCompletionDate);
}
