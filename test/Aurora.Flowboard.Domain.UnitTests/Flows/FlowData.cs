namespace Aurora.Flowboard.Domain.UnitTests.Flows;

internal static class FlowData
{
    public const string Name = "Default Flow";
    public const string Description = "Main workflow";
    public const string StateName = "In Progress";
    public static readonly DateTime CreatedOnUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime UpdatedOnUtc = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    public static Flow GetFlow(User? admin = null, bool isDefault = false)
    {
        User user = admin ?? UserData.GetActiveUser();
        Project project = ProjectData.GetDraftProject(user);
        return Flow.Create(Name, Description, project, isDefault, CreatedOnUtc).Value;
    }

    public static Flow GetDeactivatedFlow(User? admin = null)
    {
        User user = admin ?? UserData.GetActiveUser();
        Flow flow = GetFlow(user, isDefault: false);
        flow.Deactivate(user, UpdatedOnUtc);
        return flow;
    }

    // Returns a flow whose project is in OnHold status (CanAddOrUpdateFlow returns false).
    public static (Flow Flow, User Admin) GetFlowOnBlockedProject()
    {
        User admin = UserData.GetActiveUser();
        Project project = ProjectData.GetProjectWithStatus(ProjectStatus.Active, admin);
        Flow flow = Flow.Create(Name, Description, project, false, CreatedOnUtc).Value;
        project.ChangeStatus(ProjectStatus.OnHold, admin, ProjectData.UpdatedOnUtc);
        return (flow, admin);
    }
}
