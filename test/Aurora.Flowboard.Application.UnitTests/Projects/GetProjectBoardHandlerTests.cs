using Aurora.Flowboard.Application.Projects.Shared;
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
        _dbContext.Seed(c => c.Projects);

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
        _dbContext.Seed(c => c.Projects, project);

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
        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.FlowStates, project.FlowStates);
        _dbContext.Seed(c => c.WorkItems);
        _dbContext.Seed(c => c.Users);
        _dbContext.Seed(c => c.Components);
        _dbContext.Seed(c => c.Milestones);
        _dbContext.Seed(c => c.FlowTransitions, project.FlowTransitions);

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
        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.FlowStates, project.FlowStates);
        _dbContext.Seed(c => c.WorkItems, workItem);
        _dbContext.Seed(c => c.Users);
        _dbContext.Seed(c => c.Components);
        _dbContext.Seed(c => c.Milestones);
        _dbContext.Seed(c => c.FlowTransitions, project.FlowTransitions);

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
        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.FlowStates, project.FlowStates);
        _dbContext.Seed(c => c.WorkItems, workItem);
        _dbContext.Seed(c => c.Users);
        _dbContext.Seed(c => c.Components);
        _dbContext.Seed(c => c.Milestones);
        _dbContext.Seed(c => c.FlowTransitions, project.FlowTransitions);

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
        FlowStateDefinition[] flowStates = FlowStateDefinitionData.GetFlow(
            ("Backlog", FlowStateCategory.Active),
            ("In Progress", FlowStateCategory.Active),
            ("QA", FlowStateCategory.Active),
            ("Done", FlowStateCategory.Completed),
            ("Cancelled", FlowStateCategory.Cancelled));
        Project project = Project.Create("Board Project", "Desc", ProjectCode.Create("BRD").Value, ProjectKind.Product, Color.Create("white").Value, flowStates, admin, WorkItemQueryData.UtcNow).Value;

        _userContext.UserId.Returns(admin.Id);
        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.FlowStates, project.FlowStates);
        _dbContext.Seed(c => c.WorkItems);
        _dbContext.Seed(c => c.Users);
        _dbContext.Seed(c => c.Components);
        _dbContext.Seed(c => c.Milestones);
        _dbContext.Seed(c => c.FlowTransitions, project.FlowTransitions);

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
        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.FlowStates, project.FlowStates);
        _dbContext.Seed(c => c.WorkItems, wi1, wi2);
        _dbContext.Seed(c => c.Users);
        _dbContext.Seed(c => c.Components);
        _dbContext.Seed(c => c.Milestones);
        _dbContext.Seed(c => c.FlowTransitions, project.FlowTransitions);

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
        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.FlowStates, project.FlowStates);
        _dbContext.Seed(c => c.WorkItems, lowPriority, criticalLate, highPriority, criticalEarly);
        _dbContext.Seed(c => c.Users);
        _dbContext.Seed(c => c.Components);
        _dbContext.Seed(c => c.Milestones);
        _dbContext.Seed(c => c.FlowTransitions, project.FlowTransitions);

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
        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.FlowStates, project.FlowStates);
        _dbContext.Seed(c => c.WorkItems, workItem);
        _dbContext.Seed(c => c.Users, assignee);
        _dbContext.Seed(c => c.Components);
        _dbContext.Seed(c => c.Milestones);
        _dbContext.Seed(c => c.FlowTransitions, project.FlowTransitions);

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
    public async Task Should_ReturnNullAssigneeInitialsAndFullName_When_WorkItemIsUnassigned()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        Project project = WorkItemQueryData.GetActiveProjectWithFlow(admin);
        WorkItem workItem = WorkItem.Create("Item", null, WorkItemType.Story, Priority.Medium, project, admin, null, null, WorkItemQueryData.UtcNow).Value;

        _userContext.UserId.Returns(admin.Id);
        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.FlowStates, project.FlowStates);
        _dbContext.Seed(c => c.WorkItems, workItem);
        _dbContext.Seed(c => c.Users, admin);
        _dbContext.Seed(c => c.Components);
        _dbContext.Seed(c => c.Milestones);
        _dbContext.Seed(c => c.FlowTransitions, project.FlowTransitions);

        // Act
        Result<IReadOnlyCollection<BoardColumnResponse>> result =
            await _handler.Handle(new GetProjectBoardQuery(project.Id), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        BoardWorkItemResponse summary = result.Value.SelectMany(s => s.WorkItems).Single();
        summary.AssigneeId.Should().BeNull();
        summary.AssigneeInitials.Should().BeNull();
        summary.AssigneeFullName.Should().BeNull();
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
        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.FlowStates, project.FlowStates);
        _dbContext.Seed(c => c.WorkItems, workItem);
        _dbContext.Seed(c => c.Users);
        _dbContext.Seed(c => c.Components);
        _dbContext.Seed(c => c.Milestones);
        _dbContext.Seed(c => c.FlowTransitions, project.FlowTransitions);

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

    [Fact]
    public async Task Should_FilterAvailableTransitions_ByRequesterRole()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        User developer = WorkItemQueryData.GetDeveloperUser();
        Project project = WorkItemQueryData.GetActiveProjectWithFlow(admin);
        project.AddMember(developer, ProjectRole.Developer, admin, WorkItemQueryData.UtcNow);
        FlowState backlogState = project.FlowStates.Single(s => s.Name == "Backlog");
        FlowState doneState = project.FlowStates.Single(s => s.Name == "Done");
        FlowState cancelledState = project.FlowStates.Single(s => s.Name == "Cancelled");
        FlowTransition toCancelled = project.FlowTransitions.Single(t => t.FromStateId == backlogState.Id && t.ToStateId == cancelledState.Id);
        project.RemoveFlowTransitionRole(toCancelled.Id, ProjectRole.Developer, admin);

        ArrangeBoard(project, developer.Id, project.FlowTransitions);

        // Act
        Result<IReadOnlyCollection<BoardColumnResponse>> result =
            await _handler.Handle(new GetProjectBoardQuery(project.Id), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Single(c => c.FlowStateId == backlogState.Id).AvailableTransitions
            .Should().ContainSingle()
            .Which.ToStateId.Should().Be(doneState.Id);
    }

    [Fact]
    public async Task Should_ReturnEmptyAvailableTransitions_When_ColumnHasNoOutgoingTransitions()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        Project project = WorkItemQueryData.GetActiveProjectWithFlow(admin);

        ArrangeBoard(project, admin.Id, Array.Empty<FlowTransition>());

        // Act
        Result<IReadOnlyCollection<BoardColumnResponse>> result =
            await _handler.Handle(new GetProjectBoardQuery(project.Id), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().ContainSingle()
            .Which.AvailableTransitions.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task Should_IncludeTerminalDestinationsWithTheirNames_InAvailableTransitions()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        Project project = WorkItemQueryData.GetActiveProjectWithFlow(admin);
        FlowState doneState = project.FlowStates.Single(s => s.Category == FlowStateCategory.Completed);
        FlowState cancelledState = project.FlowStates.Single(s => s.Category == FlowStateCategory.Cancelled);

        ArrangeBoard(project, admin.Id, project.FlowTransitions);

        // Act
        Result<IReadOnlyCollection<BoardColumnResponse>> result =
            await _handler.Handle(new GetProjectBoardQuery(project.Id), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        WorkItemFlowTransitionResponse[] expected =
        [
            new(doneState.Id, "Done"),
            new(cancelledState.Id, "Cancelled")
        ];
        result.Value.Single().AvailableTransitions.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task Should_OrderAvailableTransitions_ByToStateName()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        Project project = WorkItemQueryData.GetActiveProjectWithFlow(admin);

        // Transitions are created Backlog → Done first, then Backlog → Cancelled, so insertion order is not alphabetical.
        ArrangeBoard(project, admin.Id, project.FlowTransitions);

        // Act
        Result<IReadOnlyCollection<BoardColumnResponse>> result =
            await _handler.Handle(new GetProjectBoardQuery(project.Id), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Single().AvailableTransitions.Select(t => t.ToStateName)
            .Should().Equal("Cancelled", "Done");
    }

    [Fact]
    public async Task Should_AttributeAvailableTransitions_ToTheirFromStateColumn()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        Project project = WorkItemQueryData.GetActiveProjectWithFlow(admin);
        // Adding a second Active state links Backlog <-> In Progress and reroutes Backlog -> Done to In Progress -> Done,
        // so each column ends up with a different set of destinations.
        project.AddFlowState("In Progress", FlowStateCategory.Active, Color.Create("white").Value, [ProjectRole.Admin, ProjectRole.Developer], admin);
        FlowState backlogState = project.FlowStates.Single(s => s.Name == "Backlog");
        FlowState inProgressState = project.FlowStates.Single(s => s.Name == "In Progress");

        ArrangeBoard(project, admin.Id, project.FlowTransitions);

        // Act
        Result<IReadOnlyCollection<BoardColumnResponse>> result =
            await _handler.Handle(new GetProjectBoardQuery(project.Id), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Single(c => c.FlowStateId == backlogState.Id).AvailableTransitions.Select(t => t.ToStateName)
            .Should().Equal("Cancelled", "In Progress");
        result.Value.Single(c => c.FlowStateId == inProgressState.Id).AvailableTransitions.Select(t => t.ToStateName)
            .Should().Equal("Backlog", "Done");
    }

    [Fact]
    public async Task Should_ReturnEmptyBoard_When_ProjectHasNoActiveStates()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        Project project = WorkItemQueryData.GetLegacyProjectWithoutFlow(admin);

        ArrangeBoard(project, admin.Id, project.FlowTransitions);

        // Act
        Result<IReadOnlyCollection<BoardColumnResponse>> result =
            await _handler.Handle(new GetProjectBoardQuery(project.Id), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeEmpty();
        _ = _dbContext.DidNotReceive().FlowTransitions;
        _ = _dbContext.DidNotReceive().WorkItems;
    }

    private void ArrangeBoard(Project project, Guid userId, IEnumerable<FlowTransition> transitions)
    {
        _userContext.UserId.Returns(userId);
        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.FlowStates, project.FlowStates);
        _dbContext.Seed(c => c.FlowTransitions, transitions);
        _dbContext.Seed(c => c.WorkItems);
        _dbContext.Seed(c => c.Users);
        _dbContext.Seed(c => c.Components);
        _dbContext.Seed(c => c.Milestones);
    }
}
