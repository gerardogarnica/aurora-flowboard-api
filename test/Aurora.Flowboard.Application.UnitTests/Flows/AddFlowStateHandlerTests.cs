namespace Aurora.Flowboard.Application.UnitTests.Flows;

public sealed class AddFlowStateHandlerTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IUserContext _userContext;
    private readonly AddFlowStateHandler _handler;

    public AddFlowStateHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _userContext = Substitute.For<IUserContext>();
        _handler = new AddFlowStateHandler(_dbContext, _userContext);
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_CommandIsValid()
    {
        // Arrange
        User admin = FlowCommandData.GetAdmin();
        Flow flow = FlowCommandData.GetFlow(admin);
        _userContext.UserId.Returns(admin.Id);

        DbSet<Flow> flowsMock = MockDbSetHelper.CreateMockDbSet([flow]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.Flows.Returns(flowsMock);
        _dbContext.Users.Returns(usersMock);

        AddFlowStateCommand command = new(flow.Id, FlowCommandData.StateName, FlowStateCategory.Active, FlowCommandData.StateColor, [ProjectRole.Developer]);

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

        DbSet<Flow> flowsMock = MockDbSetHelper.CreateMockDbSet([flow]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.Flows.Returns(flowsMock);
        _dbContext.Users.Returns(usersMock);

        AddFlowStateCommand command = new(flow.Id, FlowCommandData.StateName, FlowStateCategory.Active, FlowCommandData.StateColor, [ProjectRole.Developer]);

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

        AddFlowStateCommand command = new(Guid.NewGuid(), FlowCommandData.StateName, FlowStateCategory.Active, FlowCommandData.StateColor, [ProjectRole.Developer]);

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

        AddFlowStateCommand command = new(Guid.NewGuid(), FlowCommandData.StateName, FlowStateCategory.Active, FlowCommandData.StateColor, [ProjectRole.Developer]);

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

        AddFlowStateCommand command = new(flow.Id, FlowCommandData.StateName, FlowStateCategory.Active, FlowCommandData.StateColor, [ProjectRole.Developer]);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(UserErrors.NotFound);
    }

    [Fact]
    public async Task Should_ReturnDomainError_When_UserIsNotProjectAdmin()
    {
        // Arrange
        User admin = FlowCommandData.GetAdmin();
        Flow flow = FlowCommandData.GetFlow(admin);
        User nonAdmin = FlowCommandData.GetNonAdmin();
        _userContext.UserId.Returns(nonAdmin.Id);

        DbSet<Flow> flowsMock = MockDbSetHelper.CreateMockDbSet([flow]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([nonAdmin]);
        _dbContext.Flows.Returns(flowsMock);
        _dbContext.Users.Returns(usersMock);

        AddFlowStateCommand command = new(flow.Id, FlowCommandData.StateName, FlowStateCategory.Active, FlowCommandData.StateColor, [ProjectRole.Developer]);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(FlowErrors.OnlyAdminCanModifyFlow);
    }

    [Fact]
    public async Task Should_ReturnDomainError_When_StateNameAlreadyExists()
    {
        // Arrange
        User admin = FlowCommandData.GetAdmin();
        Flow flow = FlowCommandData.GetFlow(admin);
        flow.AddState("Existing State", FlowStateCategory.Active, Color.Create("white").Value, [ProjectRole.Admin], admin);
        _userContext.UserId.Returns(admin.Id);

        DbSet<Flow> flowsMock = MockDbSetHelper.CreateMockDbSet([flow]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.Flows.Returns(flowsMock);
        _dbContext.Users.Returns(usersMock);

        AddFlowStateCommand command = new(flow.Id, "Existing State", FlowStateCategory.Active, FlowCommandData.StateColor, [ProjectRole.Developer]);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(FlowErrors.DuplicateStateName);
    }

    [Fact]
    public async Task Should_NotPersist_When_DomainValidationFails()
    {
        // Arrange
        User admin = FlowCommandData.GetAdmin();
        Flow flow = FlowCommandData.GetFlow(admin);
        User nonAdmin = FlowCommandData.GetNonAdmin();
        _userContext.UserId.Returns(nonAdmin.Id);

        DbSet<Flow> flowsMock = MockDbSetHelper.CreateMockDbSet([flow]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([nonAdmin]);
        _dbContext.Flows.Returns(flowsMock);
        _dbContext.Users.Returns(usersMock);

        AddFlowStateCommand command = new(flow.Id, FlowCommandData.StateName, FlowStateCategory.Active, FlowCommandData.StateColor, [ProjectRole.Developer]);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
