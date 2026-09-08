using Aurora.Flowboard.Application.UnitTests.WorkItems;

namespace Aurora.Flowboard.Application.UnitTests.Projects;

public sealed class GetProjectBoardHandlerTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IUserContext _userContext;
    private readonly GetProjectBoardHandler _handler;

    public GetProjectBoardHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _userContext = Substitute.For<IUserContext>();
        _handler = new GetProjectBoardHandler(_dbContext, _userContext);
    }

    [Fact]
    public async Task Should_ReturnProjectNotFoundError_When_ProjectDoesNotExist()
    {
        // Arrange
        _userContext.UserId.Returns(Guid.NewGuid());
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Project>());
        _dbContext.Projects.Returns(projectsMock);

        // Act
        Result<IReadOnlyCollection<BoardColumnResponse>> result =
            await _handler.Handle(new GetProjectBoardQuery(Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.NotFound);
    }

    [Fact]
    public async Task Should_ReturnProjectNotFoundError_When_UserIsNotMember()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        User other = WorkItemQueryData.GetDeveloperUser();
        Project project = WorkItemQueryData.GetActiveProjectWithFlow(admin);
        _userContext.UserId.Returns(other.Id);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        _dbContext.Projects.Returns(projectsMock);

        // Act
        Result<IReadOnlyCollection<BoardColumnResponse>> result =
            await _handler.Handle(new GetProjectBoardQuery(project.Id), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.NotFound);
    }

    [Fact]
    public async Task Should_ReturnOnlyActiveColumns_When_ProjectHasTerminalFlowStates()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        Project project = WorkItemQueryData.GetActiveProjectWithFlow(admin);

        _userContext.UserId.Returns(admin.Id);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<FlowState> statesMock = MockDbSetHelper.CreateMockDbSet(project.FlowStates);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<WorkItem>());
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<User>());
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.FlowStates.Returns(statesMock);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);
        DbSet<Component> componentsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Component>());
        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Milestone>());
        _dbContext.Components.Returns(componentsMock);
        _dbContext.Milestones.Returns(milestonesMock);

        // Act
        Result<IReadOnlyCollection<BoardColumnResponse>> result =
            await _handler.Handle(new GetProjectBoardQuery(project.Id), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.Should().OnlyContain(c => c.Category == FlowStateCategory.Active);
        result.Value.Should().ContainSingle(c => c.FlowStateName == "Backlog");
    }

    [Fact]
    public async Task Should_ExcludeWorkItem_When_ItsFlowStateIsCancelled()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        Project project = WorkItemQueryData.GetActiveProjectWithFlow(admin);
        FlowState cancelledState = project.FlowStates.Single(s => s.Category == FlowStateCategory.Cancelled);
        WorkItem workItem = WorkItem.Create("Item", null, WorkItemType.Story, Priority.Medium, project, admin, null, null, WorkItemQueryData.UtcNow).Value;
        WorkItemQueryData.SetWorkItemFlowState(workItem, cancelledState.Id);

        _userContext.UserId.Returns(admin.Id);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<FlowState> statesMock = MockDbSetHelper.CreateMockDbSet(project.FlowStates);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<User>());
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.FlowStates.Returns(statesMock);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);
        DbSet<Component> componentsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Component>());
        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Milestone>());
        _dbContext.Components.Returns(componentsMock);
        _dbContext.Milestones.Returns(milestonesMock);

        // Act
        Result<IReadOnlyCollection<BoardColumnResponse>> result =
            await _handler.Handle(new GetProjectBoardQuery(project.Id), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.SelectMany(c => c.WorkItems).Should().BeEmpty();
    }

    [Fact]
    public async Task Should_ExcludeWorkItem_When_ItsFlowStateIsCompleted()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        Project project = WorkItemQueryData.GetActiveProjectWithFlow(admin);
        FlowState completedState = project.FlowStates.Single(s => s.Category == FlowStateCategory.Completed);
        WorkItem workItem = WorkItem.Create("Item", null, WorkItemType.Story, Priority.Medium, project, admin, null, null, WorkItemQueryData.UtcNow).Value;
        WorkItemQueryData.SetWorkItemFlowState(workItem, completedState.Id);

        _userContext.UserId.Returns(admin.Id);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<FlowState> statesMock = MockDbSetHelper.CreateMockDbSet(project.FlowStates);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<User>());
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.FlowStates.Returns(statesMock);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);
        DbSet<Component> componentsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Component>());
        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Milestone>());
        _dbContext.Components.Returns(componentsMock);
        _dbContext.Milestones.Returns(milestonesMock);

        // Act
        Result<IReadOnlyCollection<BoardColumnResponse>> result =
            await _handler.Handle(new GetProjectBoardQuery(project.Id), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.SelectMany(c => c.WorkItems).Should().BeEmpty();
    }

    [Fact]
    public async Task Should_SortColumns_BySortOrder()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        Color color = Color.Create("white").Value;
        Project project = Project.Create("Board Project", "Desc", ProjectCode.Create("BRD").Value, ProjectKind.Product, color, admin, WorkItemQueryData.UtcNow).Value;
        ProjectRole[] roles = [ProjectRole.Admin, ProjectRole.Developer];
        project.AddFlowState("Backlog", FlowStateCategory.Active, color, roles, admin);
        project.AddFlowState("In Progress", FlowStateCategory.Active, color, roles, admin);
        project.AddFlowState("QA", FlowStateCategory.Active, color, roles, admin);
        project.AddFlowState("Done", FlowStateCategory.Completed, color, roles, admin);
        project.AddFlowState("Cancelled", FlowStateCategory.Cancelled, color, roles, admin);

        _userContext.UserId.Returns(admin.Id);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<FlowState> statesMock = MockDbSetHelper.CreateMockDbSet(project.FlowStates);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<WorkItem>());
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<User>());
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.FlowStates.Returns(statesMock);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);
        DbSet<Component> componentsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Component>());
        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Milestone>());
        _dbContext.Components.Returns(componentsMock);
        _dbContext.Milestones.Returns(milestonesMock);

        // Act
        Result<IReadOnlyCollection<BoardColumnResponse>> result =
            await _handler.Handle(new GetProjectBoardQuery(project.Id), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        List<BoardColumnResponse> columns = result.Value.ToList();
        columns.Should().OnlyContain(c => c.Category == FlowStateCategory.Active);
        columns.Select(c => c.FlowStateName).Should().ContainInOrder("Backlog", "In Progress", "QA");
        columns.Select(c => c.SortOrder).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Should_GroupWorkItemsByFlowState()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        Project project = WorkItemQueryData.GetActiveProjectWithFlow(admin);
        project.AddFlowState("In Progress", FlowStateCategory.Active, Color.Create("white").Value, [ProjectRole.Admin, ProjectRole.Developer], admin);
        FlowState todoState = project.FlowStates.Single(s => s.Name == "Backlog");
        FlowState inProgressState = project.FlowStates.Single(s => s.Name == "In Progress");

        WorkItem wi1 = WorkItem.Create("Item 1", null, WorkItemType.Story, Priority.Medium, project, admin, null, null, WorkItemQueryData.UtcNow).Value;
        WorkItem wi2 = WorkItem.Create("Item 2", null, WorkItemType.Bug, Priority.High, project, admin, null, null, WorkItemQueryData.UtcNow.AddHours(1)).Value;
        WorkItemQueryData.SetWorkItemFlowState(wi2, inProgressState.Id);

        _userContext.UserId.Returns(admin.Id);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<FlowState> statesMock = MockDbSetHelper.CreateMockDbSet(project.FlowStates);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([wi1, wi2]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<User>());
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.FlowStates.Returns(statesMock);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);
        DbSet<Component> componentsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Component>());
        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Milestone>());
        _dbContext.Components.Returns(componentsMock);
        _dbContext.Milestones.Returns(milestonesMock);

        // Act
        Result<IReadOnlyCollection<BoardColumnResponse>> result =
            await _handler.Handle(new GetProjectBoardQuery(project.Id), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        BoardColumnResponse todoColumn = result.Value.Single(s => s.FlowStateId == todoState.Id);
        BoardColumnResponse inProgressColumn = result.Value.Single(s => s.FlowStateId == inProgressState.Id);
        todoColumn.WorkItems.Should().ContainSingle(w => w.WorkItemId == wi1.Id);
        inProgressColumn.WorkItems.Should().ContainSingle(w => w.WorkItemId == wi2.Id);
    }

    [Fact]
    public async Task Should_SortWorkItems_ByPriorityDescThenCreatedOnUtcAsc()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        Project project = WorkItemQueryData.GetActiveProjectWithFlow(admin);
        FlowState todoState = project.FlowStates.Single(s => s.Name == "Backlog");

        WorkItem lowPriority = WorkItem.Create("Low", null, WorkItemType.Story, Priority.Low, project, admin, null, null, WorkItemQueryData.UtcNow).Value;
        WorkItem criticalEarly = WorkItem.Create("Critical Early", null, WorkItemType.Story, Priority.Critical, project, admin, null, null, WorkItemQueryData.UtcNow.AddHours(1)).Value;
        WorkItem criticalLate = WorkItem.Create("Critical Late", null, WorkItemType.Story, Priority.Critical, project, admin, null, null, WorkItemQueryData.UtcNow.AddHours(2)).Value;
        WorkItem highPriority = WorkItem.Create("High", null, WorkItemType.Story, Priority.High, project, admin, null, null, WorkItemQueryData.UtcNow.AddHours(3)).Value;

        _userContext.UserId.Returns(admin.Id);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<FlowState> statesMock = MockDbSetHelper.CreateMockDbSet(project.FlowStates);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([lowPriority, criticalLate, highPriority, criticalEarly]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<User>());
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.FlowStates.Returns(statesMock);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);
        DbSet<Component> componentsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Component>());
        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Milestone>());
        _dbContext.Components.Returns(componentsMock);
        _dbContext.Milestones.Returns(milestonesMock);

        // Act
        Result<IReadOnlyCollection<BoardColumnResponse>> result =
            await _handler.Handle(new GetProjectBoardQuery(project.Id), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        IReadOnlyCollection<BoardWorkItemResponse> items = result.Value.Single(s => s.FlowStateId == todoState.Id).WorkItems;
        items.Select(w => w.Priority).Should().ContainInOrder(
            Priority.Critical, Priority.Critical, Priority.High, Priority.Low);
        items.Where(w => w.Priority == Priority.Critical)
            .Select(w => w.CreatedOnUtc)
            .Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Should_MapAssigneeInitialsAndFullName_When_WorkItemIsAssigned()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        User assignee = WorkItemQueryData.GetAssigneeUser();
        Project project = WorkItemQueryData.GetActiveProjectWithFlow(admin);
        project.AddMember(assignee, ProjectRole.Developer, admin, WorkItemQueryData.UtcNow);
        WorkItem workItem = WorkItem.Create("Item", null, WorkItemType.Story, Priority.Medium, project, admin, null, null, WorkItemQueryData.UtcNow, assignee).Value;

        _userContext.UserId.Returns(admin.Id);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<FlowState> statesMock = MockDbSetHelper.CreateMockDbSet(project.FlowStates);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([assignee]);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.FlowStates.Returns(statesMock);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);
        DbSet<Component> componentsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Component>());
        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Milestone>());
        _dbContext.Components.Returns(componentsMock);
        _dbContext.Milestones.Returns(milestonesMock);

        // Act
        Result<IReadOnlyCollection<BoardColumnResponse>> result =
            await _handler.Handle(new GetProjectBoardQuery(project.Id), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        BoardWorkItemResponse summary = result.Value.SelectMany(s => s.WorkItems).Single();
        summary.AssigneeId.Should().Be(assignee.Id);
        summary.AssigneeInitials.Should().Be("WA");
        summary.AssigneeFullName.Should().Be("Work Assignee");
    }

    [Fact]
    public async Task Should_MapAllWorkItemFields_When_WorkItemExists()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        Project project = WorkItemQueryData.GetActiveProjectWithFlow(admin);
        FlowState todoState = project.FlowStates.Single(s => s.Name == "Backlog");
        WorkItem workItem = WorkItem.Create("Test Work Item", null, WorkItemType.Story, Priority.Medium, project, admin, null, null, WorkItemQueryData.UtcNow).Value;
        workItem.AddComment(admin, WorkItemQueryData.CommentContent, WorkItemQueryData.UtcNow);
        workItem.LogTime(admin, 1.5m, null, WorkItemQueryData.UtcNow, WorkItemQueryData.UtcNow);

        _userContext.UserId.Returns(admin.Id);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<FlowState> statesMock = MockDbSetHelper.CreateMockDbSet(project.FlowStates);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<User>());
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.FlowStates.Returns(statesMock);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);
        DbSet<Component> componentsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Component>());
        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Milestone>());
        _dbContext.Components.Returns(componentsMock);
        _dbContext.Milestones.Returns(milestonesMock);

        // Act
        Result<IReadOnlyCollection<BoardColumnResponse>> result =
            await _handler.Handle(new GetProjectBoardQuery(project.Id), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        BoardWorkItemResponse summary = result.Value.Single(s => s.FlowStateId == todoState.Id).WorkItems.Single();
        summary.WorkItemId.Should().Be(workItem.Id);
        summary.Title.Should().Be("Test Work Item");
        summary.Type.Should().Be(WorkItemType.Story);
        summary.Priority.Should().Be(Priority.Medium);
        summary.FlowStateId.Should().Be(todoState.Id);
        summary.FlowStateName.Should().Be("Backlog");
        summary.AssigneeId.Should().BeNull();
        summary.CreatedOnUtc.Should().Be(WorkItemQueryData.UtcNow);
        summary.CommentCount.Should().Be(1);
        summary.TimeEntryCount.Should().Be(1);
    }
}
