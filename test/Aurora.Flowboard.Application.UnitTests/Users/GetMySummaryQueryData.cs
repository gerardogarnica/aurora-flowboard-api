using System.Reflection;

namespace Aurora.Flowboard.Application.UnitTests.Users;

internal static class GetMySummaryQueryData
{
    public static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private const string Description = "Summary test project";
    private static readonly Color Color = Color.Create("white").Value;

    public static User GetUser(string firstName = "Caller", string lastName = "User", Role? role = null)
    {
        Email email = Email.Create($"{firstName.ToLowerInvariant()}.{lastName.ToLowerInvariant()}@test.com").Value;
        User user = User.Create(firstName, lastName, email, Password.Create("hashed_password_123").Value, UtcNow).Value;
        user.AssignRole(role ?? Role.Member);
        return user;
    }

    public static Project CreateProject(
        string name,
        string code,
        ProjectStatus status,
        User admin,
        params User[] additionalMembers)
    {
        // Completed is only reachable for timeboxed kinds (Client/Research); Maintenance and a
        // direct Active-to-Archived transition are only reachable for continuous kinds (Product/Internal).
        ProjectKind kind = status == ProjectStatus.Completed ? ProjectKind.Client : ProjectKind.Product;

        Project project = Project.Create(
            name, Description, ProjectCode.Create(code).Value, kind, Color, admin, UtcNow).Value;

        foreach (User member in additionalMembers)
        {
            project.AddMember(member, ProjectRole.Developer, admin, UtcNow);
        }

        if (status != ProjectStatus.Active)
        {
            project.ChangeStatus(status, admin, UtcNow);
        }

        return project;
    }

    public static void AddWorkItem(Project project, Guid assigneeId, FlowStateCategory category)
    {
        FlowState state = CreateFlowStateWithCategory(category);
        var workItem = (WorkItem)Activator.CreateInstance(typeof(WorkItem), nonPublic: true)!;

        typeof(WorkItem)
            .GetField("<FlowState>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(workItem, state);
        typeof(WorkItem)
            .GetField("<AssigneeId>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(workItem, assigneeId);

        var workItems = (List<WorkItem>?)typeof(Project)
            .GetField("_workItems", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(project);
        workItems?.Add(workItem);
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
