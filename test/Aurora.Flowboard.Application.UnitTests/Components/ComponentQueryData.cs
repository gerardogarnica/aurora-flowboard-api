namespace Aurora.Flowboard.Application.UnitTests.Components;

internal static class ComponentQueryData
{
    public static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static User GetAdminUser()
    {
        Email email = Email.Create("q.admin@test.com").Value;
        return User.Create("Query", "Admin", email, Password.Create("hashed_password_123").Value, UtcNow).Value;
    }

    public static User GetOtherUser()
    {
        Email email = Email.Create("q.other@test.com").Value;
        return User.Create("Query", "Other", email, Password.Create("hashed_password_123").Value, UtcNow).Value;
    }

    public static Project GetProject(User admin) =>
        Project.Create("Query Project", "Query test project", ProjectCode.Create("QRY").Value, ProjectKind.Product, Color.Create("white").Value, admin, UtcNow).Value;

    public static Project GetProjectWithComponents(User admin, params string[] names)
    {
        Project project = GetProject(admin);

        foreach (string name in names)
        {
            Component.Create(name, project, admin, UtcNow);
        }

        return project;
    }
}
