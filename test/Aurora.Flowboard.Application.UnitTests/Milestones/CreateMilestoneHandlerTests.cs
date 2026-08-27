namespace Aurora.Flowboard.Application.UnitTests.Milestones;

public sealed class CreateMilestoneHandlerTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUserContext _userContext;
    private readonly CreateMilestoneHandler _handler;

    public CreateMilestoneHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _userContext = Substitute.For<IUserContext>();
        _handler = new CreateMilestoneHandler(_dbContext, _dateTimeProvider, _userContext);
    }

    [Fact]
    public async Task Should_ReturnMilestoneId_When_CommandIsValid()
    {
        // Arrange
        User admin = MilestoneCommandData.GetAdmin();
        Project project = MilestoneCommandData.GetProject(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(MilestoneCommandData.UtcNow);

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Milestone>());
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.Milestones.Returns(milestonesMock);

        CreateMilestoneCommand command = MilestoneCommandData.GetCreateCommand(project.Id);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Should_PersistMilestone_When_CommandIsValid()
    {
        // Arrange
        User admin = MilestoneCommandData.GetAdmin();
        Project project = MilestoneCommandData.GetProject(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(MilestoneCommandData.UtcNow);

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Milestone>());
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.Milestones.Returns(milestonesMock);

        CreateMilestoneCommand command = MilestoneCommandData.GetCreateCommand(project.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _dbContext.Milestones.Received(1).Add(Arg.Any<Milestone>());
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnProjectNotFoundError_When_ProjectDoesNotExist()
    {
        // Arrange
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Project>());
        _dbContext.Projects.Returns(projectsMock);

        CreateMilestoneCommand command = MilestoneCommandData.GetCreateCommand(Guid.NewGuid());

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

        CreateMilestoneCommand command = MilestoneCommandData.GetCreateCommand(Guid.NewGuid());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnUserNotFoundError_When_CallerDoesNotExist()
    {
        // Arrange
        User admin = MilestoneCommandData.GetAdmin();
        Project project = MilestoneCommandData.GetProject(admin);
        _userContext.UserId.Returns(Guid.NewGuid());

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);

        CreateMilestoneCommand command = MilestoneCommandData.GetCreateCommand(project.Id);

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
        User admin = MilestoneCommandData.GetAdmin();
        Project project = MilestoneCommandData.GetProject(admin);
        _userContext.UserId.Returns(Guid.NewGuid());

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);

        CreateMilestoneCommand command = MilestoneCommandData.GetCreateCommand(project.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnDomainError_When_UserIsNotProjectAdmin()
    {
        // Arrange
        User admin = MilestoneCommandData.GetAdmin();
        User nonAdmin = MilestoneCommandData.GetNonAdmin();
        Project project = MilestoneCommandData.GetProject(admin);
        _userContext.UserId.Returns(nonAdmin.Id);
        _dateTimeProvider.UtcNow.Returns(MilestoneCommandData.UtcNow);

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([nonAdmin]);
        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Milestone>());
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.Milestones.Returns(milestonesMock);

        CreateMilestoneCommand command = MilestoneCommandData.GetCreateCommand(project.Id);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(MilestoneErrors.OnlyAdminCanManageMilestone);
    }

    [Fact]
    public async Task Should_NotPersist_When_UserIsNotProjectAdmin()
    {
        // Arrange
        User admin = MilestoneCommandData.GetAdmin();
        User nonAdmin = MilestoneCommandData.GetNonAdmin();
        Project project = MilestoneCommandData.GetProject(admin);
        _userContext.UserId.Returns(nonAdmin.Id);
        _dateTimeProvider.UtcNow.Returns(MilestoneCommandData.UtcNow);

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([nonAdmin]);
        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Milestone>());
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.Milestones.Returns(milestonesMock);

        CreateMilestoneCommand command = MilestoneCommandData.GetCreateCommand(project.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnDuplicateNameError_When_NameAlreadyExistsInProject()
    {
        // Arrange
        User admin = MilestoneCommandData.GetAdmin();
        Project project = MilestoneCommandData.GetProject(admin);
        Milestone existing = Milestone.Create(
            MilestoneCommandData.Name,
            MilestoneCommandData.Description,
            null,
            null,
            project,
            admin,
            MilestoneCommandData.UtcNow).Value;
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(MilestoneCommandData.UtcNow);

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet([existing]);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.Milestones.Returns(milestonesMock);

        CreateMilestoneCommand command = MilestoneCommandData.GetCreateCommand(project.Id);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(MilestoneErrors.DuplicateName);
    }

    [Fact]
    public async Task Should_NotPersist_When_NameAlreadyExistsInProject()
    {
        // Arrange
        User admin = MilestoneCommandData.GetAdmin();
        Project project = MilestoneCommandData.GetProject(admin);
        Milestone existing = Milestone.Create(
            MilestoneCommandData.Name,
            MilestoneCommandData.Description,
            null,
            null,
            project,
            admin,
            MilestoneCommandData.UtcNow).Value;
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(MilestoneCommandData.UtcNow);

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet([existing]);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.Milestones.Returns(milestonesMock);

        CreateMilestoneCommand command = MilestoneCommandData.GetCreateCommand(project.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnDomainError_When_NameIsEmpty()
    {
        // Arrange
        User admin = MilestoneCommandData.GetAdmin();
        Project project = MilestoneCommandData.GetProject(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(MilestoneCommandData.UtcNow);

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Milestone>());
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.Milestones.Returns(milestonesMock);

        var command = new CreateMilestoneCommand(project.Id, string.Empty, null, null, null);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(MilestoneErrors.NameRequired);
    }

    [Fact]
    public async Task Should_ReturnDomainError_When_TargetEndDateBeforeTargetStartDate()
    {
        // Arrange
        User admin = MilestoneCommandData.GetAdmin();
        Project project = MilestoneCommandData.GetProject(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(MilestoneCommandData.UtcNow);

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Milestone>());
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.Milestones.Returns(milestonesMock);

        var laterDate = new DateOnly(2026, 2, 15);
        var earlierDate = new DateOnly(2026, 1, 15);
        var command = new CreateMilestoneCommand(
            project.Id,
            MilestoneCommandData.Name,
            MilestoneCommandData.Description,
            laterDate,
            earlierDate);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(MilestoneErrors.InvalidDateRange);
    }
}
