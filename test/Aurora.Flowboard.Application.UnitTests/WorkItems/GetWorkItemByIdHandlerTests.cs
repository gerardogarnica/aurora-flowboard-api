namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class GetWorkItemByIdHandlerTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly GetWorkItemByIdHandler _handler;

    public GetWorkItemByIdHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _handler = new GetWorkItemByIdHandler(_dbContext);
    }

    [Fact]
    public async Task Should_ReturnNotFoundError_When_WorkItemDoesNotExist()
    {
        // Arrange
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<WorkItem>());
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<User>());
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);

        // Act
        Result<WorkItemResponse> result =
            await _handler.Handle(new GetWorkItemByIdQuery(Guid.NewGuid()), CancellationToken.None);

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
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);

        // Act
        Result<WorkItemResponse> result =
            await _handler.Handle(new GetWorkItemByIdQuery(workItem.Id), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task Should_MapAllScalarFields_When_WorkItemExists()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        (Project _, WorkItem workItem) = WorkItemQueryData.GetProjectAndWorkItem(admin);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);

        // Act
        Result<WorkItemResponse> result =
            await _handler.Handle(new GetWorkItemByIdQuery(workItem.Id), CancellationToken.None);

        // Assert
        WorkItemResponse response = result.Value;
        response.WorkItemId.Should().Be(workItem.Id);
        response.Title.Should().Be("Test Work Item");
        response.Type.Should().Be(WorkItemType.Story);
        response.Priority.Should().Be(Priority.Medium);
        response.ProjectId.Should().Be(workItem.ProjectId);
        response.FlowStateId.Should().Be(workItem.FlowStateId);
        response.CreatedById.Should().Be(admin.Id);
        response.EstimatedPoints.Should().BeNull();
        response.EstimatedCompletionDate.Should().BeNull();
        response.CreatedOnUtc.Should().Be(WorkItemQueryData.UtcNow);
        response.UpdatedOnUtc.Should().BeNull();
        response.CompletedOnUtc.Should().BeNull();
    }

    [Fact]
    public async Task Should_MapProjectAndFlowStateNames_When_WorkItemExists()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        (Project project, WorkItem workItem) = WorkItemQueryData.GetProjectAndWorkItem(admin);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);

        // Act
        Result<WorkItemResponse> result =
            await _handler.Handle(new GetWorkItemByIdQuery(workItem.Id), CancellationToken.None);

        // Assert
        result.Value.ProjectName.Should().Be(project.Name);
        result.Value.FlowStateName.Should().Be("Todo");
    }

    [Fact]
    public async Task Should_ResolveCreatedByAndAssigneeFullNames_When_WorkItemIsAssigned()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        User assignee = WorkItemQueryData.GetAssigneeUser();
        (Project _, WorkItem workItem) = WorkItemQueryData.GetProjectAndWorkItemWithAssignee(admin, assignee);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin, assignee]);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);

        // Act
        Result<WorkItemResponse> result =
            await _handler.Handle(new GetWorkItemByIdQuery(workItem.Id), CancellationToken.None);

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
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);

        // Act
        Result<WorkItemResponse> result =
            await _handler.Handle(new GetWorkItemByIdQuery(workItem.Id), CancellationToken.None);

        // Assert
        result.Value.Comments.Should().HaveCount(1);
        result.Value.ChangeLogs.Should().HaveCount(2); // Created + CommentAdded
        result.Value.Tags.Should().BeEmpty();
        result.Value.TimeEntries.Should().BeEmpty();
        result.Value.StateHistory.Should().BeEmpty();
    }
}
