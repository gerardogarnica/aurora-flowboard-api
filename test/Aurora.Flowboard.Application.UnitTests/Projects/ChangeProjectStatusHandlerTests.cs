namespace Aurora.Flowboard.Application.UnitTests.Projects;

public sealed class ChangeProjectStatusHandlerTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUserContext _userContext;
    private readonly ChangeProjectStatusHandler _handler;

    public ChangeProjectStatusHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _userContext = Substitute.For<IUserContext>();
        _handler = new ChangeProjectStatusHandler(_dbContext, _dateTimeProvider, _userContext);
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_CommandIsValid()
    {
        // Arrange
        User admin = ChangeProjectStatusCommandData.GetAdmin();
        Project project = ChangeProjectStatusCommandData.GetDraftProject(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(ChangeProjectStatusCommandData.UtcNow);

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);

        ChangeProjectStatusCommand command = ChangeProjectStatusCommandData.GetValidCommand(project.Id);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task Should_PersistChanges_When_CommandIsValid()
    {
        // Arrange
        User admin = ChangeProjectStatusCommandData.GetAdmin();
        Project project = ChangeProjectStatusCommandData.GetDraftProject(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(ChangeProjectStatusCommandData.UtcNow);

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);

        ChangeProjectStatusCommand command = ChangeProjectStatusCommandData.GetValidCommand(project.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnProjectNotFoundError_When_ProjectDoesNotExist()
    {
        // Arrange
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Project>());
        _dbContext.Projects.Returns(projectsMock);

        ChangeProjectStatusCommand command = ChangeProjectStatusCommandData.GetValidCommand(Guid.NewGuid());

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

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

        ChangeProjectStatusCommand command = ChangeProjectStatusCommandData.GetValidCommand(Guid.NewGuid());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnUserNotFoundError_When_UserDoesNotExist()
    {
        // Arrange
        User admin = ChangeProjectStatusCommandData.GetAdmin();
        Project project = ChangeProjectStatusCommandData.GetDraftProject(admin);
        _userContext.UserId.Returns(Guid.NewGuid());

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<User>());
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);

        ChangeProjectStatusCommand command = ChangeProjectStatusCommandData.GetValidCommand(project.Id);

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
        User admin = ChangeProjectStatusCommandData.GetAdmin();
        Project project = ChangeProjectStatusCommandData.GetDraftProject(admin);
        _userContext.UserId.Returns(Guid.NewGuid());

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<User>());
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);

        ChangeProjectStatusCommand command = ChangeProjectStatusCommandData.GetValidCommand(project.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnDomainError_When_UserIsNotProjectAdmin()
    {
        // Arrange
        User admin = ChangeProjectStatusCommandData.GetAdmin();
        Project project = ChangeProjectStatusCommandData.GetDraftProject(admin);
        User nonAdmin = ChangeProjectStatusCommandData.GetNonAdmin();
        _userContext.UserId.Returns(nonAdmin.Id);
        _dateTimeProvider.UtcNow.Returns(ChangeProjectStatusCommandData.UtcNow);

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([nonAdmin]);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);

        ChangeProjectStatusCommand command = ChangeProjectStatusCommandData.GetValidCommand(project.Id);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.OnlyAdminCanChangeStatus);
    }

    [Fact]
    public async Task Should_NotPersist_When_ChangeStatusFails()
    {
        // Arrange
        User admin = ChangeProjectStatusCommandData.GetAdmin();
        Project project = ChangeProjectStatusCommandData.GetDraftProject(admin);
        User nonAdmin = ChangeProjectStatusCommandData.GetNonAdmin();
        _userContext.UserId.Returns(nonAdmin.Id);
        _dateTimeProvider.UtcNow.Returns(ChangeProjectStatusCommandData.UtcNow);

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([nonAdmin]);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);

        ChangeProjectStatusCommand command = ChangeProjectStatusCommandData.GetValidCommand(project.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnDomainError_When_StatusTransitionIsInvalid()
    {
        // Arrange
        User admin = ChangeProjectStatusCommandData.GetAdmin();
        Project project = ChangeProjectStatusCommandData.GetArchivedProject(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(ChangeProjectStatusCommandData.UtcNow);

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);

        var command = new ChangeProjectStatusCommand(project.Id, ProjectStatus.Active);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.InvalidStatusTransition);
    }

    [Fact]
    public async Task Should_NotPersist_When_StatusTransitionIsInvalid()
    {
        // Arrange
        User admin = ChangeProjectStatusCommandData.GetAdmin();
        Project project = ChangeProjectStatusCommandData.GetArchivedProject(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(ChangeProjectStatusCommandData.UtcNow);

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);

        var command = new ChangeProjectStatusCommand(project.Id, ProjectStatus.Active);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
