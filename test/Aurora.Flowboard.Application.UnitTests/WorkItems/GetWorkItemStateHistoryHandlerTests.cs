using Aurora.Flowboard.Application.Abstractions.Pagination;
using Aurora.Flowboard.Application.WorkItems.GetStateHistory;

namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class GetWorkItemStateHistoryHandlerTests
{
    private const int Page = PaginationDefaults.DefaultPage;
    private const int PageSize = PaginationDefaults.DefaultPageSize;

    private readonly IApplicationDbContext _dbContext;
    private readonly IUserContext _userContext;
    private readonly GetWorkItemStateHistoryHandler _handler;

    public GetWorkItemStateHistoryHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _userContext = Substitute.For<IUserContext>();
        _handler = new GetWorkItemStateHistoryHandler(_dbContext, _userContext);
    }

    [Fact]
    public async Task Should_ReturnNotFoundError_When_UserIsNotProjectMember()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        (Project _, WorkItem workItem) = WorkItemQueryData.GetProjectAndWorkItemWithStateHistory(admin);
        _userContext.UserId.Returns(Guid.NewGuid());
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<StateTransitionHistory> stateHistoryMock = MockDbSetHelper.CreateMockDbSet(workItem.StateHistory);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<FlowState> statesMock = MockDbSetHelper.CreateMockDbSet(workItem.Project.FlowStates);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.StateTransitionHistories.Returns(stateHistoryMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.FlowStates.Returns(statesMock);

        // Act
        Result<PagedResponse<WorkItemStateTransitionResponse>> result =
            await _handler.Handle(new GetWorkItemStateHistoryQuery(workItem.Id, Page, PageSize), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(WorkItemErrors.NotFound);
    }

    [Fact]
    public async Task Should_ResolveStateNamesAndChangedBy_When_WorkItemHasStateHistory()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        (Project _, WorkItem workItem) = WorkItemQueryData.GetProjectAndWorkItemWithStateHistory(admin);
        _userContext.UserId.Returns(admin.Id);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<StateTransitionHistory> stateHistoryMock = MockDbSetHelper.CreateMockDbSet(workItem.StateHistory);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<FlowState> statesMock = MockDbSetHelper.CreateMockDbSet(workItem.Project.FlowStates);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.StateTransitionHistories.Returns(stateHistoryMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.FlowStates.Returns(statesMock);

        // Act
        Result<PagedResponse<WorkItemStateTransitionResponse>> result =
            await _handler.Handle(new GetWorkItemStateHistoryQuery(workItem.Id, Page, PageSize), CancellationToken.None);

        // Assert
        result.Value.Items.Should().ContainSingle();
        WorkItemStateTransitionResponse transition = result.Value.Items.Single();
        transition.FromStateName.Should().Be("Backlog");
        transition.ToStateName.Should().Be("Done");
        transition.ChangedByFullName.Should().Be("Work Admin");
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_ReturnDifferentItemsPerPage_When_ResultsSpanSeveralPages()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        (Project _, WorkItem workItem) = WorkItemQueryData.GetProjectAndWorkItemWithThreeStateTransitions(admin);
        _userContext.UserId.Returns(admin.Id);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<StateTransitionHistory> stateHistoryMock = MockDbSetHelper.CreateMockDbSet(workItem.StateHistory);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<FlowState> statesMock = MockDbSetHelper.CreateMockDbSet(workItem.Project.FlowStates);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.StateTransitionHistories.Returns(stateHistoryMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.FlowStates.Returns(statesMock);

        // Act
        Result<PagedResponse<WorkItemStateTransitionResponse>> firstPage =
            await _handler.Handle(new GetWorkItemStateHistoryQuery(workItem.Id, 1, 2), CancellationToken.None);
        Result<PagedResponse<WorkItemStateTransitionResponse>> secondPage =
            await _handler.Handle(new GetWorkItemStateHistoryQuery(workItem.Id, 2, 2), CancellationToken.None);

        // Assert — newest first: Review→Done, In Progress→Review, Backlog→In Progress
        firstPage.Value.Items.Select(s => s.ToStateName).Should().Equal("Done", "Review");
        secondPage.Value.Items.Select(s => s.ToStateName).Should().Equal("In Progress");
        secondPage.Value.Items.Single().FromStateName.Should().Be("Backlog");
        firstPage.Value.TotalCount.Should().Be(3);
        firstPage.Value.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task Should_ReturnEmptyPage_When_WorkItemHasNoStateHistory()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        (Project _, WorkItem workItem) = WorkItemQueryData.GetProjectAndWorkItem(admin);
        _userContext.UserId.Returns(admin.Id);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<StateTransitionHistory> stateHistoryMock = MockDbSetHelper.CreateMockDbSet(workItem.StateHistory);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<FlowState> statesMock = MockDbSetHelper.CreateMockDbSet(workItem.Project.FlowStates);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.StateTransitionHistories.Returns(stateHistoryMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.FlowStates.Returns(statesMock);

        // Act
        Result<PagedResponse<WorkItemStateTransitionResponse>> result =
            await _handler.Handle(new GetWorkItemStateHistoryQuery(workItem.Id, Page, PageSize), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
        result.Value.TotalPages.Should().Be(0);
    }
}
