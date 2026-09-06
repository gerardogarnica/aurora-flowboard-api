using Aurora.Flowboard.Application.Abstractions.Pagination;
using Aurora.Flowboard.Application.WorkItems.GetChangeLogs;

namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class GetWorkItemChangeLogsHandlerTests
{
    private const int Page = PaginationDefaults.DefaultPage;
    private const int PageSize = PaginationDefaults.DefaultPageSize;

    private readonly IApplicationDbContext _dbContext;
    private readonly IUserContext _userContext;
    private readonly GetWorkItemChangeLogsHandler _handler;

    public GetWorkItemChangeLogsHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _userContext = Substitute.For<IUserContext>();
        _handler = new GetWorkItemChangeLogsHandler(_dbContext, _userContext);
    }

    [Fact]
    public async Task Should_ReturnNotFoundError_When_UserIsNotProjectMember()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        (Project _, WorkItem workItem) = WorkItemQueryData.GetProjectAndWorkItem(admin);
        _userContext.UserId.Returns(Guid.NewGuid());
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<WorkItemChangeLog> changeLogsMock = MockDbSetHelper.CreateMockDbSet(workItem.ChangeLogs);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.WorkItemChangeLogs.Returns(changeLogsMock);
        _dbContext.Users.Returns(usersMock);

        // Act
        Result<PagedResponse<WorkItemChangeLogResponse>> result =
            await _handler.Handle(new GetWorkItemChangeLogsQuery(workItem.Id, Page, PageSize), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(WorkItemErrors.NotFound);
    }

    [Fact]
    public async Task Should_ResolveChangedByFullName_When_WorkItemHasChangeLogs()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        (Project _, WorkItem workItem) = WorkItemQueryData.GetProjectAndWorkItem(admin);
        _userContext.UserId.Returns(admin.Id);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<WorkItemChangeLog> changeLogsMock = MockDbSetHelper.CreateMockDbSet(workItem.ChangeLogs);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<FlowState> statesMock = MockDbSetHelper.CreateMockDbSet(workItem.Project.FlowStates);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.WorkItemChangeLogs.Returns(changeLogsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.FlowStates.Returns(statesMock);

        // Act
        Result<PagedResponse<WorkItemChangeLogResponse>> result =
            await _handler.Handle(new GetWorkItemChangeLogsQuery(workItem.Id, Page, PageSize), CancellationToken.None);

        // Assert
        result.Value.Items.Should().ContainSingle();
        result.Value.Items.Single().ChangeType.Should().Be(WorkItemChangeType.Created);
        result.Value.Items.Single().ChangedByFullName.Should().Be("Work Admin");
        result.Value.Items.Single().AffectedEntityName.Should().BeNull();
    }

    [Fact]
    public async Task Should_ResolveAffectedEntityName_When_ChangeLogTypeHasAnAffectedEntity()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        User assignee = WorkItemQueryData.GetAssigneeUser();
        (Project _, WorkItem workItem, Component component, Milestone milestone) =
            WorkItemQueryData.GetProjectAndWorkItemWithAllChangeLogTypes(admin, assignee);
        _userContext.UserId.Returns(admin.Id);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<WorkItemChangeLog> changeLogsMock = MockDbSetHelper.CreateMockDbSet(workItem.ChangeLogs);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin, assignee]);
        DbSet<FlowState> statesMock = MockDbSetHelper.CreateMockDbSet(workItem.Project.FlowStates);
        DbSet<Component> componentsMock = MockDbSetHelper.CreateMockDbSet([component]);
        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet([milestone]);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.WorkItemChangeLogs.Returns(changeLogsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.FlowStates.Returns(statesMock);
        _dbContext.Components.Returns(componentsMock);
        _dbContext.Milestones.Returns(milestonesMock);

        // Act
        Result<PagedResponse<WorkItemChangeLogResponse>> result =
            await _handler.Handle(new GetWorkItemChangeLogsQuery(workItem.Id, Page, PageSize), CancellationToken.None);

        // Assert
        IReadOnlyCollection<WorkItemChangeLogResponse> changeLogs = result.Value.Items;
        changeLogs.Single(c => c.ChangeType == WorkItemChangeType.Assigned).AffectedEntityName.Should().Be("Work Assignee");
        changeLogs.Single(c => c.ChangeType == WorkItemChangeType.Moved).AffectedEntityName.Should().Be("Done");
        changeLogs.Single(c => c.ChangeType == WorkItemChangeType.ComponentChanged).AffectedEntityName.Should().Be("Auth Module");
        changeLogs.Single(c => c.ChangeType == WorkItemChangeType.MilestoneChanged).AffectedEntityName.Should().Be("Sprint 1");
        changeLogs.Single(c => c.ChangeType == WorkItemChangeType.Created).AffectedEntityName.Should().BeNull();
    }

    [Fact]
    public async Task Should_OrderNewestFirstAndHonourPageSize_When_WorkItemHasSeveralChangeLogs()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        User assignee = WorkItemQueryData.GetAssigneeUser();
        (Project _, WorkItem workItem, Component component, Milestone milestone) =
            WorkItemQueryData.GetProjectAndWorkItemWithAllChangeLogTypes(admin, assignee);
        _userContext.UserId.Returns(admin.Id);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<WorkItemChangeLog> changeLogsMock = MockDbSetHelper.CreateMockDbSet(workItem.ChangeLogs);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin, assignee]);
        DbSet<FlowState> statesMock = MockDbSetHelper.CreateMockDbSet(workItem.Project.FlowStates);
        DbSet<Component> componentsMock = MockDbSetHelper.CreateMockDbSet([component]);
        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet([milestone]);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.WorkItemChangeLogs.Returns(changeLogsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.FlowStates.Returns(statesMock);
        _dbContext.Components.Returns(componentsMock);
        _dbContext.Milestones.Returns(milestonesMock);

        // Act
        Result<PagedResponse<WorkItemChangeLogResponse>> result =
            await _handler.Handle(new GetWorkItemChangeLogsQuery(workItem.Id, Page, 2), CancellationToken.None);

        // Assert — MilestoneChanged is the most recent, ComponentChanged the one before it
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items.Select(c => c.ChangeType).Should()
            .ContainInOrder(WorkItemChangeType.MilestoneChanged, WorkItemChangeType.ComponentChanged);
        result.Value.TotalCount.Should().Be(workItem.ChangeLogs.Count);
        result.Value.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task Should_ReturnDifferentItemsPerPage_When_ResultsSpanSeveralPages()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        User assignee = WorkItemQueryData.GetAssigneeUser();
        (Project _, WorkItem workItem, Component component, Milestone milestone) =
            WorkItemQueryData.GetProjectAndWorkItemWithAllChangeLogTypes(admin, assignee);
        _userContext.UserId.Returns(admin.Id);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<WorkItemChangeLog> changeLogsMock = MockDbSetHelper.CreateMockDbSet(workItem.ChangeLogs);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin, assignee]);
        DbSet<FlowState> statesMock = MockDbSetHelper.CreateMockDbSet(workItem.Project.FlowStates);
        DbSet<Component> componentsMock = MockDbSetHelper.CreateMockDbSet([component]);
        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet([milestone]);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.WorkItemChangeLogs.Returns(changeLogsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.FlowStates.Returns(statesMock);
        _dbContext.Components.Returns(componentsMock);
        _dbContext.Milestones.Returns(milestonesMock);

        // Act
        Result<PagedResponse<WorkItemChangeLogResponse>> firstPage =
            await _handler.Handle(new GetWorkItemChangeLogsQuery(workItem.Id, 1, 2), CancellationToken.None);
        Result<PagedResponse<WorkItemChangeLogResponse>> secondPage =
            await _handler.Handle(new GetWorkItemChangeLogsQuery(workItem.Id, 2, 2), CancellationToken.None);
        Result<PagedResponse<WorkItemChangeLogResponse>> thirdPage =
            await _handler.Handle(new GetWorkItemChangeLogsQuery(workItem.Id, 3, 2), CancellationToken.None);

        // Assert — newest first: MilestoneChanged, ComponentChanged, Moved, Assigned, Created
        firstPage.Value.Items.Select(c => c.ChangeType).Should()
            .Equal(WorkItemChangeType.MilestoneChanged, WorkItemChangeType.ComponentChanged);
        secondPage.Value.Items.Select(c => c.ChangeType).Should()
            .Equal(WorkItemChangeType.Moved, WorkItemChangeType.Assigned);
        thirdPage.Value.Items.Select(c => c.ChangeType).Should()
            .Equal(WorkItemChangeType.Created);
        firstPage.Value.TotalCount.Should().Be(5);
        firstPage.Value.TotalPages.Should().Be(3);
    }
}
