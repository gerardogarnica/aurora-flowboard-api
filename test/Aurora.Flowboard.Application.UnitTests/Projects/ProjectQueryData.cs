using System.Reflection;

namespace Aurora.Flowboard.Application.UnitTests.Projects;

internal static class ProjectQueryData
{
    public static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateOnly EstimatedCompletionDate = new(2026, 12, 31);
    public const string Description = "Query test project";
    public static readonly Color Color = Color.Create("white").Value;

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

    // For GetAllProjectsHandler
    public static Project GetProjectForGetAll(string name, User admin)
    {
        Project project = Project.Create(name, Description, "QRY", Color, EstimatedCompletionDate, admin, UtcNow).Value;
        PopulateMemberUserNavProperties(project, admin);
        return project;
    }

    public static Flow GetFlowForProject(string name, Project project, bool isDefault, User admin)
    {
        project.ChangeStatus(ProjectStatus.Active, admin, UtcNow);
        Flow flow = Flow.Create(name, "Flow description", project, isDefault, admin, UtcNow).Value;
        AddFlowToProject(project, flow);
        return flow;
    }

    private static void AddFlowToProject(Project project, Flow flow)
    {
        var flows = (List<Flow>?)typeof(Project)
            .GetField("_flows", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(project);
        flows?.Add(flow);
    }

    public static Project GetActiveProjectForGetAll(string name, User admin)
    {
        Project project = Project.Create(name, Description, "QRA", Color, EstimatedCompletionDate, admin, UtcNow).Value;
        project.ChangeStatus(ProjectStatus.Active, admin, UtcNow);
        PopulateMemberUserNavProperties(project, admin);
        return project;
    }

    private static void PopulateMemberUserNavProperties(Project project, params User[] users)
    {
        Dictionary<Guid, User> usersById = users.ToDictionary(u => u.Id);
        foreach (ProjectMember m in project.Members)
        {
            if (usersById.TryGetValue(m.UserId, out User? u))
            {
                SetMemberUserNavProperty(m, u);
            }
        }
    }

    public static Project GetProjectForGetAllWithWorkItems(string name, User admin, int openCount, int closedCount, int cancelledCount = 0)
    {
        Project project = GetProjectForGetAll(name, admin);
        AddWorkItemsWithCategory(project, openCount, FlowStateCategory.Active);
        AddWorkItemsWithCategory(project, closedCount, FlowStateCategory.Completed);
        AddWorkItemsWithCategory(project, cancelledCount, FlowStateCategory.Cancelled);
        return project;
    }

    // For GetProjectByIdHandler — navigation properties set via reflection
    public static Project GetProjectWithNavProperties(User admin)
    {
        Project project = Project.Create("Nav Project", Description, "NAV", Color, EstimatedCompletionDate, admin, UtcNow).Value;
        SetCreatorNavProperty(project, admin);
        PopulateNavProperties(project, admin);
        return project;
    }

    public static Project GetProjectWithMemberNavProperties(User admin, User member)
    {
        Project project = Project.Create("Nav Project", Description, "NVM", Color, EstimatedCompletionDate, admin, UtcNow).Value;
        project.AddMember(member, ProjectRole.Developer, admin, UtcNow);
        SetCreatorNavProperty(project, admin);
        PopulateNavProperties(project, admin, member);
        return project;
    }

    public static Project GetProjectWithOrderedMembers(User admin, User alpha, User zeta)
    {
        Project project = Project.Create("Order Project", Description, "ORD", Color, null, admin, UtcNow).Value;
        project.AddMember(alpha, ProjectRole.Developer, admin, UtcNow);
        project.AddMember(zeta, ProjectRole.Developer, admin, UtcNow);
        SetCreatorNavProperty(project, admin);
        PopulateNavProperties(project, admin, alpha, zeta);
        return project;
    }

    public static Project GetProjectWithWorkItemCounts(User admin, int openCount, int closedCount, int cancelledCount = 0)
    {
        Project project = GetProjectWithNavProperties(admin);
        AddWorkItemsWithCategory(project, openCount, FlowStateCategory.Active);
        AddWorkItemsWithCategory(project, closedCount, FlowStateCategory.Completed);
        AddWorkItemsWithCategory(project, cancelledCount, FlowStateCategory.Cancelled);
        return project;
    }

    public static Project GetProjectWithOrderedChangeLogs(User admin, User member)
    {
        Project project = Project.Create("Log Project", Description, "LOG", Color, null, admin, UtcNow).Value;
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

    private static void AddWorkItemsWithCategory(Project project, int count, FlowStateCategory category)
    {
        for (int i = 0; i < count; i++)
        {
            FlowState state = CreateFlowStateWithCategory(category);
            var workItem = (WorkItem)Activator.CreateInstance(typeof(WorkItem), nonPublic: true)!;
            typeof(WorkItem)
                .GetField("<FlowState>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(workItem, state);
            var workItems = (List<WorkItem>?)typeof(Project)
                .GetField("_workItems", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(project);
            workItems?.Add(workItem);
        }
    }

    private static FlowState CreateFlowStateWithCategory(FlowStateCategory category)
    {
        var state = (FlowState)Activator.CreateInstance(typeof(FlowState), nonPublic: true)!;
        typeof(FlowState)
            .GetField("<Category>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(state, category);
        return state;
    }
}
