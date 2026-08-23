namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class UpdateWorkItemEstimatedPointsHandlerTests
{
    private const int NewPoints = 8;

    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUserContext _userContext;
    private readonly UpdateWorkItemEstimatedPointsHandler _handler;

    public UpdateWorkItemEstimatedPointsHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _userContext = Substitute.For<IUserContext>();
        _handler = new UpdateWorkItemEstimatedPointsHandler(_dbContext, _dateTimeProvider, _userContext);
    }

    [Fact]
    public async Task Should_UpdatePointsAndPersist_When_CommandIsValid()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        WorkItem workItem = WorkItemCommandData.GetWorkItem(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(WorkItemCommandData.UtcNow);

        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);

        UpdateWorkItemEstimatedPointsCommand command = new(workItem.Id, NewPoints);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        workItem.EstimatedPoints.Should().Be(NewPoints);
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnDomainError_When_PointsAreNotPositive()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        WorkItem workItem = WorkItemCommandData.GetWorkItem(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(WorkItemCommandData.UtcNow);

        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);

        UpdateWorkItemEstimatedPointsCommand command = new(workItem.Id, 0);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(WorkItemErrors.EstimatedPointsInvalid);
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnNotFoundError_When_UserIsNotProjectMember()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        WorkItem workItem = WorkItemCommandData.GetWorkItem(admin);
        User nonMember = WorkItemCommandData.GetNonMember();
        _userContext.UserId.Returns(nonMember.Id);
        _dateTimeProvider.UtcNow.Returns(WorkItemCommandData.UtcNow);

        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([nonMember]);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.Users.Returns(usersMock);

        UpdateWorkItemEstimatedPointsCommand command = new(workItem.Id, NewPoints);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(WorkItemErrors.NotFound);
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
