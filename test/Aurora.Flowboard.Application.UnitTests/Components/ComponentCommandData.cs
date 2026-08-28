namespace Aurora.Flowboard.Application.UnitTests.Components;

internal static class ComponentCommandData
{
    public const string Name = "Billing";
    public const string RenamedTo = "Payments";
    public static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static User GetAdmin()
    {
        Email email = Email.Create("admin@example.com").Value;
        return User.Create("Admin", "User", email, Password.Create("hashed_password_123").Value, UtcNow).Value;
    }

    public static User GetNonAdmin()
    {
        Email email = Email.Create("nonadmin@example.com").Value;
        return User.Create("Non", "Admin", email, Password.Create("hashed_password_456").Value, UtcNow).Value;
    }

    public static Project GetProject(User admin) =>
        Project.Create("Test Project", "Test Description", ProjectCode.Create("TST").Value, ProjectKind.Product, Color.Create("white").Value, admin, UtcNow).Value;

    public static Project GetProjectWithComponent(User admin, out Component component)
    {
        Project project = GetProject(admin);
        component = Component.Create(Name, project, admin, UtcNow).Value;
        return project;
    }

    public static CreateComponentCommand GetCreateCommand(Guid projectId) =>
        new(projectId, Name);

    public static RenameComponentCommand GetRenameCommand(Guid componentId) =>
        new(componentId, RenamedTo);

    public static RetireComponentCommand GetRetireCommand(Guid componentId) =>
        new(componentId);
}
