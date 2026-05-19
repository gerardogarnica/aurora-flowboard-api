using System.Reflection;

namespace Aurora.Flowboard.Application.UnitTests.Flows;

internal static class FlowCommandData
{
    public const string FlowName = "Sprint Flow";
    public const string FlowDescription = "Standard sprint workflow";
    public const string UpdatedName = "Updated Sprint Flow";
    public const string? UpdatedDescription = "Updated sprint workflow";
    public const string StateName = "In Review";
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

    public static Project GetActiveProject(User admin)
    {
        Project project = Project.Create("My Project", "Desc", "MYP", null, admin, UtcNow).Value;
        project.ChangeStatus(ProjectStatus.Active, admin, UtcNow);
        return project;
    }

    public static Flow GetFlow(User admin)
    {
        Project project = GetActiveProject(admin);
        return Flow.Create(FlowName, FlowDescription, project, false, admin, UtcNow).Value;
    }

    public static Flow GetFlowWithStates(User admin)
    {
        Project project = GetActiveProject(admin);
        Flow flow = Flow.Create(FlowName, FlowDescription, project, false, admin, UtcNow).Value;

        ProjectRole[] allRoles = [ProjectRole.Admin, ProjectRole.Developer];
        flow.AddState("Todo", FlowStateCategory.Active, allRoles, admin);
        flow.AddState("Done", FlowStateCategory.Completed, allRoles, admin);
        flow.AddState("Cancelled", FlowStateCategory.Cancelled, allRoles, admin);

        foreach (FlowState state in flow.States)
        {
            SetFlowNavProperty(state, flow);
        }

        return flow;
    }

    public static Flow GetFlowWithTransition(User admin, out FlowTransition transition)
    {
        Flow flow = GetFlowWithStates(admin);
        transition = flow.Transitions.First();
        return flow;
    }

    private static void SetFlowNavProperty(FlowState state, Flow flow) =>
        typeof(FlowState)
            .GetField("<Flow>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(state, flow);
}
