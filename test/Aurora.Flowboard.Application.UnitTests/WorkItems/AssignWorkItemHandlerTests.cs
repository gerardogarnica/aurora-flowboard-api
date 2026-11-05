namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class AssignWorkItemHandlerTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUserContext _userContext;
    private readonly AssignWorkItemHandler _handler;

    public AssignWorkItemHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _userContext = Substitute.For<IUserContext>();
        _handler = new AssignWorkItemHandler(_dbContext, _dateTimeProvider, _userContext);
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_CommandIsValid()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        (WorkItem workItem, User assignee) = WorkItemCommandData.GetWorkItemForAssign(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(WorkItemCommandData.UtcNow);

        _dbContext.Seed(c => c.WorkItems, workItem);
        _dbContext.Seed(c => c.Users, assignee, admin);

        AssignWorkItemCommand command = new(workItem.Id, assignee.Id);

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
        (WorkItem workItem, User assignee) = WorkItemCommandData.GetWorkItemForAssign(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(WorkItemCommandData.UtcNow);

        _dbContext.Seed(c => c.WorkItems, workItem);
        _dbContext.Seed(c => c.Users, assignee, admin);

        AssignWorkItemCommand command = new(workItem.Id, assignee.Id);

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

        AssignWorkItemCommand command = new(Guid.NewGuid(), Guid.NewGuid());

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

        AssignWorkItemCommand command = new(Guid.NewGuid(), Guid.NewGuid());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnAssigneeNotFoundError_When_AssigneeDoesNotExist()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        WorkItem workItem = WorkItemCommandData.GetWorkItem(admin);
        _userContext.UserId.Returns(admin.Id);

        _dbContext.Seed(c => c.WorkItems, workItem);
        _dbContext.Seed(c => c.Users, admin);

        AssignWorkItemCommand command = new(workItem.Id, Guid.NewGuid());

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(WorkItemErrors.AssigneeNotFound);
    }

    [Fact]
    public async Task Should_NotPersist_When_AssigneeDoesNotExist()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        WorkItem workItem = WorkItemCommandData.GetWorkItem(admin);
        _userContext.UserId.Returns(admin.Id);

        _dbContext.Seed(c => c.WorkItems, workItem);
        _dbContext.Seed(c => c.Users, admin);

        AssignWorkItemCommand command = new(workItem.Id, Guid.NewGuid());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnUserNotFoundError_When_CallerDoesNotExist()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        (WorkItem workItem, User assignee) = WorkItemCommandData.GetWorkItemForAssign(admin);
        _userContext.UserId.Returns(Guid.NewGuid());

        _dbContext.Seed(c => c.WorkItems, workItem);
        _dbContext.Seed(c => c.Users, assignee);

        AssignWorkItemCommand command = new(workItem.Id, assignee.Id);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(UserErrors.NotFound);
    }

    [Fact]
    public async Task Should_NotPersist_When_CallerDoesNotExist()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        (WorkItem workItem, User assignee) = WorkItemCommandData.GetWorkItemForAssign(admin);
        _userContext.UserId.Returns(Guid.NewGuid());

        _dbContext.Seed(c => c.WorkItems, workItem);
        _dbContext.Seed(c => c.Users, assignee);

        AssignWorkItemCommand command = new(workItem.Id, assignee.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnUserNotFoundError_When_CallerAndAssigneeDoNotExist()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        WorkItem workItem = WorkItemCommandData.GetWorkItem(admin);
        _userContext.UserId.Returns(Guid.NewGuid());

        _dbContext.Seed(c => c.WorkItems, workItem);
        _dbContext.Seed(c => c.Users);

        AssignWorkItemCommand command = new(workItem.Id, Guid.NewGuid());

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(UserErrors.NotFound);
    }

    [Fact]
    public async Task Should_ReturnDomainError_When_AssigneeIsNotProjectMember()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        WorkItem workItem = WorkItemCommandData.GetWorkItem(admin);
        User nonMember = WorkItemCommandData.GetNonMember();
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(WorkItemCommandData.UtcNow);

        _dbContext.Seed(c => c.WorkItems, workItem);
        _dbContext.Seed(c => c.Users, nonMember, admin);

        AssignWorkItemCommand command = new(workItem.Id, nonMember.Id);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(WorkItemErrors.AssigneeNotProjectMember);
    }

    [Fact]
    public async Task Should_NotPersist_When_AssigneeIsNotProjectMember()
    {
        // Arrange
        User admin = WorkItemCommandData.GetAdmin();
        WorkItem workItem = WorkItemCommandData.GetWorkItem(admin);
        User nonMember = WorkItemCommandData.GetNonMember();
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(WorkItemCommandData.UtcNow);

        _dbContext.Seed(c => c.WorkItems, workItem);
        _dbContext.Seed(c => c.Users, nonMember, admin);

        AssignWorkItemCommand command = new(workItem.Id, nonMember.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
