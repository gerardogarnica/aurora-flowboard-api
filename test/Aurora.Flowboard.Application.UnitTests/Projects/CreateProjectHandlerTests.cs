namespace Aurora.Flowboard.Application.UnitTests.Projects;

public sealed class CreateProjectHandlerTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUserContext _userContext;
    private readonly CreateProjectHandler _handler;

    public CreateProjectHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _userContext = Substitute.For<IUserContext>();
        _handler = new CreateProjectHandler(_dbContext, _dateTimeProvider, _userContext);
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
        _dbContext.Users.Returns(usersMock);
        _dbContext.Projects.Returns(projectsMock);

        CreateProjectCommand command = ProjectCommandData.GetCreateCommand();

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Should_PersistProject_When_CommandIsValid()
    {
        // Arrange
        User user = ProjectCommandData.GetUser();
        _userContext.UserId.Returns(user.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Project>());
        _dbContext.Users.Returns(usersMock);
        _dbContext.Projects.Returns(projectsMock);

        CreateProjectCommand command = ProjectCommandData.GetCreateCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnUserNotFoundError_When_UserDoesNotExist()
    {
        // Arrange
        _userContext.UserId.Returns(Guid.NewGuid());

        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<User>());
        _dbContext.Users.Returns(usersMock);

        CreateProjectCommand command = ProjectCommandData.GetCreateCommand();

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(UserErrors.NotFound);
    }

    [Fact]
    public async Task Should_NotPersist_When_UserDoesNotExist()
    {
        // Arrange
        _userContext.UserId.Returns(Guid.NewGuid());

        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<User>());
        _dbContext.Users.Returns(usersMock);

        CreateProjectCommand command = ProjectCommandData.GetCreateCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
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

        var command = new CreateProjectCommand(string.Empty, null, ProjectCommandData.Code, ProjectCommandData.Color, null);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.NameRequired);
    }

    [Fact]
    public async Task Should_NotPersist_When_ProjectCreationFails()
    {
        // Arrange
        User user = ProjectCommandData.GetUser();
        _userContext.UserId.Returns(user.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        _dbContext.Users.Returns(usersMock);

        var command = new CreateProjectCommand(string.Empty, null, ProjectCommandData.Code, ProjectCommandData.Color, null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
