using System.Reflection;

namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

internal static class WorkItemQueryData
{
    public static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public const string CommentContent = "Test comment";
    public const string TagName = "query-tag";

    public static User GetAdminUser()
    {
        Email email = Email.Create("wi.admin@test.com").Value;
        return User.Create("Work", "Admin", email, Password.Create("hashed_password_123").Value, UtcNow).Value;
    }

    public static User GetAssigneeUser()
    {
        Email email = Email.Create("wi.assignee@test.com").Value;
        return User.Create("Work", "Assignee", email, Password.Create("hashed_password_123").Value, UtcNow).Value;
    }

    public static (Project Project, WorkItem WorkItem) GetProjectAndWorkItem(User admin)
    {
        (Project project, Flow flow) = GetActiveProjectWithFlow(admin);
        WorkItem workItem = WorkItem.Create("Test Work Item", null, WorkItemType.Story, Priority.Medium, project, flow, admin, null, null, UtcNow).Value;
        return (project, workItem);
    }

    public static (Project Project, WorkItem WorkItem) GetProjectAndWorkItemWithAssignee(User admin, User assignee)
    {
        (Project project, Flow flow) = GetActiveProjectWithFlow(admin);
        project.AddMember(assignee, ProjectRole.Developer, admin, UtcNow);
        WorkItem workItem = WorkItem.Create("Test Work Item", null, WorkItemType.Story, Priority.Medium, project, flow, admin, null, null, UtcNow, assignee).Value;
        return (project, workItem);
    }

    public static (Project Project, WorkItem WorkItem) GetProjectAndWorkItemWithComment(User admin)
    {
        (Project project, WorkItem workItem) = GetProjectAndWorkItem(admin);
        workItem.AddComment(admin, CommentContent, UtcNow);
        return (project, workItem);
    }

    public static (Project Project, WorkItem WorkItem) GetProjectAndWorkItemWithTag(User admin)
    {
        (Project project, WorkItem workItem) = GetProjectAndWorkItem(admin);
        workItem.AddTag(TagName, admin, UtcNow);
        return (project, workItem);
    }

    public static (Project Project, WorkItem WorkItem) GetProjectAndWorkItemWithTimeEntry(User admin)
    {
        (Project project, WorkItem workItem) = GetProjectAndWorkItem(admin);
        workItem.LogTime(admin, 2.5m, "Work done", UtcNow, UtcNow);
        return (project, workItem);
    }

    public static (Project Project, WorkItem WorkItem1, WorkItem WorkItem2) GetProjectWithTwoWorkItems(User admin)
    {
        (Project project, Flow flow) = GetActiveProjectWithFlow(admin);
        WorkItem workItem1 = WorkItem.Create("Alpha Item", null, WorkItemType.Story, Priority.Low, project, flow, admin, null, null, UtcNow).Value;
        WorkItem workItem2 = WorkItem.Create("Beta Item", null, WorkItemType.Bug, Priority.High, project, flow, admin, null, null, UtcNow.AddHours(1)).Value;
        return (project, workItem1, workItem2);
    }

    private static (Project, Flow) GetActiveProjectWithFlow(User admin)
    {
        Project project = Project.Create("WI Project", "Desc", "WIP", null, admin, UtcNow).Value;
        project.ChangeStatus(ProjectStatus.Active, admin, UtcNow);
        Flow flow = Flow.Create("Sprint Flow", null, project, false, admin, UtcNow).Value;
        ProjectRole[] allRoles = [ProjectRole.Admin, ProjectRole.Developer];
        flow.AddState("Todo", FlowStateCategory.Active, allRoles, admin);
        flow.AddState("Done", FlowStateCategory.Completed, allRoles, admin);
        flow.AddState("Cancelled", FlowStateCategory.Cancelled, allRoles, admin);
        foreach (FlowState state in flow.States)
        {
            SetFlowNavProperty(state, flow);
        }
        return (project, flow);
    }

    private static void SetFlowNavProperty(FlowState state, Flow flow) =>
        typeof(FlowState)
            .GetField("<Flow>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(state, flow);
}
