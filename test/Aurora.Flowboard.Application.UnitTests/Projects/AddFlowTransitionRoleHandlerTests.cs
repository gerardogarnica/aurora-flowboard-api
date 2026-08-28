namespace Aurora.Flowboard.Application.UnitTests.Projects;

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
        User admin = ProjectCommandData.GetAdmin();
        (Project project, FlowTransition transition) = ProjectCommandData.GetProjectWithFlowTransition(admin);
        _userContext.UserId.Returns(admin.Id);

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);

        AddFlowTransitionRoleCommand command = new(project.Id, transition.Id, ProjectRole.QA);

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
        (Project project, FlowTransition transition) = ProjectCommandData.GetProjectWithFlowTransition(admin);
        _userContext.UserId.Returns(admin.Id);

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);

        AddFlowTransitionRoleCommand command = new(project.Id, transition.Id, ProjectRole.QA);

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

        AddFlowTransitionRoleCommand command = new(Guid.NewGuid(), Guid.NewGuid(), ProjectRole.QA);

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
        User admin = ProjectCommandData.GetAdmin();
        (Project project, FlowTransition transition) = ProjectCommandData.GetProjectWithFlowTransition(admin);
        _userContext.UserId.Returns(Guid.NewGuid());

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<User>());
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);

        AddFlowTransitionRoleCommand command = new(project.Id, transition.Id, ProjectRole.QA);

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
        User admin = ProjectCommandData.GetAdmin();
        (Project project, FlowTransition transition) = ProjectCommandData.GetProjectWithFlowTransition(admin);
        User nonAdmin = ProjectCommandData.GetNonAdmin();
        _userContext.UserId.Returns(nonAdmin.Id);

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([nonAdmin]);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);

        AddFlowTransitionRoleCommand command = new(project.Id, transition.Id, ProjectRole.QA);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.OnlyAdminCanModifyFlow);
    }

    [Fact]
    public async Task Should_ReturnDomainError_When_TransitionDoesNotExist()
    {
        // Arrange
        User admin = ProjectCommandData.GetAdmin();
        Project project = ProjectCommandData.GetProjectWithFlowStates(admin);
        _userContext.UserId.Returns(admin.Id);

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);

        AddFlowTransitionRoleCommand command = new(project.Id, Guid.NewGuid(), ProjectRole.QA);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.FlowTransitionNotFound);
    }

    [Fact]
    public async Task Should_NotPersist_When_DomainValidationFails()
    {
        // Arrange
        User admin = ProjectCommandData.GetAdmin();
        Project project = ProjectCommandData.GetProjectWithFlowStates(admin);
        _userContext.UserId.Returns(admin.Id);

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);

        AddFlowTransitionRoleCommand command = new(project.Id, Guid.NewGuid(), ProjectRole.QA);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
