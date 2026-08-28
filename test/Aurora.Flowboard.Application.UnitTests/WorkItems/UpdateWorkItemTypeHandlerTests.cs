namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class UpdateWorkItemTypeHandlerTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUserContext _userContext;
    private readonly UpdateWorkItemTypeHandler _handler;

    public UpdateWorkItemTypeHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _userContext = Substitute.For<IUserContext>();
        _handler = new UpdateWorkItemTypeHandler(_dbContext, _dateTimeProvider, _userContext);
    }

    [Fact]
    public async Task Should_UpdateTypeAndPersist_When_CommandIsValid()
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

        UpdateWorkItemTypeCommand command = new(workItem.Id, WorkItemType.Bug);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        workItem.Type.Should().Be(WorkItemType.Bug);
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnWorkItemNotFoundError_When_WorkItemDoesNotExist()
    {
        // Arrange
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<WorkItem>());
        _dbContext.WorkItems.Returns(workItemsMock);

        UpdateWorkItemTypeCommand command = new(Guid.NewGuid(), WorkItemType.Bug);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(WorkItemErrors.NotFound);
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

        UpdateWorkItemTypeCommand command = new(workItem.Id, WorkItemType.Bug);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(WorkItemErrors.NotFound);
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
