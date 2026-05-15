namespace Aurora.Flowboard.Application.UnitTests.Projects;

public sealed class SetupProjectHandlerTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUserContext _userContext;
    private readonly IDbContextTransaction _transaction;
    private readonly SetupProjectHandler _handler;

    public SetupProjectHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _userContext = Substitute.For<IUserContext>();
        _transaction = Substitute.For<IDbContextTransaction>();
        _handler = new SetupProjectHandler(_dbContext, _dateTimeProvider, _userContext);

        _dbContext.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(_transaction);
    }

    [Fact]
    public async Task Should_ReturnProjectId_When_CommandIsValid()
    {
        // Arrange
        User user = ProjectCommandData.GetUser();
        _userContext.UserId.Returns(user.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Project>());
        DbSet<Flow> flowsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Flow>());
        _dbContext.Users.Returns(usersMock);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Flows.Returns(flowsMock);

        SetupProjectCommand command = ProjectCommandData.GetSetupCommand();

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Should_PersistAndCommit_When_CommandIsValid()
    {
        // Arrange
        User user = ProjectCommandData.GetUser();
        _userContext.UserId.Returns(user.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Project>());
        DbSet<Flow> flowsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Flow>());
        _dbContext.Users.Returns(usersMock);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Flows.Returns(flowsMock);

        SetupProjectCommand command = ProjectCommandData.GetSetupCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnUserNotFoundError_When_UserDoesNotExist()
    {
        // Arrange
        _userContext.UserId.Returns(Guid.NewGuid());

        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<User>());
        _dbContext.Users.Returns(usersMock);

        SetupProjectCommand command = ProjectCommandData.GetSetupCommand();

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(UserErrors.NotFound);
    }

    [Fact]
    public async Task Should_RollbackAndNotPersist_When_UserDoesNotExist()
    {
        // Arrange
        _userContext.UserId.Returns(Guid.NewGuid());

        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<User>());
        _dbContext.Users.Returns(usersMock);

        SetupProjectCommand command = ProjectCommandData.GetSetupCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _transaction.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnDomainError_When_ProjectCreationFails()
    {
        // Arrange
        User user = ProjectCommandData.GetUser();
        _userContext.UserId.Returns(user.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        _dbContext.Users.Returns(usersMock);

        var command = new SetupProjectCommand(string.Empty, null, ProjectCommandData.Code, null, ProjectCommandData.GetSetupFlowDto());

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.NameRequired);
    }

    [Fact]
    public async Task Should_RollbackAndNotPersist_When_ProjectCreationFails()
    {
        // Arrange
        User user = ProjectCommandData.GetUser();
        _userContext.UserId.Returns(user.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        _dbContext.Users.Returns(usersMock);

        var command = new SetupProjectCommand(string.Empty, null, ProjectCommandData.Code, null, ProjectCommandData.GetSetupFlowDto());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _transaction.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnDomainError_When_FlowCreationFails()
    {
        // Arrange
        User user = ProjectCommandData.GetUser();
        _userContext.UserId.Returns(user.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Project>());
        _dbContext.Users.Returns(usersMock);
        _dbContext.Projects.Returns(projectsMock);

        var invalidFlow = new SetupProjectFlowDto(string.Empty, null, []);
        var command = new SetupProjectCommand(ProjectCommandData.Name, null, ProjectCommandData.Code, null, invalidFlow);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(FlowErrors.NameRequired);
    }

    [Fact]
    public async Task Should_RollbackAndNotPersist_When_FlowCreationFails()
    {
        // Arrange
        User user = ProjectCommandData.GetUser();
        _userContext.UserId.Returns(user.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Project>());
        _dbContext.Users.Returns(usersMock);
        _dbContext.Projects.Returns(projectsMock);

        var invalidFlow = new SetupProjectFlowDto(string.Empty, null, []);
        var command = new SetupProjectCommand(ProjectCommandData.Name, null, ProjectCommandData.Code, null, invalidFlow);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _transaction.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnDomainError_When_StateAdditionFails()
    {
        // Arrange
        User user = ProjectCommandData.GetUser();
        _userContext.UserId.Returns(user.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Project>());
        DbSet<Flow> flowsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Flow>());
        _dbContext.Users.Returns(usersMock);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Flows.Returns(flowsMock);

        var flowWithDuplicateState = new SetupProjectFlowDto("Sprint Flow", null,
        [
            new("Todo", FlowStateCategory.Active, [ProjectRole.Developer]),
            new("Todo", FlowStateCategory.Active, [ProjectRole.Developer]),
            new("Done", FlowStateCategory.Completed, [ProjectRole.Developer]),
            new("Cancelled", FlowStateCategory.Cancelled, [ProjectRole.Developer])
        ]);
        var command = new SetupProjectCommand(ProjectCommandData.Name, null, ProjectCommandData.Code, null, flowWithDuplicateState);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(FlowErrors.DuplicateStateName);
    }

    [Fact]
    public async Task Should_RollbackAndNotPersist_When_StateAdditionFails()
    {
        // Arrange
        User user = ProjectCommandData.GetUser();
        _userContext.UserId.Returns(user.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Project>());
        DbSet<Flow> flowsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Flow>());
        _dbContext.Users.Returns(usersMock);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Flows.Returns(flowsMock);

        var flowWithDuplicateState = new SetupProjectFlowDto("Sprint Flow", null,
        [
            new("Todo", FlowStateCategory.Active, [ProjectRole.Developer]),
            new("Todo", FlowStateCategory.Active, [ProjectRole.Developer]),
            new("Done", FlowStateCategory.Completed, [ProjectRole.Developer]),
            new("Cancelled", FlowStateCategory.Cancelled, [ProjectRole.Developer])
        ]);
        var command = new SetupProjectCommand(ProjectCommandData.Name, null, ProjectCommandData.Code, null, flowWithDuplicateState);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _transaction.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
