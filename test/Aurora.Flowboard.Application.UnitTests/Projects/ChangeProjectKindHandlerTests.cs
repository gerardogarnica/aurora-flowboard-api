namespace Aurora.Flowboard.Application.UnitTests.Projects;

public sealed class ChangeProjectKindHandlerTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUserContext _userContext;
    private readonly ChangeProjectKindHandler _handler;

    public ChangeProjectKindHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _userContext = Substitute.For<IUserContext>();
        _handler = new ChangeProjectKindHandler(_dbContext, _dateTimeProvider, _userContext);
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_CommandIsValid()
    {
        // Arrange
        User admin = ProjectCommandData.GetAdmin();
        Project project = ProjectCommandData.GetProject(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.Users, admin);

        ChangeProjectKindCommand command = ProjectCommandData.GetChangeKindCommand(project.Id);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task Should_PersistChanges_When_CommandIsValid()
    {
        // Arrange
        User admin = ProjectCommandData.GetAdmin();
        Project project = ProjectCommandData.GetProject(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.Users, admin);

        ChangeProjectKindCommand command = ProjectCommandData.GetChangeKindCommand(project.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnProjectNotFoundError_When_ProjectDoesNotExist()
    {
        // Arrange
        _dbContext.Seed(c => c.Projects);

        ChangeProjectKindCommand command = ProjectCommandData.GetChangeKindCommand(Guid.NewGuid());

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
        _dbContext.Seed(c => c.Projects);

        ChangeProjectKindCommand command = ProjectCommandData.GetChangeKindCommand(Guid.NewGuid());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnUserNotFoundError_When_UserDoesNotExist()
    {
        // Arrange
        User admin = ProjectCommandData.GetAdmin();
        Project project = ProjectCommandData.GetProject(admin);
        _userContext.UserId.Returns(Guid.NewGuid());

        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.Users);

        ChangeProjectKindCommand command = ProjectCommandData.GetChangeKindCommand(project.Id);

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
        User admin = ProjectCommandData.GetAdmin();
        Project project = ProjectCommandData.GetProject(admin);
        _userContext.UserId.Returns(Guid.NewGuid());

        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.Users);

        ChangeProjectKindCommand command = ProjectCommandData.GetChangeKindCommand(project.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnDomainError_When_UserIsNotProjectAdmin()
    {
        // Arrange
        User admin = ProjectCommandData.GetAdmin();
        Project project = ProjectCommandData.GetProject(admin);
        User nonAdmin = ProjectCommandData.GetNonAdmin();
        _userContext.UserId.Returns(nonAdmin.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.Users, nonAdmin);

        ChangeProjectKindCommand command = ProjectCommandData.GetChangeKindCommand(project.Id);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.OnlyAdminCanChangeKind);
    }

    [Fact]
    public async Task Should_NotPersist_When_UserIsNotProjectAdmin()
    {
        // Arrange
        User admin = ProjectCommandData.GetAdmin();
        Project project = ProjectCommandData.GetProject(admin);
        User nonAdmin = ProjectCommandData.GetNonAdmin();
        _userContext.UserId.Returns(nonAdmin.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.Users, nonAdmin);

        ChangeProjectKindCommand command = ProjectCommandData.GetChangeKindCommand(project.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnDomainError_When_KindIsUnchanged()
    {
        // Arrange
        User admin = ProjectCommandData.GetAdmin();
        Project project = ProjectCommandData.GetProject(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.Users, admin);

        var command = new ChangeProjectKindCommand(project.Id, ProjectCommandData.Kind);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.KindUnchanged);
    }

    [Fact]
    public async Task Should_NotPersist_When_KindIsUnchanged()
    {
        // Arrange
        User admin = ProjectCommandData.GetAdmin();
        Project project = ProjectCommandData.GetProject(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.Users, admin);

        var command = new ChangeProjectKindCommand(project.Id, ProjectCommandData.Kind);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnDomainError_When_ProjectIsArchived()
    {
        // Arrange
        User admin = ProjectCommandData.GetAdmin();
        Project project = ProjectCommandData.GetArchivedProject(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.Users, admin);

        ChangeProjectKindCommand command = ProjectCommandData.GetChangeKindCommand(project.Id);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.OperationNotAllowedInCurrentStatus);
    }

    [Fact]
    public async Task Should_NotPersist_When_ProjectIsArchived()
    {
        // Arrange
        User admin = ProjectCommandData.GetAdmin();
        Project project = ProjectCommandData.GetArchivedProject(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.Users, admin);

        ChangeProjectKindCommand command = ProjectCommandData.GetChangeKindCommand(project.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
