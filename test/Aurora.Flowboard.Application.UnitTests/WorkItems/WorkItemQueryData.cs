using System.Collections;
using System.Reflection;

namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

internal static class WorkItemQueryData
{
    public static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public const string CommentContent = "Test comment";
    public const string TagName = "query-tag";
    public const string MilestoneColorValue = "#F97316";
    public static readonly Color MilestoneColor = Color.Create(MilestoneColorValue).Value;

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

    public static User GetDeveloperUser()
    {
        Email email = Email.Create("wi.developer@test.com").Value;
        return User.Create("Work", "Developer", email, Password.Create("hashed_password_123").Value, UtcNow).Value;
    }

    public static (Project Project, WorkItem WorkItem) GetProjectAndWorkItem(User admin)
    {
        Project project = GetActiveProjectWithFlow(admin);
        WorkItem workItem = WorkItem.Create("Test Work Item", null, WorkItemType.Story, Priority.Medium, project, admin, null, null, UtcNow).Value;
        return (project, workItem);
    }

    public static (Project Project, WorkItem WorkItem) GetProjectAndWorkItemWithAssignee(User admin, User assignee)
    {
        Project project = GetActiveProjectWithFlow(admin);
        project.AddMember(assignee, ProjectRole.Developer, admin, UtcNow);
        WorkItem workItem = WorkItem.Create("Test Work Item", null, WorkItemType.Story, Priority.Medium, project, admin, null, null, UtcNow, assignee).Value;
        return (project, workItem);
    }

    public static (Project Project, WorkItem WorkItem) GetProjectAndWorkItemWithComment(User admin)
    {
        (Project project, WorkItem workItem) = GetProjectAndWorkItem(admin);
        workItem.AddComment(admin, CommentContent, UtcNow);
        return (project, workItem);
    }

    public static (Project Project, WorkItem WorkItem) GetProjectAndWorkItemWithStateHistory(User admin)
    {
        (Project project, WorkItem workItem) = GetProjectAndWorkItem(admin);
        FlowState doneState = project.FlowStates.Single(s => s.Name == "Done");
        workItem.Move(doneState, admin, "Ready for QA", UtcNow.AddHours(1));
        return (project, workItem);
    }

    public static (Project Project, WorkItem WorkItem) GetProjectAndWorkItemWithThreeComments(User admin)
    {
        (Project project, WorkItem workItem) = GetProjectAndWorkItem(admin);
        workItem.AddComment(admin, "oldest comment", UtcNow);
        workItem.AddComment(admin, "middle comment", UtcNow.AddHours(1));
        workItem.AddComment(admin, "newest comment", UtcNow.AddHours(2));
        return (project, workItem);
    }

    public static (Project Project, WorkItem WorkItem) GetProjectAndWorkItemWithThreeTimeEntries(User admin)
    {
        (Project project, WorkItem workItem) = GetProjectAndWorkItem(admin);
        workItem.LogTime(admin, 1m, "oldest entry", UtcNow, UtcNow);
        workItem.LogTime(admin, 2m, "middle entry", UtcNow.AddHours(1), UtcNow.AddHours(1));
        workItem.LogTime(admin, 3m, "newest entry", UtcNow.AddHours(2), UtcNow.AddHours(2));
        return (project, workItem);
    }

    public static (Project Project, WorkItem WorkItem) GetProjectAndWorkItemWithThreeStateTransitions(User admin)
    {
        // Three consecutive Active states get bidirectional transitions between neighbours,
        // so the work item can be moved along the whole chain and end on the Completed state.
        FlowStateDefinition[] flowStates = FlowStateDefinitionData.GetFlow(
            ("Backlog", FlowStateCategory.Active),
            ("In Progress", FlowStateCategory.Active),
            ("Review", FlowStateCategory.Active),
            ("Done", FlowStateCategory.Completed),
            ("Cancelled", FlowStateCategory.Cancelled));
        Project project = Project.Create("WI Project", "Desc", ProjectCode.Create("WIP").Value, ProjectKind.Product, Color.Create("white").Value, flowStates, admin, UtcNow).Value;

        WorkItem workItem = WorkItem.Create("Test Work Item", null, WorkItemType.Story, Priority.Medium, project, admin, null, null, UtcNow).Value;

        workItem.Move(project.FlowStates.Single(s => s.Name == "In Progress"), admin, "started", UtcNow.AddHours(1));
        workItem.Move(project.FlowStates.Single(s => s.Name == "Review"), admin, "ready for review", UtcNow.AddHours(2));
        workItem.Move(project.FlowStates.Single(s => s.Name == "Done"), admin, "shipped", UtcNow.AddHours(3));

        return (project, workItem);
    }

    public static (Project Project, WorkItem WorkItem, Component Component, Milestone Milestone) GetProjectAndWorkItemWithComponentAndMilestone(User admin)
    {
        Project project = GetActiveProjectWithFlow(admin);
        Component component = Component.Create("Auth Module", project, admin, UtcNow).Value;
        Milestone milestone = Milestone.Create("Sprint 1", null, MilestoneColor, null, null, project, admin, UtcNow).Value;
        WorkItem workItem = WorkItem.Create(
            "Test Work Item", null, WorkItemType.Story, Priority.Medium, project, admin, null, null, UtcNow,
            milestone: milestone, component: component).Value;
        return (project, workItem, component, milestone);
    }

    public static (Project Project, WorkItem WorkItem, Component Component, Milestone Milestone) GetProjectAndWorkItemWithAllChangeLogTypes(User admin, User assignee)
    {
        Project project = GetActiveProjectWithFlow(admin);
        project.AddMember(assignee, ProjectRole.Developer, admin, UtcNow);
        Component component = Component.Create("Auth Module", project, admin, UtcNow).Value;
        Milestone milestone = Milestone.Create("Sprint 1", null, MilestoneColor, null, null, project, admin, UtcNow).Value;
        WorkItem workItem = WorkItem.Create("Test Work Item", null, WorkItemType.Story, Priority.Medium, project, admin, null, null, UtcNow).Value;

        workItem.Assign(assignee, admin, UtcNow.AddMinutes(1));

        FlowState doneState = project.FlowStates.Single(s => s.Name == "Done");
        workItem.Move(doneState, admin, null, UtcNow.AddMinutes(2));

        workItem.ChangeComponent(component, admin, UtcNow.AddMinutes(3));
        workItem.ChangeMilestone(milestone, admin, UtcNow.AddMinutes(4));

        return (project, workItem, component, milestone);
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

    public static (Project Project, WorkItem WorkItem) GetProjectAndWorkItemWithRoleRestrictedTransition(User admin, User developer)
    {
        Project project = GetActiveProjectWithFlow(admin);
        project.AddMember(developer, ProjectRole.Developer, admin, UtcNow);

        Guid backlogStateId = project.FlowStates.Single(s => s.Name == "Backlog").Id;
        Guid doneStateId = project.FlowStates.Single(s => s.Name == "Done").Id;
        FlowTransition backlogToDone = project.FlowTransitions.Single(t => t.FromStateId == backlogStateId && t.ToStateId == doneStateId);
        project.RemoveFlowTransitionRole(backlogToDone.Id, ProjectRole.Developer, admin);

        WorkItem workItem = WorkItem.Create("Test Work Item", null, WorkItemType.Story, Priority.Medium, project, admin, null, null, UtcNow).Value;
        return (project, workItem);
    }

    public static (Project Project, WorkItem WorkItem1, WorkItem WorkItem2) GetProjectWithTwoWorkItems(User admin)
    {
        Project project = GetActiveProjectWithFlow(admin);
        WorkItem workItem1 = WorkItem.Create("Alpha Item", null, WorkItemType.Story, Priority.Low, project, admin, null, null, UtcNow).Value;
        WorkItem workItem2 = WorkItem.Create("Beta Item", null, WorkItemType.Bug, Priority.High, project, admin, null, null, UtcNow.AddHours(1)).Value;
        return (project, workItem1, workItem2);
    }

    public static Project GetActiveProjectWithFlow(User admin) =>
        Project.Create("WI Project", "Desc", ProjectCode.Create("WIP").Value, ProjectKind.Product, Color.Create("white").Value, FlowStateDefinitionData.GetMinimalFlow(), admin, UtcNow).Value;

    // A project with no flow states, like those created before Project.Create required an initial
    // flow, which may still exist in the database. The domain cannot build one, so the private
    // collections are cleared by reflection.
    public static Project GetLegacyProjectWithoutFlow(User admin)
    {
        Project project = GetActiveProjectWithFlow(admin);
        ClearPrivateList(project, "_flowStates");
        ClearPrivateList(project, "_flowTransitions");

        return project;
    }

    private static void ClearPrivateList(Project project, string fieldName)
    {
        IList list = typeof(Project).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(project) as IList
            ?? throw new InvalidOperationException($"Project has no list field '{fieldName}'; update {nameof(GetLegacyProjectWithoutFlow)} if it was renamed.");
        list.Clear();
    }

    public static void SetWorkItemFlowState(WorkItem workItem, Guid flowStateId) =>
        typeof(WorkItem)
            .GetField("<FlowStateId>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(workItem, flowStateId);
}
