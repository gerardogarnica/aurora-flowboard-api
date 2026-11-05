namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class UpdateWorkItemCommentHandlerTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUserContext _userContext;
    private readonly UpdateWorkItemCommentHandler _handler;

    public UpdateWorkItemCommentHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _userContext = Substitute.For<IUserContext>();
        _handler = new UpdateWorkItemCommentHandler(_dbContext, _dateTimeProvider, _userContext);
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_CommandIsValid()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        WorkItem workItem = WorkItemCommandData.GetWorkItemWithComment(admin, out Guid commentId);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(WorkItemCommandData.UtcNow);

        _dbContext.Seed(c => c.WorkItems, workItem);
        _dbContext.Seed(c => c.Users, admin);

        UpdateWorkItemCommentCommand command = new(workItem.Id, commentId, WorkItemCommandData.UpdatedContent);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task Should_PersistChanges_When_CommandIsValid()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        WorkItem workItem = WorkItemCommandData.GetWorkItemWithComment(admin, out Guid commentId);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(WorkItemCommandData.UtcNow);

        _dbContext.Seed(c => c.WorkItems, workItem);
        _dbContext.Seed(c => c.Users, admin);

        UpdateWorkItemCommentCommand command = new(workItem.Id, commentId, WorkItemCommandData.UpdatedContent);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnWorkItemNotFoundError_When_WorkItemDoesNotExist()
    {
        // Arrange
        _dbContext.Seed(c => c.WorkItems);

        UpdateWorkItemCommentCommand command = new(Guid.NewGuid(), Guid.NewGuid(), WorkItemCommandData.UpdatedContent);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(WorkItemErrors.NotFound);
    }

    [Fact]
    public async Task Should_NotPersist_When_WorkItemDoesNotExist()
    {
        // Arrange
        _dbContext.Seed(c => c.WorkItems);

        UpdateWorkItemCommentCommand command = new(Guid.NewGuid(), Guid.NewGuid(), WorkItemCommandData.UpdatedContent);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnUserNotFoundError_When_UserDoesNotExist()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        WorkItem workItem = WorkItemCommandData.GetWorkItemWithComment(admin, out Guid commentId);
        _userContext.UserId.Returns(Guid.NewGuid());

        _dbContext.Seed(c => c.WorkItems, workItem);
        _dbContext.Seed(c => c.Users);

        UpdateWorkItemCommentCommand command = new(workItem.Id, commentId, WorkItemCommandData.UpdatedContent);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(UserErrors.NotFound);
    }

    [Fact]
    public async Task Should_NotPersist_When_UserDoesNotExist()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        WorkItem workItem = WorkItemCommandData.GetWorkItemWithComment(admin, out Guid commentId);
        _userContext.UserId.Returns(Guid.NewGuid());

        _dbContext.Seed(c => c.WorkItems, workItem);
        _dbContext.Seed(c => c.Users);

        UpdateWorkItemCommentCommand command = new(workItem.Id, commentId, WorkItemCommandData.UpdatedContent);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnDomainError_When_CommentNotFound()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        WorkItem workItem = WorkItemCommandData.GetWorkItem(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(WorkItemCommandData.UtcNow);

        _dbContext.Seed(c => c.WorkItems, workItem);
        _dbContext.Seed(c => c.Users, admin);

        UpdateWorkItemCommentCommand command = new(workItem.Id, Guid.NewGuid(), WorkItemCommandData.UpdatedContent);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(WorkItemErrors.CommentNotFound);
    }

    [Fact]
    public async Task Should_NotPersist_When_CommentNotFound()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        WorkItem workItem = WorkItemCommandData.GetWorkItem(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(WorkItemCommandData.UtcNow);

        _dbContext.Seed(c => c.WorkItems, workItem);
        _dbContext.Seed(c => c.Users, admin);

        UpdateWorkItemCommentCommand command = new(workItem.Id, Guid.NewGuid(), WorkItemCommandData.UpdatedContent);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
