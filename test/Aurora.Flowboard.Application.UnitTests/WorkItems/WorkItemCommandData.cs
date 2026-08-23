
namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

internal static class WorkItemCommandData
{
    public const string Title = "Implement authentication";
    public const string UpdatedTitle = "Updated authentication task";
    public const string Content = "This is a test comment";
    public const string UpdatedContent = "Updated comment content";
    public const string TagName = "backend";
    public const string ComponentName = "Billing";
    public static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateOnly Today = DateOnly.FromDateTime(UtcNow);
    public static readonly DateOnly EstimatedCompletionDate = new(2026, 12, 31);

    public static User GetAdmin()
    {
        Email email = Email.Create("admin@example.com").Value;
        return User.Create("Admin", "User", email, Password.Create("hashed_password_123").Value, UtcNow).Value;
    }

    public static User GetNonMember()
    {
        Email email = Email.Create("nonmember@example.com").Value;
        return User.Create("Non", "Member", email, Password.Create("hashed_password_456").Value, UtcNow).Value;
    }

    public static User GetAssignee()
    {
        Email email = Email.Create("assignee@example.com").Value;
        return User.Create("Assignee", "User", email, Password.Create("hashed_password_789").Value, UtcNow).Value;
    }

    public static Project GetActiveProjectWithFlow(User admin)
    {
        Project project = Project.Create("My Project", "Desc", ProjectCode.Create("MYP").Value, ProjectKind.Product, Color.Create("white").Value, admin, UtcNow).Value;

        ProjectRole[] allRoles = [ProjectRole.Admin, ProjectRole.Developer];
        Color stateColor = Color.Create("white").Value;
        project.AddFlowState("Backlog", FlowStateCategory.Active, stateColor, allRoles, admin);
        project.AddFlowState("In Progress", FlowStateCategory.Active, stateColor, allRoles, admin);
        project.AddFlowState("Done", FlowStateCategory.Completed, stateColor, allRoles, admin);
        project.AddFlowState("Cancelled", FlowStateCategory.Cancelled, stateColor, allRoles, admin);

        return project;
    }

    public static WorkItem GetWorkItem(User admin)
    {
        Project project = GetActiveProjectWithFlow(admin);

        return WorkItem.Create(
            Title,
            null,
            WorkItemType.Story,
            Priority.Medium,
            project,

            admin,
            null,
            null,
            UtcNow).Value;
    }

    public static WorkItem GetWorkItemWithComment(User admin, out Guid commentId)
    {
        WorkItem workItem = GetWorkItem(admin);
        workItem.AddComment(admin, Content, UtcNow);
        commentId = workItem.Comments.First().Id;
        return workItem;
    }

    public static WorkItem GetWorkItemWithTag(User admin, out Guid tagId)
    {
        WorkItem workItem = GetWorkItem(admin);
        workItem.AddTag(TagName, admin, UtcNow);
        tagId = workItem.Tags.First().Id;
        return workItem;
    }

    public static WorkItem GetWorkItemWithAssignee(User admin, User assignee)
    {
        Project project = GetActiveProjectWithFlow(admin);
        project.AddMember(assignee, ProjectRole.Developer, admin, UtcNow);

        return WorkItem.Create(
            Title,
            null,
            WorkItemType.Story,
            Priority.Medium,
            project,

            admin,
            null,
            null,
            UtcNow,
            assignee).Value;
    }

    public static (WorkItem WorkItem, FlowState ToState) GetWorkItemForMove(User admin)
    {
        Project project = GetActiveProjectWithFlow(admin);

        WorkItem workItem = WorkItem.Create(
            Title,
            null,
            WorkItemType.Story,
            Priority.Medium,
            project,

            admin,
            null,
            null,
            UtcNow).Value;

        // workItem starts at the first active state; toState is "In Progress"
        FlowState toState = project.FlowStates.First(s => s.Name == "In Progress");

        return (workItem, toState);
    }

    public static (WorkItem WorkItem, User Assignee) GetWorkItemForAssign(User admin)
    {
        Project project = GetActiveProjectWithFlow(admin);

        User assignee = GetAssignee();
        project.AddMember(assignee, ProjectRole.Developer, admin, UtcNow);

        WorkItem workItem = WorkItem.Create(
            Title,
            null,
            WorkItemType.Story,
            Priority.Medium,
            project,

            admin,
            null,
            null,
            UtcNow).Value;

        return (workItem, assignee);
    }

    public static WorkItem GetWorkItemWithProjectComponent(User admin, out Component component)
    {
        Project project = GetActiveProjectWithFlow(admin);
        component = Component.Create(ComponentName, project, admin, UtcNow).Value;

        return WorkItem.Create(
            Title,
            null,
            WorkItemType.Story,
            Priority.Medium,
            project,
            admin,
            null,
            null,
            UtcNow).Value;
    }
}
