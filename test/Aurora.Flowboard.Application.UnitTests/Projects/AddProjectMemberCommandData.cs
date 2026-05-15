namespace Aurora.Flowboard.Application.UnitTests.Projects;

internal static class AddProjectMemberCommandData
{
    public static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

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

    public static User GetNewUser()
    {
        Email email = Email.Create("newuser@example.com").Value;
        return User.Create("New", "User", email, "hashed_password_789", UtcNow).Value;
    }

    public static Project GetDraftProject(User admin) =>
        Project.Create("Test Project", "Test Description", "TST", null, admin, UtcNow).Value;

    public static AddProjectMemberCommand GetValidCommand(Guid projectId, Guid userId) =>
        new(projectId, userId, ProjectRole.Developer);
}
