namespace Aurora.Flowboard.Application.UnitTests.Flows;

public sealed class AddFlowTransitionRoleHandlerTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IUserContext _userContext;
    private readonly AddFlowTransitionRoleHandler _handler;

    public AddFlowTransitionRoleHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _userContext = Substitute.For<IUserContext>();
        _handler = new AddFlowTransitionRoleHandler(_dbContext, _userContext);
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_CommandIsValid()
    {
        // Arrange
        User admin = FlowCommandData.GetAdmin();
        Flow flow = FlowCommandData.GetFlowWithTransition(admin, out FlowTransition transition);
        _userContext.UserId.Returns(admin.Id);

        DbSet<Flow> flowsMock = MockDbSetHelper.CreateMockDbSet([flow]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.Flows.Returns(flowsMock);
        _dbContext.Users.Returns(usersMock);

        AddFlowTransitionRoleCommand command = new(flow.Id, transition.Id, ProjectRole.QA);

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
        Flow flow = FlowCommandData.GetFlowWithTransition(admin, out FlowTransition transition);
        _userContext.UserId.Returns(admin.Id);

        DbSet<Flow> flowsMock = MockDbSetHelper.CreateMockDbSet([flow]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.Flows.Returns(flowsMock);
        _dbContext.Users.Returns(usersMock);

        AddFlowTransitionRoleCommand command = new(flow.Id, transition.Id, ProjectRole.QA);

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

        AddFlowTransitionRoleCommand command = new(Guid.NewGuid(), Guid.NewGuid(), ProjectRole.QA);

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

        AddFlowTransitionRoleCommand command = new(Guid.NewGuid(), Guid.NewGuid(), ProjectRole.QA);

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
        Flow flow = FlowCommandData.GetFlowWithTransition(admin, out FlowTransition transition);
        _userContext.UserId.Returns(Guid.NewGuid());

        DbSet<Flow> flowsMock = MockDbSetHelper.CreateMockDbSet([flow]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<User>());
        _dbContext.Flows.Returns(flowsMock);
        _dbContext.Users.Returns(usersMock);

        AddFlowTransitionRoleCommand command = new(flow.Id, transition.Id, ProjectRole.QA);

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
        Flow flow = FlowCommandData.GetFlowWithTransition(admin, out FlowTransition transition);
        User nonAdmin = FlowCommandData.GetNonAdmin();
        _userContext.UserId.Returns(nonAdmin.Id);

        DbSet<Flow> flowsMock = MockDbSetHelper.CreateMockDbSet([flow]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([nonAdmin]);
        _dbContext.Flows.Returns(flowsMock);
        _dbContext.Users.Returns(usersMock);

        AddFlowTransitionRoleCommand command = new(flow.Id, transition.Id, ProjectRole.QA);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(FlowErrors.OnlyAdminCanModifyFlow);
    }

    [Fact]
    public async Task Should_ReturnDomainError_When_TransitionDoesNotExist()
    {
        // Arrange
        User admin = FlowCommandData.GetAdmin();
        Flow flow = FlowCommandData.GetFlowWithTransition(admin, out _);
        _userContext.UserId.Returns(admin.Id);

        DbSet<Flow> flowsMock = MockDbSetHelper.CreateMockDbSet([flow]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.Flows.Returns(flowsMock);
        _dbContext.Users.Returns(usersMock);

        AddFlowTransitionRoleCommand command = new(flow.Id, Guid.NewGuid(), ProjectRole.QA);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(FlowErrors.TransitionNotFound);
    }

    [Fact]
    public async Task Should_NotPersist_When_DomainValidationFails()
    {
        // Arrange
        User admin = FlowCommandData.GetAdmin();
        Flow flow = FlowCommandData.GetFlowWithTransition(admin, out FlowTransition transition);
        User nonAdmin = FlowCommandData.GetNonAdmin();
        _userContext.UserId.Returns(nonAdmin.Id);

        DbSet<Flow> flowsMock = MockDbSetHelper.CreateMockDbSet([flow]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([nonAdmin]);
        _dbContext.Flows.Returns(flowsMock);
        _dbContext.Users.Returns(usersMock);

        AddFlowTransitionRoleCommand command = new(flow.Id, transition.Id, ProjectRole.QA);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
