namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class GetWorkItemsByProjectHandlerTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly GetWorkItemsByProjectHandler _handler;

    public GetWorkItemsByProjectHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _handler = new GetWorkItemsByProjectHandler(_dbContext);
    }

    [Fact]
    public async Task Should_ReturnProjectNotFoundError_When_ProjectDoesNotExist()
    {
        // Arrange
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Project>());
        _dbContext.Projects.Returns(projectsMock);

        // Act
        Result<IReadOnlyCollection<WorkItemSummaryResponse>> result =
            await _handler.Handle(new GetWorkItemsByProjectQuery(Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.NotFound);
    }

    [Fact]
    public async Task Should_ReturnEmptyList_When_ProjectHasNoWorkItems()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        (Project project, WorkItem _) = WorkItemQueryData.GetProjectAndWorkItem(admin);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<WorkItem>());
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.WorkItems.Returns(workItemsMock);

        // Act
        Result<IReadOnlyCollection<WorkItemSummaryResponse>> result =
            await _handler.Handle(new GetWorkItemsByProjectQuery(project.Id), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_ReturnOnlyWorkItemsForProject_When_MultipleProjectsExist()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        (Project project1, WorkItem wi1, WorkItem wi2) = WorkItemQueryData.GetProjectWithTwoWorkItems(admin);
        (Project _, WorkItem wi3) = WorkItemQueryData.GetProjectAndWorkItem(admin); // different project
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project1]);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([wi1, wi2, wi3]);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.WorkItems.Returns(workItemsMock);

        // Act
        Result<IReadOnlyCollection<WorkItemSummaryResponse>> result =
            await _handler.Handle(new GetWorkItemsByProjectQuery(project1.Id), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().AllSatisfy(w => w.WorkItemId.Should().NotBe(wi3.Id));
    }

    [Fact]
    public async Task Should_ReturnWorkItemsOrderedByCreatedOnUtc()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        (Project project, WorkItem wi1, WorkItem wi2) = WorkItemQueryData.GetProjectWithTwoWorkItems(admin);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([wi2, wi1]); // reversed order
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.WorkItems.Returns(workItemsMock);

        // Act
        Result<IReadOnlyCollection<WorkItemSummaryResponse>> result =
            await _handler.Handle(new GetWorkItemsByProjectQuery(project.Id), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Select(w => w.CreatedOnUtc).Should().BeInAscendingOrder();
        result.Value.First().Title.Should().Be("Alpha Item");
    }

    [Fact]
    public async Task Should_MapAllResponseFields_When_WorkItemExists()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        (Project project, WorkItem workItem) = WorkItemQueryData.GetProjectAndWorkItem(admin);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.WorkItems.Returns(workItemsMock);

        // Act
        Result<IReadOnlyCollection<WorkItemSummaryResponse>> result =
            await _handler.Handle(new GetWorkItemsByProjectQuery(project.Id), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        WorkItemSummaryResponse summary = result.Value.Single();
        summary.WorkItemId.Should().Be(workItem.Id);
        summary.Title.Should().Be("Test Work Item");
        summary.Type.Should().Be(WorkItemType.Story);
        summary.Priority.Should().Be(Priority.Medium);
        summary.FlowStateId.Should().Be(workItem.FlowStateId);
        summary.FlowStateName.Should().Be("Todo");
        summary.AssigneeId.Should().BeNull();
        summary.CreatedOnUtc.Should().Be(WorkItemQueryData.UtcNow);
        summary.CommentCount.Should().Be(0);
        summary.TimeEntryCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_MapCommentAndTimeEntryCounts_When_WorkItemHasCollections()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        (Project project, WorkItem workItem) = WorkItemQueryData.GetProjectAndWorkItemWithComment(admin);
        workItem.LogTime(admin, 1.5m, null, WorkItemQueryData.UtcNow, WorkItemQueryData.UtcNow);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.WorkItems.Returns(workItemsMock);

        // Act
        Result<IReadOnlyCollection<WorkItemSummaryResponse>> result =
            await _handler.Handle(new GetWorkItemsByProjectQuery(project.Id), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        WorkItemSummaryResponse summary = result.Value.Single();
        summary.CommentCount.Should().Be(1);
        summary.TimeEntryCount.Should().Be(1);
    }
}
