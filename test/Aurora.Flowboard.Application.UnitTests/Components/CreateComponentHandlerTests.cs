namespace Aurora.Flowboard.Application.UnitTests.Components;

public sealed class CreateComponentHandlerTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUserContext _userContext;
    private readonly CreateComponentHandler _handler;

    public CreateComponentHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _userContext = Substitute.For<IUserContext>();
        _handler = new CreateComponentHandler(_dbContext, _dateTimeProvider, _userContext);
    }

    [Fact]
    public async Task Should_ReturnComponentId_When_CommandIsValid()
    {
        // Arrange
        User admin = ComponentCommandData.GetAdmin();
        Project project = ComponentCommandData.GetProject(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(ComponentCommandData.UtcNow);

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<Component> componentsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Component>());
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.Components.Returns(componentsMock);

        CreateComponentCommand command = ComponentCommandData.GetCreateCommand(project.Id);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Should_PersistComponent_When_CommandIsValid()
    {
        // Arrange
        User admin = ComponentCommandData.GetAdmin();
        Project project = ComponentCommandData.GetProject(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(ComponentCommandData.UtcNow);

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<Component> componentsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Component>());
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.Components.Returns(componentsMock);

        CreateComponentCommand command = ComponentCommandData.GetCreateCommand(project.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _dbContext.Components.Received(1).Add(Arg.Any<Component>());
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnProjectNotFoundError_When_ProjectDoesNotExist()
    {
        // Arrange
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Project>());
        _dbContext.Projects.Returns(projectsMock);

        CreateComponentCommand command = ComponentCommandData.GetCreateCommand(Guid.NewGuid());

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.NotFound);
    }

    [Fact]
    public async Task Should_NotPersist_When_ProjectDoesNotExist()
    {
        // Arrange
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Project>());
        _dbContext.Projects.Returns(projectsMock);

        CreateComponentCommand command = ComponentCommandData.GetCreateCommand(Guid.NewGuid());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnUserNotFoundError_When_CallerDoesNotExist()
    {
        // Arrange
        User admin = ComponentCommandData.GetAdmin();
        Project project = ComponentCommandData.GetProject(admin);
        _userContext.UserId.Returns(Guid.NewGuid());

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);

        CreateComponentCommand command = ComponentCommandData.GetCreateCommand(project.Id);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(UserErrors.NotFound);
    }

    [Fact]
    public async Task Should_NotPersist_When_CallerDoesNotExist()
    {
        // Arrange
        User admin = ComponentCommandData.GetAdmin();
        Project project = ComponentCommandData.GetProject(admin);
        _userContext.UserId.Returns(Guid.NewGuid());

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);

        CreateComponentCommand command = ComponentCommandData.GetCreateCommand(project.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnDomainError_When_UserIsNotProjectAdmin()
    {
        // Arrange
        User admin = ComponentCommandData.GetAdmin();
        User nonAdmin = ComponentCommandData.GetNonAdmin();
        Project project = ComponentCommandData.GetProject(admin);
        _userContext.UserId.Returns(nonAdmin.Id);
        _dateTimeProvider.UtcNow.Returns(ComponentCommandData.UtcNow);

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([nonAdmin]);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);

        CreateComponentCommand command = ComponentCommandData.GetCreateCommand(project.Id);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ComponentErrors.OnlyAdminCanManageComponent);
    }

    [Fact]
    public async Task Should_NotPersist_When_UserIsNotProjectAdmin()
    {
        // Arrange
        User admin = ComponentCommandData.GetAdmin();
        User nonAdmin = ComponentCommandData.GetNonAdmin();
        Project project = ComponentCommandData.GetProject(admin);
        _userContext.UserId.Returns(nonAdmin.Id);
        _dateTimeProvider.UtcNow.Returns(ComponentCommandData.UtcNow);

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([nonAdmin]);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);

        CreateComponentCommand command = ComponentCommandData.GetCreateCommand(project.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnDuplicateComponentNameError_When_NameAlreadyExists()
    {
        // Arrange
        User admin = ComponentCommandData.GetAdmin();
        Project project = ComponentCommandData.GetProject(admin);
        Component.Create(ComponentCommandData.Name, project, admin, ComponentCommandData.UtcNow);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(ComponentCommandData.UtcNow);

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);

        CreateComponentCommand command = ComponentCommandData.GetCreateCommand(project.Id);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ComponentErrors.DuplicateName);
    }

    [Fact]
    public async Task Should_NotPersist_When_NameAlreadyExists()
    {
        // Arrange
        User admin = ComponentCommandData.GetAdmin();
        Project project = ComponentCommandData.GetProject(admin);
        Component.Create(ComponentCommandData.Name, project, admin, ComponentCommandData.UtcNow);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(ComponentCommandData.UtcNow);

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);

        CreateComponentCommand command = ComponentCommandData.GetCreateCommand(project.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnDomainError_When_NameIsEmpty()
    {
        // Arrange
        User admin = ComponentCommandData.GetAdmin();
        Project project = ComponentCommandData.GetProject(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(ComponentCommandData.UtcNow);

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);

        var command = new CreateComponentCommand(project.Id, string.Empty);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ComponentErrors.NameRequired);
    }

    [Fact]
    public async Task Should_NotPersist_When_NameIsEmpty()
    {
        // Arrange
        User admin = ComponentCommandData.GetAdmin();
        Project project = ComponentCommandData.GetProject(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(ComponentCommandData.UtcNow);

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);

        var command = new CreateComponentCommand(project.Id, string.Empty);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
