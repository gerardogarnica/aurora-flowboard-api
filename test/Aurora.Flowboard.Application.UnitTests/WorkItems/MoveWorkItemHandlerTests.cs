namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class MoveWorkItemHandlerTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUserContext _userContext;
    private readonly MoveWorkItemHandler _handler;

    public MoveWorkItemHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _userContext = Substitute.For<IUserContext>();
        _handler = new MoveWorkItemHandler(_dbContext, _dateTimeProvider, _userContext);
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_CommandIsValid()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        (WorkItem workItem, FlowState toState) = WorkItemCommandData.GetWorkItemForMove(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(WorkItemCommandData.UtcNow);

        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<FlowState> flowStatesMock = MockDbSetHelper.CreateMockDbSet([toState]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.FlowStates.Returns(flowStatesMock);
        _dbContext.Users.Returns(usersMock);

        MoveWorkItemCommand command = new(workItem.Id, toState.Id, null);

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
        (WorkItem workItem, FlowState toState) = WorkItemCommandData.GetWorkItemForMove(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(WorkItemCommandData.UtcNow);

        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<FlowState> flowStatesMock = MockDbSetHelper.CreateMockDbSet([toState]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.FlowStates.Returns(flowStatesMock);
        _dbContext.Users.Returns(usersMock);

        MoveWorkItemCommand command = new(workItem.Id, toState.Id, null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnWorkItemNotFoundError_When_WorkItemDoesNotExist()
    {
        // Arrange
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<WorkItem>());
        _dbContext.WorkItems.Returns(workItemsMock);

        MoveWorkItemCommand command = new(Guid.NewGuid(), Guid.NewGuid(), null);

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
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<WorkItem>());
        _dbContext.WorkItems.Returns(workItemsMock);

        MoveWorkItemCommand command = new(Guid.NewGuid(), Guid.NewGuid(), null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnStateNotFoundError_When_ToStateDoesNotExist()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        (WorkItem workItem, _) = WorkItemCommandData.GetWorkItemForMove(admin);

        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<FlowState> flowStatesMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<FlowState>());
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.FlowStates.Returns(flowStatesMock);

        MoveWorkItemCommand command = new(workItem.Id, Guid.NewGuid(), null);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(FlowErrors.StateNotFound);
    }

    [Fact]
    public async Task Should_NotPersist_When_ToStateDoesNotExist()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        (WorkItem workItem, _) = WorkItemCommandData.GetWorkItemForMove(admin);

        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<FlowState> flowStatesMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<FlowState>());
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.FlowStates.Returns(flowStatesMock);

        MoveWorkItemCommand command = new(workItem.Id, Guid.NewGuid(), null);

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
        (WorkItem workItem, FlowState toState) = WorkItemCommandData.GetWorkItemForMove(admin);
        _userContext.UserId.Returns(Guid.NewGuid());

        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<FlowState> flowStatesMock = MockDbSetHelper.CreateMockDbSet([toState]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<User>());
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.FlowStates.Returns(flowStatesMock);
        _dbContext.Users.Returns(usersMock);

        MoveWorkItemCommand command = new(workItem.Id, toState.Id, null);

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
        (WorkItem workItem, FlowState toState) = WorkItemCommandData.GetWorkItemForMove(admin);
        _userContext.UserId.Returns(Guid.NewGuid());

        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<FlowState> flowStatesMock = MockDbSetHelper.CreateMockDbSet([toState]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<User>());
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.FlowStates.Returns(flowStatesMock);
        _dbContext.Users.Returns(usersMock);

        MoveWorkItemCommand command = new(workItem.Id, toState.Id, null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnDomainError_When_TransitionNotAllowed()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        (WorkItem workItem, _) = WorkItemCommandData.GetWorkItemForMove(admin);
        // "Done" state has no valid transition from the initial state
        FlowState doneState = workItem.FlowState.Flow.States.First(s => s.Name == "Done");
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(WorkItemCommandData.UtcNow);

        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<FlowState> flowStatesMock = MockDbSetHelper.CreateMockDbSet([doneState]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.WorkItems.Returns(workItemsMock);
        _dbContext.FlowStates.Returns(flowStatesMock);
        _dbContext.Users.Returns(usersMock);

        MoveWorkItemCommand command = new(workItem.Id, doneState.Id, null);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(WorkItemErrors.TransitionNotAllowed);
    }

    [Fact]
    public async Task Should_NotPersist_When_TransitionNotAllowed()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        (WorkItem workItem, _) = WorkItemCommandData.GetWorkItemForMove(admin);
        FlowState doneState = workItem.FlowState.Flow.States.First(s => s.Name == "Done");
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(WorkItemCommandData.UtcNow);

        DbSet<WorkItem> workItemsMock2 = MockDbSetHelper.CreateMockDbSet([workItem]);
        DbSet<FlowState> flowStatesMock2 = MockDbSetHelper.CreateMockDbSet([doneState]);
        DbSet<User> usersMock2 = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.WorkItems.Returns(workItemsMock2);
        _dbContext.FlowStates.Returns(flowStatesMock2);
        _dbContext.Users.Returns(usersMock2);

        MoveWorkItemCommand command = new(workItem.Id, doneState.Id, null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
