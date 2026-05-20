using System.Reflection;

namespace Aurora.Flowboard.Application.UnitTests.Projects;

internal static class ProjectQueryData
{
    public static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateOnly EstimatedCompletionDate = new(2026, 12, 31);
    public const string Description = "Query test project";

    public static User GetAdminUser()
    {
        Email email = Email.Create("q.admin@test.com").Value;
        return User.Create("Query", "Admin", email, Password.Create("hashed_password_123").Value, UtcNow).Value;
    }

    public static User GetMemberUser()
    {
        Email email = Email.Create("q.member@test.com").Value;
        return User.Create("Query", "Member", email, Password.Create("hashed_password_123").Value, UtcNow).Value;
    }

    public static User GetOtherUser()
    {
        Email email = Email.Create("q.other@test.com").Value;
        return User.Create("Query", "Other", email, Password.Create("hashed_password_123").Value, UtcNow).Value;
    }

    public static User GetAlphaUser()
    {
        Email email = Email.Create("alpha@test.com").Value;
        return User.Create("Alpha", "User", email, Password.Create("hashed_password_123").Value, UtcNow).Value;
    }

    public static User GetZetaUser()
    {
        Email email = Email.Create("zeta@test.com").Value;
        return User.Create("Zeta", "User", email, Password.Create("hashed_password_123").Value, UtcNow).Value;
    }

    // For GetAllProjectsHandler — no navigation properties needed
    public static Project GetProjectForGetAll(string name, User admin) =>
        Project.Create(name, Description, "QRY", EstimatedCompletionDate, admin, UtcNow).Value;

    public static Project GetActiveProjectForGetAll(string name, User admin)
    {
        Project project = Project.Create(name, Description, "QRA", EstimatedCompletionDate, admin, UtcNow).Value;
        project.ChangeStatus(ProjectStatus.Active, admin, UtcNow);
        return project;
    }

    // For GetProjectByIdHandler — navigation properties set via reflection
    public static Project GetProjectWithNavProperties(User admin)
    {
        Project project = Project.Create("Nav Project", Description, "NAV", EstimatedCompletionDate, admin, UtcNow).Value;
        SetCreatorNavProperty(project, admin);
        PopulateNavProperties(project, admin);
        return project;
    }

    public static Project GetProjectWithMemberNavProperties(User admin, User member)
    {
        Project project = Project.Create("Nav Project", Description, "NVM", EstimatedCompletionDate, admin, UtcNow).Value;
        project.AddMember(member, ProjectRole.Developer, admin, UtcNow);
        SetCreatorNavProperty(project, admin);
        PopulateNavProperties(project, admin, member);
        return project;
    }

    public static Project GetProjectWithOrderedMembers(User admin, User alpha, User zeta)
    {
        Project project = Project.Create("Order Project", Description, "ORD", null, admin, UtcNow).Value;
        project.AddMember(alpha, ProjectRole.Developer, admin, UtcNow);
        project.AddMember(zeta, ProjectRole.Developer, admin, UtcNow);
        SetCreatorNavProperty(project, admin);
        PopulateNavProperties(project, admin, alpha, zeta);
        return project;
    }

    public static Project GetProjectWithOrderedChangeLogs(User admin, User member)
    {
        Project project = Project.Create("Log Project", Description, "LOG", null, admin, UtcNow).Value;
        project.AddMember(member, ProjectRole.Developer, admin, UtcNow.AddHours(1));
        SetCreatorNavProperty(project, admin);
        PopulateNavProperties(project, admin, member);
        return project;
    }

    private static void SetCreatorNavProperty(Project project, User creator) =>
        typeof(Project)
            .GetField("<Creator>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(project, creator);

    private static void SetMemberUserNavProperty(ProjectMember member, User user) =>
        typeof(ProjectMember)
            .GetField("<User>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(member, user);

    private static void SetChangeLogChangedByNavProperty(ProjectChangeLog log, User changedBy) =>
        typeof(ProjectChangeLog)
            .GetField("<ChangedBy>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(log, changedBy);

    private static void PopulateNavProperties(Project project, params User[] users)
    {
        Dictionary<Guid, User> usersById = users.ToDictionary(u => u.Id);
        foreach (ProjectMember m in project.Members)
        {
            if (usersById.TryGetValue(m.UserId, out User? u))
            {
                SetMemberUserNavProperty(m, u);
            }
        }
        foreach (ProjectChangeLog cl in project.ChangeLogs)
        {
            if (usersById.TryGetValue(cl.ChangedById, out User? u))
            {
                SetChangeLogChangedByNavProperty(cl, u);
            }
        }
    }
}
