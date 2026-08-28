namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class GetWorkItemByCodeHandlerTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IUserContext _userContext;
    private readonly GetWorkItemByCodeHandler _handler;

    public GetWorkItemByCodeHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _userContext = Substitute.For<IUserContext>();
        _handler = new GetWorkItemByCodeHandler(_dbContext, _userContext);
    }

    [Fact]
    public async Task Should_ReturnNotFoundError_When_WorkItemDoesNotExist()
    {
        // Arrange
        _userContext.UserId.Returns(Guid.NewGuid());
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<WorkItem>());
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<User>());
        DbSet<FlowTransition> transitionsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<FlowTransition>());
        DbSet<FlowState> statesMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<FlowState>());
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.FlowTransitions.Returns(transitionsMock);
        _dbContext.FlowStates.Returns(statesMock);

        // Act
        Result<WorkItemResponse> result =
            await _handler.Handle(new GetWorkItemByCodeQuery("WIP-1"), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(WorkItemErrors.NotFound);
    }

    [Fact]
    public async Task Should_ReturnNotFoundError_When_UserIsNotMember()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        (Project _, WorkItem workItem) = WorkItemQueryData.GetProjectAndWorkItem(admin);
        _userContext.UserId.Returns(Guid.NewGuid());
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<FlowTransition> transitionsMock = MockDbSetHelper.CreateMockDbSet(workItem.Project.FlowTransitions);
        DbSet<FlowState> statesMock = MockDbSetHelper.CreateMockDbSet(workItem.Project.FlowStates);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.FlowTransitions.Returns(transitionsMock);
        _dbContext.FlowStates.Returns(statesMock);

        // Act
        Result<WorkItemResponse> result =
            await _handler.Handle(new GetWorkItemByCodeQuery(workItem.Code), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(WorkItemErrors.NotFound);
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_WorkItemExists()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        (Project _, WorkItem workItem) = WorkItemQueryData.GetProjectAndWorkItem(admin);
        _userContext.UserId.Returns(admin.Id);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<FlowTransition> transitionsMock = MockDbSetHelper.CreateMockDbSet(workItem.Project.FlowTransitions);
        DbSet<FlowState> statesMock = MockDbSetHelper.CreateMockDbSet(workItem.Project.FlowStates);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.FlowTransitions.Returns(transitionsMock);
        _dbContext.FlowStates.Returns(statesMock);

        // Act
        Result<WorkItemResponse> result =
            await _handler.Handle(new GetWorkItemByCodeQuery(workItem.Code), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task Should_MapAllScalarFields_When_WorkItemExists()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        (Project _, WorkItem workItem) = WorkItemQueryData.GetProjectAndWorkItem(admin);
        _userContext.UserId.Returns(admin.Id);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<FlowTransition> transitionsMock = MockDbSetHelper.CreateMockDbSet(workItem.Project.FlowTransitions);
        DbSet<FlowState> statesMock = MockDbSetHelper.CreateMockDbSet(workItem.Project.FlowStates);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.FlowTransitions.Returns(transitionsMock);
        _dbContext.FlowStates.Returns(statesMock);

        // Act
        Result<WorkItemResponse> result =
            await _handler.Handle(new GetWorkItemByCodeQuery(workItem.Code), CancellationToken.None);

        // Assert
        WorkItemResponse response = result.Value;
        response.WorkItemId.Should().Be(workItem.Id);
        response.Code.Should().Be(workItem.Code);
        response.Title.Should().Be("Test Work Item");
        response.Type.Should().Be(WorkItemType.Story);
        response.Priority.Should().Be(Priority.Medium);
        response.ProjectId.Should().Be(workItem.ProjectId);
        response.FlowStateId.Should().Be(workItem.FlowStateId);
        response.CreatedById.Should().Be(admin.Id);
        response.ComponentId.Should().BeNull();
        response.ComponentName.Should().BeNull();
        response.MilestoneId.Should().BeNull();
        response.MilestoneName.Should().BeNull();
        response.EstimatedPoints.Should().BeNull();
        response.EstimatedCompletionDate.Should().BeNull();
        response.CreatedOnUtc.Should().Be(WorkItemQueryData.UtcNow);
        response.UpdatedOnUtc.Should().BeNull();
        response.CompletedOnUtc.Should().BeNull();
    }

    [Fact]
    public async Task Should_MapComponentAndMilestone_When_WorkItemHasThem()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        (Project _, WorkItem workItem) = WorkItemQueryData.GetProjectAndWorkItemWithComponentAndMilestone(admin);
        _userContext.UserId.Returns(admin.Id);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<FlowTransition> transitionsMock = MockDbSetHelper.CreateMockDbSet(workItem.Project.FlowTransitions);
        DbSet<FlowState> statesMock = MockDbSetHelper.CreateMockDbSet(workItem.Project.FlowStates);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.FlowTransitions.Returns(transitionsMock);
        _dbContext.FlowStates.Returns(statesMock);

        // Act
        Result<WorkItemResponse> result =
            await _handler.Handle(new GetWorkItemByCodeQuery(workItem.Code), CancellationToken.None);

        // Assert
        result.Value.ComponentId.Should().Be(workItem.ComponentId);
        result.Value.ComponentName.Should().Be("Auth Module");
        result.Value.MilestoneId.Should().Be(workItem.MilestoneId);
        result.Value.MilestoneName.Should().Be("Sprint 1");
    }

    [Fact]
    public async Task Should_MapProjectAndFlowStateNames_When_WorkItemExists()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        (Project project, WorkItem workItem) = WorkItemQueryData.GetProjectAndWorkItem(admin);
        _userContext.UserId.Returns(admin.Id);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<FlowTransition> transitionsMock = MockDbSetHelper.CreateMockDbSet(workItem.Project.FlowTransitions);
        DbSet<FlowState> statesMock = MockDbSetHelper.CreateMockDbSet(workItem.Project.FlowStates);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.FlowTransitions.Returns(transitionsMock);
        _dbContext.FlowStates.Returns(statesMock);

        // Act
        Result<WorkItemResponse> result =
            await _handler.Handle(new GetWorkItemByCodeQuery(workItem.Code), CancellationToken.None);

        // Assert
        result.Value.ProjectName.Should().Be(project.Name);
        result.Value.FlowStateName.Should().Be("Backlog");
    }

    [Fact]
    public async Task Should_ResolveCreatedByAndAssigneeFullNames_When_WorkItemIsAssigned()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        User assignee = WorkItemQueryData.GetAssigneeUser();
        (Project _, WorkItem workItem) = WorkItemQueryData.GetProjectAndWorkItemWithAssignee(admin, assignee);
        _userContext.UserId.Returns(admin.Id);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin, assignee]);
        DbSet<FlowTransition> transitionsMock = MockDbSetHelper.CreateMockDbSet(workItem.Project.FlowTransitions);
        DbSet<FlowState> statesMock = MockDbSetHelper.CreateMockDbSet(workItem.Project.FlowStates);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.FlowTransitions.Returns(transitionsMock);
        _dbContext.FlowStates.Returns(statesMock);

        // Act
        Result<WorkItemResponse> result =
            await _handler.Handle(new GetWorkItemByCodeQuery(workItem.Code), CancellationToken.None);

        // Assert
        result.Value.CreatedByFullName.Should().Be("Work Admin");
        result.Value.AssigneeFullName.Should().Be("Work Assignee");
    }

    [Fact]
    public async Task Should_MapCollections_When_WorkItemHasData()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        (Project _, WorkItem workItem) = WorkItemQueryData.GetProjectAndWorkItemWithComment(admin);
        _userContext.UserId.Returns(admin.Id);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<FlowTransition> transitionsMock = MockDbSetHelper.CreateMockDbSet(workItem.Project.FlowTransitions);
        DbSet<FlowState> statesMock = MockDbSetHelper.CreateMockDbSet(workItem.Project.FlowStates);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.FlowTransitions.Returns(transitionsMock);
        _dbContext.FlowStates.Returns(statesMock);

        // Act
        Result<WorkItemResponse> result =
            await _handler.Handle(new GetWorkItemByCodeQuery(workItem.Code), CancellationToken.None);

        // Assert
        result.Value.Comments.Should().HaveCount(1);
        result.Value.ChangeLogs.Should().HaveCount(2); // Created + CommentAdded
        result.Value.Tags.Should().BeEmpty();
        result.Value.TimeEntries.Should().BeEmpty();
        result.Value.StateHistory.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_MapAvailableTransitions_When_WorkItemHasTransitions()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        (Project _, WorkItem workItem) = WorkItemQueryData.GetProjectAndWorkItem(admin);
        _userContext.UserId.Returns(admin.Id);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<FlowTransition> transitionsMock = MockDbSetHelper.CreateMockDbSet(workItem.Project.FlowTransitions);
        DbSet<FlowState> statesMock = MockDbSetHelper.CreateMockDbSet(workItem.Project.FlowStates);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.FlowTransitions.Returns(transitionsMock);
        _dbContext.FlowStates.Returns(statesMock);

        // Act
        Result<WorkItemResponse> result =
            await _handler.Handle(new GetWorkItemByCodeQuery(workItem.Code), CancellationToken.None);

        // Assert
        result.Value.AvailableTransitions.Should().HaveCount(2);
        result.Value.AvailableTransitions.Select(t => t.ToStateName).Should().BeEquivalentTo("Done", "Cancelled");
    }

    [Fact]
    public async Task Should_ExcludeTransitionsNotAllowedForUserRole_When_UserIsNotAdmin()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        User developer = WorkItemQueryData.GetDeveloperUser();
        (Project _, WorkItem workItem) = WorkItemQueryData.GetProjectAndWorkItemWithRoleRestrictedTransition(admin, developer);
        _userContext.UserId.Returns(developer.Id);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin, developer]);
        DbSet<FlowTransition> transitionsMock = MockDbSetHelper.CreateMockDbSet(workItem.Project.FlowTransitions);
        DbSet<FlowState> statesMock = MockDbSetHelper.CreateMockDbSet(workItem.Project.FlowStates);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.FlowTransitions.Returns(transitionsMock);
        _dbContext.FlowStates.Returns(statesMock);

        // Act
        Result<WorkItemResponse> result =
            await _handler.Handle(new GetWorkItemByCodeQuery(workItem.Code), CancellationToken.None);

        // Assert
        result.Value.AvailableTransitions.Should().ContainSingle();
        result.Value.AvailableTransitions.Single().ToStateName.Should().Be("Cancelled");
    }

    [Fact]
    public async Task Should_ReturnEmptyAvailableTransitions_When_WorkItemHasNoOutgoingTransitions()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        (Project _, WorkItem workItem) = WorkItemQueryData.GetProjectAndWorkItem(admin);
        Guid cancelledStateId = workItem.Project.FlowStates.Single(s => s.Name == "Cancelled").Id;
        WorkItemQueryData.SetWorkItemFlowState(workItem, cancelledStateId);
        _userContext.UserId.Returns(admin.Id);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<FlowTransition> transitionsMock = MockDbSetHelper.CreateMockDbSet(workItem.Project.FlowTransitions);
        DbSet<FlowState> statesMock = MockDbSetHelper.CreateMockDbSet(workItem.Project.FlowStates);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.FlowTransitions.Returns(transitionsMock);
        _dbContext.FlowStates.Returns(statesMock);

        // Act
        Result<WorkItemResponse> result =
            await _handler.Handle(new GetWorkItemByCodeQuery(workItem.Code), CancellationToken.None);

        // Assert
        result.Value.AvailableTransitions.Should().BeEmpty();
    }
}
