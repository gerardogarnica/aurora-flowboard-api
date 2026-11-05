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

        _dbContext.Seed(c => c.WorkItems, workItem);
        _dbContext.Seed(c => c.FlowStates, toState);
        _dbContext.Seed(c => c.Users, admin);

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

        _dbContext.Seed(c => c.WorkItems, workItem);
        _dbContext.Seed(c => c.FlowStates, toState);
        _dbContext.Seed(c => c.Users, admin);

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
        _dbContext.Seed(c => c.WorkItems);

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
        _dbContext.Seed(c => c.WorkItems);

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
        _userContext.UserId.Returns(admin.Id);

        _dbContext.Seed(c => c.WorkItems, workItem);
        _dbContext.Seed(c => c.FlowStates);
        _dbContext.Seed(c => c.Users, admin);

        MoveWorkItemCommand command = new(workItem.Id, Guid.NewGuid(), null);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.FlowStateNotFound);
    }

    [Fact]
    public async Task Should_NotPersist_When_ToStateDoesNotExist()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        (WorkItem workItem, _) = WorkItemCommandData.GetWorkItemForMove(admin);
        _userContext.UserId.Returns(admin.Id);

        _dbContext.Seed(c => c.WorkItems, workItem);
        _dbContext.Seed(c => c.FlowStates);
        _dbContext.Seed(c => c.Users, admin);

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

        _dbContext.Seed(c => c.WorkItems, workItem);
        _dbContext.Seed(c => c.FlowStates, toState);
        _dbContext.Seed(c => c.Users);

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

        _dbContext.Seed(c => c.WorkItems, workItem);
        _dbContext.Seed(c => c.FlowStates, toState);
        _dbContext.Seed(c => c.Users);

        MoveWorkItemCommand command = new(workItem.Id, toState.Id, null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnUserNotFoundError_When_UserAndToStateDoNotExist()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        (WorkItem workItem, _) = WorkItemCommandData.GetWorkItemForMove(admin);
        _userContext.UserId.Returns(Guid.NewGuid());

        _dbContext.Seed(c => c.WorkItems, workItem);
        _dbContext.Seed(c => c.FlowStates);
        _dbContext.Seed(c => c.Users);

        MoveWorkItemCommand command = new(workItem.Id, Guid.NewGuid(), null);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(UserErrors.NotFound);
    }

    [Fact]
    public async Task Should_ReturnDomainError_When_TransitionNotAllowed()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        (WorkItem workItem, _) = WorkItemCommandData.GetWorkItemForMove(admin);
        // "Done" state has no valid transition from the initial state
        FlowState doneState = workItem.Project.FlowStates.First(s => s.Name == "Done");
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(WorkItemCommandData.UtcNow);

        _dbContext.Seed(c => c.WorkItems, workItem);
        _dbContext.Seed(c => c.FlowStates, doneState);
        _dbContext.Seed(c => c.Users, admin);

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
        FlowState doneState = workItem.Project.FlowStates.First(s => s.Name == "Done");
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(WorkItemCommandData.UtcNow);

        _dbContext.Seed(c => c.WorkItems, workItem);
        _dbContext.Seed(c => c.FlowStates, doneState);
        _dbContext.Seed(c => c.Users, admin);

        MoveWorkItemCommand command = new(workItem.Id, doneState.Id, null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
