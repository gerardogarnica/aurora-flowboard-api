namespace Aurora.Flowboard.Application.UnitTests.Projects;

internal static class ChangeProjectStatusCommandData
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

    public static Project GetDraftProject(User admin) =>
        Project.Create("Test Project", "Test Description", "TST", null, admin, UtcNow).Value;

    public static Project GetArchivedProject(User admin)
    {
        Project project = GetDraftProject(admin);
        project.ChangeStatus(ProjectStatus.Archived, admin, UtcNow);
        return project;
    }

    public static ChangeProjectStatusCommand GetValidCommand(Guid projectId) =>
        new(projectId, ProjectStatus.Active);
}
