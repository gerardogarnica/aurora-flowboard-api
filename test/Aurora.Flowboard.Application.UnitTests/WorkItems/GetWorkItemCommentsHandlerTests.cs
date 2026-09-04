using Aurora.Flowboard.Application.Abstractions.Pagination;
using Aurora.Flowboard.Application.WorkItems.GetComments;

namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class GetWorkItemCommentsHandlerTests
{
    private const int Page = PaginationDefaults.DefaultPage;
    private const int PageSize = PaginationDefaults.DefaultPageSize;

    private readonly IApplicationDbContext _dbContext;
    private readonly IUserContext _userContext;
    private readonly GetWorkItemCommentsHandler _handler;

    public GetWorkItemCommentsHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _userContext = Substitute.For<IUserContext>();
        _handler = new GetWorkItemCommentsHandler(_dbContext, _userContext);
    }

    [Fact]
    public async Task Should_ReturnNotFoundError_When_UserIsNotProjectMember()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        (Project _, WorkItem workItem) = WorkItemQueryData.GetProjectAndWorkItemWithComment(admin);
        _userContext.UserId.Returns(Guid.NewGuid());
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<Comment> commentsMock = MockDbSetHelper.CreateMockDbSet(workItem.Comments);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Comments.Returns(commentsMock);
        _dbContext.Users.Returns(usersMock);

        // Act
        Result<PagedResponse<WorkItemCommentResponse>> result =
            await _handler.Handle(new GetWorkItemCommentsQuery(workItem.Id, Page, PageSize), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(WorkItemErrors.NotFound);
    }

    [Fact]
    public async Task Should_ReturnNotFoundError_When_WorkItemDoesNotExist()
    {
        // Arrange
        _userContext.UserId.Returns(Guid.NewGuid());
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<WorkItem>());
        DbSet<Comment> commentsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Comment>());
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<User>());
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Comments.Returns(commentsMock);
        _dbContext.Users.Returns(usersMock);

        // Act
        Result<PagedResponse<WorkItemCommentResponse>> result =
            await _handler.Handle(new GetWorkItemCommentsQuery(Guid.NewGuid(), Page, PageSize), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(WorkItemErrors.NotFound);
    }

    [Fact]
    public async Task Should_ResolveAuthorFullName_When_WorkItemHasComment()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        (Project _, WorkItem workItem) = WorkItemQueryData.GetProjectAndWorkItemWithComment(admin);
        _userContext.UserId.Returns(admin.Id);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<Comment> commentsMock = MockDbSetHelper.CreateMockDbSet(workItem.Comments);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Comments.Returns(commentsMock);
        _dbContext.Users.Returns(usersMock);

        // Act
        Result<PagedResponse<WorkItemCommentResponse>> result =
            await _handler.Handle(new GetWorkItemCommentsQuery(workItem.Id, Page, PageSize), CancellationToken.None);

        // Assert
        result.Value.Items.Should().ContainSingle();
        result.Value.Items.Single().AuthorFullName.Should().Be("Work Admin");
        result.Value.Items.Single().Content.Should().Be(WorkItemQueryData.CommentContent);
        result.Value.TotalCount.Should().Be(1);
        result.Value.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task Should_ReturnEmptyPage_When_PageIsBeyondTheLastOne()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        (Project _, WorkItem workItem) = WorkItemQueryData.GetProjectAndWorkItemWithComment(admin);
        _userContext.UserId.Returns(admin.Id);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<Comment> commentsMock = MockDbSetHelper.CreateMockDbSet(workItem.Comments);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Comments.Returns(commentsMock);
        _dbContext.Users.Returns(usersMock);

        // Act
        Result<PagedResponse<WorkItemCommentResponse>> result =
            await _handler.Handle(new GetWorkItemCommentsQuery(workItem.Id, 5, PageSize), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_ExcludeDeletedComments_When_CountingAndListing()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        (Project _, WorkItem workItem) = WorkItemQueryData.GetProjectAndWorkItemWithComment(admin);
        Guid commentId = workItem.Comments.Single().Id;
        workItem.RemoveComment(commentId, admin, WorkItemQueryData.UtcNow);
        _userContext.UserId.Returns(admin.Id);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<Comment> commentsMock = MockDbSetHelper.CreateMockDbSet(workItem.Comments);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Comments.Returns(commentsMock);
        _dbContext.Users.Returns(usersMock);

        // Act
        Result<PagedResponse<WorkItemCommentResponse>> result =
            await _handler.Handle(new GetWorkItemCommentsQuery(workItem.Id, Page, PageSize), CancellationToken.None);

        // Assert
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
        result.Value.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task Should_ReturnDifferentItemsPerPage_When_ResultsSpanSeveralPages()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        (Project _, WorkItem workItem) = WorkItemQueryData.GetProjectAndWorkItemWithThreeComments(admin);
        _userContext.UserId.Returns(admin.Id);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<Comment> commentsMock = MockDbSetHelper.CreateMockDbSet(workItem.Comments);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Comments.Returns(commentsMock);
        _dbContext.Users.Returns(usersMock);

        // Act
        Result<PagedResponse<WorkItemCommentResponse>> firstPage =
            await _handler.Handle(new GetWorkItemCommentsQuery(workItem.Id, 1, 2), CancellationToken.None);
        Result<PagedResponse<WorkItemCommentResponse>> secondPage =
            await _handler.Handle(new GetWorkItemCommentsQuery(workItem.Id, 2, 2), CancellationToken.None);

        // Assert
        firstPage.Value.Items.Select(c => c.Content).Should().Equal("newest comment", "middle comment");
        secondPage.Value.Items.Select(c => c.Content).Should().Equal("oldest comment");
        firstPage.Value.TotalCount.Should().Be(3);
        firstPage.Value.TotalPages.Should().Be(2);
        secondPage.Value.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task Should_OrderNewestFirst_When_WorkItemHasSeveralComments()
    {
        // Arrange
        User admin = WorkItemQueryData.GetAdminUser();
        (Project _, WorkItem workItem) = WorkItemQueryData.GetProjectAndWorkItem(admin);
        workItem.AddComment(admin, "oldest", WorkItemQueryData.UtcNow);
        workItem.AddComment(admin, "newest", WorkItemQueryData.UtcNow.AddHours(1));
        _userContext.UserId.Returns(admin.Id);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<Comment> commentsMock = MockDbSetHelper.CreateMockDbSet(workItem.Comments);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Comments.Returns(commentsMock);
        _dbContext.Users.Returns(usersMock);

        // Act
        Result<PagedResponse<WorkItemCommentResponse>> result =
            await _handler.Handle(new GetWorkItemCommentsQuery(workItem.Id, Page, PageSize), CancellationToken.None);

        // Assert
        result.Value.Items.Select(c => c.Content).Should().ContainInOrder("newest", "oldest");
    }
}
