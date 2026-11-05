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
        User admin = ProjectCommandData.GetAdmin();
        User member = ProjectCommandData.GetMember();
        Project project = ProjectCommandData.GetProjectWithMember(admin, member);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.Users, admin, member);

        RemoveProjectMemberCommand command = ProjectCommandData.GetRemoveCommand(project.Id, member.Id);

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
        User member = ProjectCommandData.GetMember();
        Project project = ProjectCommandData.GetProjectWithMember(admin, member);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.Users, admin, member);

        RemoveProjectMemberCommand command = ProjectCommandData.GetRemoveCommand(project.Id, member.Id);

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

        RemoveProjectMemberCommand command = ProjectCommandData.GetRemoveCommand(Guid.NewGuid(), Guid.NewGuid());

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

        RemoveProjectMemberCommand command = ProjectCommandData.GetRemoveCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnUserNotFoundError_When_TargetUserDoesNotExist()
    {
        // Arrange
        User admin = ProjectCommandData.GetAdmin();
        Project project = ProjectCommandData.GetProject(admin);
        _userContext.UserId.Returns(admin.Id);

        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.Users);

        RemoveProjectMemberCommand command = ProjectCommandData.GetRemoveCommand(project.Id, Guid.NewGuid());

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
        User admin = ProjectCommandData.GetAdmin();
        Project project = ProjectCommandData.GetProject(admin);
        _userContext.UserId.Returns(admin.Id);

        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.Users);

        RemoveProjectMemberCommand command = ProjectCommandData.GetRemoveCommand(project.Id, Guid.NewGuid());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnUserNotFoundError_When_CallerDoesNotExist()
    {
        // Arrange
        User admin = ProjectCommandData.GetAdmin();
        User member = ProjectCommandData.GetMember();
        Project project = ProjectCommandData.GetProjectWithMember(admin, member);
        _userContext.UserId.Returns(Guid.NewGuid());

        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.Users, member);

        RemoveProjectMemberCommand command = ProjectCommandData.GetRemoveCommand(project.Id, member.Id);

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
        User admin = ProjectCommandData.GetAdmin();
        User member = ProjectCommandData.GetMember();
        Project project = ProjectCommandData.GetProjectWithMember(admin, member);
        _userContext.UserId.Returns(Guid.NewGuid());

        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.Users, member);

        RemoveProjectMemberCommand command = ProjectCommandData.GetRemoveCommand(project.Id, member.Id);

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
        User nonAdmin = ProjectCommandData.GetNonAdmin();
        User member = ProjectCommandData.GetMember();
        Project project = ProjectCommandData.GetProjectWithMember(admin, member);
        _userContext.UserId.Returns(nonAdmin.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.Users, nonAdmin, member);

        RemoveProjectMemberCommand command = ProjectCommandData.GetRemoveCommand(project.Id, member.Id);

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
        User admin = ProjectCommandData.GetAdmin();
        User nonAdmin = ProjectCommandData.GetNonAdmin();
        User member = ProjectCommandData.GetMember();
        Project project = ProjectCommandData.GetProjectWithMember(admin, member);
        _userContext.UserId.Returns(nonAdmin.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.Users, nonAdmin, member);

        RemoveProjectMemberCommand command = ProjectCommandData.GetRemoveCommand(project.Id, member.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnDomainError_When_MemberNotFound()
    {
        // Arrange
        User admin = ProjectCommandData.GetAdmin();
        User nonMember = ProjectCommandData.GetMember();
        Project project = ProjectCommandData.GetProject(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.Users, admin, nonMember);

        RemoveProjectMemberCommand command = ProjectCommandData.GetRemoveCommand(project.Id, nonMember.Id);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.MemberNotFound);
    }
}
