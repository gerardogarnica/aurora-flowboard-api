namespace Aurora.Flowboard.Application.UnitTests.Projects;

public sealed class RemoveProjectMemberHandlerTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUserContext _userContext;
    private readonly RemoveProjectMemberHandler _handler;

    public RemoveProjectMemberHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _userContext = Substitute.For<IUserContext>();
        _handler = new RemoveProjectMemberHandler(_dbContext, _dateTimeProvider, _userContext);
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_CommandIsValid()
    {
        // Arrange
        User admin = RemoveProjectMemberCommandData.GetAdmin();
        User member = RemoveProjectMemberCommandData.GetMember();
        Project project = RemoveProjectMemberCommandData.GetProjectWithMember(admin, member);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(RemoveProjectMemberCommandData.UtcNow);

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin, member]);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);

        RemoveProjectMemberCommand command = RemoveProjectMemberCommandData.GetValidCommand(project.Id, member.Id);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task Should_PersistChanges_When_CommandIsValid()
    {
        // Arrange
        User admin = RemoveProjectMemberCommandData.GetAdmin();
        User member = RemoveProjectMemberCommandData.GetMember();
        Project project = RemoveProjectMemberCommandData.GetProjectWithMember(admin, member);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(RemoveProjectMemberCommandData.UtcNow);

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin, member]);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);

        RemoveProjectMemberCommand command = RemoveProjectMemberCommandData.GetValidCommand(project.Id, member.Id);

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

        RemoveProjectMemberCommand command = RemoveProjectMemberCommandData.GetValidCommand(Guid.NewGuid(), Guid.NewGuid());

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

        RemoveProjectMemberCommand command = RemoveProjectMemberCommandData.GetValidCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnUserNotFoundError_When_TargetUserDoesNotExist()
    {
        // Arrange
        User admin = RemoveProjectMemberCommandData.GetAdmin();
        Project project = RemoveProjectMemberCommandData.GetDraftProject(admin);
        _userContext.UserId.Returns(admin.Id);

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<User>());
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);

        RemoveProjectMemberCommand command = RemoveProjectMemberCommandData.GetValidCommand(project.Id, Guid.NewGuid());

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(UserErrors.NotFound);
    }

    [Fact]
    public async Task Should_NotPersist_When_TargetUserDoesNotExist()
    {
        // Arrange
        User admin = RemoveProjectMemberCommandData.GetAdmin();
        Project project = RemoveProjectMemberCommandData.GetDraftProject(admin);
        _userContext.UserId.Returns(admin.Id);

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<User>());
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);

        RemoveProjectMemberCommand command = RemoveProjectMemberCommandData.GetValidCommand(project.Id, Guid.NewGuid());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnUserNotFoundError_When_CallerDoesNotExist()
    {
        // Arrange
        User admin = RemoveProjectMemberCommandData.GetAdmin();
        User member = RemoveProjectMemberCommandData.GetMember();
        Project project = RemoveProjectMemberCommandData.GetProjectWithMember(admin, member);
        _userContext.UserId.Returns(Guid.NewGuid());

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([member]);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);

        RemoveProjectMemberCommand command = RemoveProjectMemberCommandData.GetValidCommand(project.Id, member.Id);

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
        User admin = RemoveProjectMemberCommandData.GetAdmin();
        User member = RemoveProjectMemberCommandData.GetMember();
        Project project = RemoveProjectMemberCommandData.GetProjectWithMember(admin, member);
        _userContext.UserId.Returns(Guid.NewGuid());

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([member]);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);

        RemoveProjectMemberCommand command = RemoveProjectMemberCommandData.GetValidCommand(project.Id, member.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnDomainError_When_UserIsNotProjectAdmin()
    {
        // Arrange
        User admin = RemoveProjectMemberCommandData.GetAdmin();
        User nonAdmin = RemoveProjectMemberCommandData.GetNonAdmin();
        User member = RemoveProjectMemberCommandData.GetMember();
        Project project = RemoveProjectMemberCommandData.GetProjectWithMember(admin, member);
        _userContext.UserId.Returns(nonAdmin.Id);
        _dateTimeProvider.UtcNow.Returns(RemoveProjectMemberCommandData.UtcNow);

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([nonAdmin, member]);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);

        RemoveProjectMemberCommand command = RemoveProjectMemberCommandData.GetValidCommand(project.Id, member.Id);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.OnlyAdminCanRemoveMembers);
    }

    [Fact]
    public async Task Should_NotPersist_When_RemoveMemberFails()
    {
        // Arrange
        User admin = RemoveProjectMemberCommandData.GetAdmin();
        User nonAdmin = RemoveProjectMemberCommandData.GetNonAdmin();
        User member = RemoveProjectMemberCommandData.GetMember();
        Project project = RemoveProjectMemberCommandData.GetProjectWithMember(admin, member);
        _userContext.UserId.Returns(nonAdmin.Id);
        _dateTimeProvider.UtcNow.Returns(RemoveProjectMemberCommandData.UtcNow);

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([nonAdmin, member]);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);

        RemoveProjectMemberCommand command = RemoveProjectMemberCommandData.GetValidCommand(project.Id, member.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnDomainError_When_MemberNotFound()
    {
        // Arrange
        User admin = RemoveProjectMemberCommandData.GetAdmin();
        User nonMember = RemoveProjectMemberCommandData.GetMember();
        Project project = RemoveProjectMemberCommandData.GetDraftProject(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(RemoveProjectMemberCommandData.UtcNow);

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin, nonMember]);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);

        RemoveProjectMemberCommand command = RemoveProjectMemberCommandData.GetValidCommand(project.Id, nonMember.Id);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.MemberNotFound);
    }
}
