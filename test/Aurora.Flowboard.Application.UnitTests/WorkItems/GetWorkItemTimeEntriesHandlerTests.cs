using Aurora.Flowboard.Application.Abstractions.Pagination;
using Aurora.Flowboard.Application.WorkItems.GetTimeEntries;

namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class GetWorkItemTimeEntriesHandlerTests
{
    private const int Page = PaginationDefaults.DefaultPage;
    private const int PageSize = PaginationDefaults.DefaultPageSize;

    private readonly IApplicationDbContext _dbContext;
    private readonly IUserContext _userContext;
    private readonly GetWorkItemTimeEntriesHandler _handler;

    public GetWorkItemTimeEntriesHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _userContext = Substitute.For<IUserContext>();
        _handler = new GetWorkItemTimeEntriesHandler(_dbContext, _userContext);
    }

    [Fact]
    public async Task Should_ReturnNotFoundError_When_UserIsNotProjectMember()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        (Project _, WorkItem workItem) = WorkItemQueryData.GetProjectAndWorkItemWithTimeEntry(admin);
        _userContext.UserId.Returns(Guid.NewGuid());
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<TimeEntry> timeEntriesMock = MockDbSetHelper.CreateMockDbSet(workItem.TimeEntries);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.TimeEntries.Returns(timeEntriesMock);
        _dbContext.Users.Returns(usersMock);

        // Act
        Result<PagedResponse<WorkItemTimeEntryResponse>> result =
            await _handler.Handle(new GetWorkItemTimeEntriesQuery(workItem.Id, Page, PageSize), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(WorkItemErrors.NotFound);
    }

    [Fact]
    public async Task Should_ResolveLoggedByFullName_When_WorkItemHasTimeEntry()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        (Project _, WorkItem workItem) = WorkItemQueryData.GetProjectAndWorkItemWithTimeEntry(admin);
        _userContext.UserId.Returns(admin.Id);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<TimeEntry> timeEntriesMock = MockDbSetHelper.CreateMockDbSet(workItem.TimeEntries);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.TimeEntries.Returns(timeEntriesMock);
        _dbContext.Users.Returns(usersMock);

        // Act
        Result<PagedResponse<WorkItemTimeEntryResponse>> result =
            await _handler.Handle(new GetWorkItemTimeEntriesQuery(workItem.Id, Page, PageSize), CancellationToken.None);

        // Assert
        result.Value.Items.Should().ContainSingle();
        WorkItemTimeEntryResponse entry = result.Value.Items.Single();
        entry.LoggedByFullName.Should().Be("Work Admin");
        entry.Hours.Should().Be(2.5m);
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_ReturnDifferentItemsPerPage_When_ResultsSpanSeveralPages()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        (Project _, WorkItem workItem) = WorkItemQueryData.GetProjectAndWorkItemWithThreeTimeEntries(admin);
        _userContext.UserId.Returns(admin.Id);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<TimeEntry> timeEntriesMock = MockDbSetHelper.CreateMockDbSet(workItem.TimeEntries);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.TimeEntries.Returns(timeEntriesMock);
        _dbContext.Users.Returns(usersMock);

        // Act
        Result<PagedResponse<WorkItemTimeEntryResponse>> firstPage =
            await _handler.Handle(new GetWorkItemTimeEntriesQuery(workItem.Id, 1, 2), CancellationToken.None);
        Result<PagedResponse<WorkItemTimeEntryResponse>> secondPage =
            await _handler.Handle(new GetWorkItemTimeEntriesQuery(workItem.Id, 2, 2), CancellationToken.None);

        // Assert
        firstPage.Value.Items.Select(t => t.Description).Should().Equal("newest entry", "middle entry");
        secondPage.Value.Items.Select(t => t.Description).Should().Equal("oldest entry");
        firstPage.Value.TotalCount.Should().Be(3);
        firstPage.Value.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task Should_ReturnEmptyPage_When_WorkItemHasNoTimeEntries()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        (Project _, WorkItem workItem) = WorkItemQueryData.GetProjectAndWorkItem(admin);
        _userContext.UserId.Returns(admin.Id);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<TimeEntry> timeEntriesMock = MockDbSetHelper.CreateMockDbSet(workItem.TimeEntries);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.TimeEntries.Returns(timeEntriesMock);
        _dbContext.Users.Returns(usersMock);

        // Act
        Result<PagedResponse<WorkItemTimeEntryResponse>> result =
            await _handler.Handle(new GetWorkItemTimeEntriesQuery(workItem.Id, Page, PageSize), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }
}
