using System.Reflection;

namespace Aurora.Flowboard.Application.UnitTests.Flows;

internal static class FlowQueryData
{
    public static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public const string Description = "Query test flow";

    public static User GetAdminUser()
    {
        Email email = Email.Create("fq.admin@test.com").Value;
        return User.Create("Flow", "Admin", email, Password.Create("hashed_password_123").Value, UtcNow).Value;
    }

    public static Project GetActiveProject(User admin)
    {
        Project project = Project.Create("Flow Project", "Desc", "FLQ", null, admin, UtcNow).Value;
        project.ChangeStatus(ProjectStatus.Active, admin, UtcNow);
        return project;
    }

    public static Flow GetFlowForGetAll(string name, User admin)
    {
        Project project = GetActiveProject(admin);
        return Flow.Create(name, Description, project, false, admin, UtcNow).Value;
    }

    public static Flow GetDeactivatedFlow(string name, User admin)
    {
        Flow flow = GetFlowForGetAll(name, admin);
        flow.Deactivate(admin, UtcNow);
        return flow;
    }

    public static Flow GetFlowWithStatesForGetAll(string name, User admin)
    {
        Project project = GetActiveProject(admin);
        Flow flow = Flow.Create(name, Description, project, false, admin, UtcNow).Value;
        AddStatesAndSetNavProps(flow, admin);
        return flow;
    }

    public static Flow GetSimpleFlowForGetById(User admin)
    {
        Project project = GetActiveProject(admin);
        return Flow.Create("Simple Flow", Description, project, false, admin, UtcNow).Value;
    }

    public static Flow GetFlowWithStatesForGetById(User admin)
    {
        Project project = GetActiveProject(admin);
        Flow flow = Flow.Create("States Flow", Description, project, false, admin, UtcNow).Value;
        AddStatesAndSetNavProps(flow, admin);
        return flow;
    }

    private static void AddStatesAndSetNavProps(Flow flow, User admin)
    {
        ProjectRole[] allRoles = [ProjectRole.Admin, ProjectRole.Developer];
        flow.AddState("Todo", FlowStateCategory.Active, allRoles, admin);
        flow.AddState("Done", FlowStateCategory.Completed, allRoles, admin);
        flow.AddState("Cancelled", FlowStateCategory.Cancelled, allRoles, admin);
        foreach (FlowState state in flow.States)
        {
            SetFlowNavProperty(state, flow);
        }
    }

    private static void SetFlowNavProperty(FlowState state, Flow flow) =>
        typeof(FlowState)
            .GetField("<Flow>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(state, flow);
}
