namespace Aurora.Flowboard.Application.UnitTests.Flows;

public sealed class DeactivateFlowHandlerTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUserContext _userContext;
    private readonly DeactivateFlowHandler _handler;

    public DeactivateFlowHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _userContext = Substitute.For<IUserContext>();
        _handler = new DeactivateFlowHandler(_dbContext, _dateTimeProvider, _userContext);
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_CommandIsValid()
    {
        // Arrange
        User admin = FlowCommandData.GetAdmin();
        Flow flow = FlowCommandData.GetFlow(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(FlowCommandData.UtcNow);

        DbSet<Flow> flowsMock = MockDbSetHelper.CreateMockDbSet([flow]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.Flows.Returns(flowsMock);
        _dbContext.Users.Returns(usersMock);

        DeactivateFlowCommand command = new(flow.Id);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task Should_PersistChanges_When_CommandIsValid()
    {
        // Arrange
        User admin = FlowCommandData.GetAdmin();
        Flow flow = FlowCommandData.GetFlow(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(FlowCommandData.UtcNow);

        DbSet<Flow> flowsMock = MockDbSetHelper.CreateMockDbSet([flow]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.Flows.Returns(flowsMock);
        _dbContext.Users.Returns(usersMock);

        DeactivateFlowCommand command = new(flow.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnFlowNotFoundError_When_FlowDoesNotExist()
    {
        // Arrange
        DbSet<Flow> flowsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Flow>());
        _dbContext.Flows.Returns(flowsMock);

        DeactivateFlowCommand command = new(Guid.NewGuid());

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(FlowErrors.NotFound);
    }

    [Fact]
    public async Task Should_NotPersist_When_FlowDoesNotExist()
    {
        // Arrange
        DbSet<Flow> flowsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Flow>());
        _dbContext.Flows.Returns(flowsMock);

        DeactivateFlowCommand command = new(Guid.NewGuid());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnUserNotFoundError_When_UserDoesNotExist()
    {
        // Arrange
        User admin = FlowCommandData.GetAdmin();
        Flow flow = FlowCommandData.GetFlow(admin);
        _userContext.UserId.Returns(Guid.NewGuid());

        DbSet<Flow> flowsMock = MockDbSetHelper.CreateMockDbSet([flow]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<User>());
        _dbContext.Flows.Returns(flowsMock);
        _dbContext.Users.Returns(usersMock);

        DeactivateFlowCommand command = new(flow.Id);

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
        User admin = FlowCommandData.GetAdmin();
        Flow flow = FlowCommandData.GetFlow(admin);
        _userContext.UserId.Returns(Guid.NewGuid());

        DbSet<Flow> flowsMock = MockDbSetHelper.CreateMockDbSet([flow]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<User>());
        _dbContext.Flows.Returns(flowsMock);
        _dbContext.Users.Returns(usersMock);

        DeactivateFlowCommand command = new(flow.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnDomainError_When_UserIsNotProjectAdmin()
    {
        // Arrange
        User admin = FlowCommandData.GetAdmin();
        Flow flow = FlowCommandData.GetFlow(admin);
        User nonAdmin = FlowCommandData.GetNonAdmin();
        _userContext.UserId.Returns(nonAdmin.Id);
        _dateTimeProvider.UtcNow.Returns(FlowCommandData.UtcNow);

        DbSet<Flow> flowsMock = MockDbSetHelper.CreateMockDbSet([flow]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([nonAdmin]);
        _dbContext.Flows.Returns(flowsMock);
        _dbContext.Users.Returns(usersMock);

        DeactivateFlowCommand command = new(flow.Id);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(FlowErrors.OnlyAdminCanModifyFlow);
    }

    [Fact]
    public async Task Should_ReturnDomainError_When_FlowIsAlreadyDeactivated()
    {
        // Arrange
        User admin = FlowCommandData.GetAdmin();
        Flow flow = FlowCommandData.GetFlow(admin);
        flow.Deactivate(admin, FlowCommandData.UtcNow);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(FlowCommandData.UtcNow);

        DbSet<Flow> flowsMock = MockDbSetHelper.CreateMockDbSet([flow]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.Flows.Returns(flowsMock);
        _dbContext.Users.Returns(usersMock);

        DeactivateFlowCommand command = new(flow.Id);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(FlowErrors.AlreadyDeactivated);
    }

    [Fact]
    public async Task Should_NotPersist_When_DomainValidationFails()
    {
        // Arrange
        User admin = FlowCommandData.GetAdmin();
        Flow flow = FlowCommandData.GetFlow(admin);
        User nonAdmin = FlowCommandData.GetNonAdmin();
        _userContext.UserId.Returns(nonAdmin.Id);
        _dateTimeProvider.UtcNow.Returns(FlowCommandData.UtcNow);

        DbSet<Flow> flowsMock = MockDbSetHelper.CreateMockDbSet([flow]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([nonAdmin]);
        _dbContext.Flows.Returns(flowsMock);
        _dbContext.Users.Returns(usersMock);

        DeactivateFlowCommand command = new(flow.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
