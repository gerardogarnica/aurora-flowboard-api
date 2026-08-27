namespace Aurora.Flowboard.Application.UnitTests.Milestones;

public sealed class UpdateMilestoneHandlerTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUserContext _userContext;
    private readonly UpdateMilestoneHandler _handler;

    public UpdateMilestoneHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _userContext = Substitute.For<IUserContext>();
        _handler = new UpdateMilestoneHandler(_dbContext, _dateTimeProvider, _userContext);
    }

    [Fact]
    public async Task Should_UpdateMilestone_When_CommandIsValid()
    {
        // Arrange
        User admin = MilestoneCommandData.GetAdmin();
        MilestoneCommandData.GetProjectWithMilestone(admin, out Milestone milestone);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(MilestoneCommandData.UtcNow);

        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet([milestone]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.Milestones.Returns(milestonesMock);
        _dbContext.Users.Returns(usersMock);

        UpdateMilestoneCommand command = MilestoneCommandData.GetUpdateCommand(milestone.Id);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        milestone.Name.Should().Be(MilestoneCommandData.UpdatedName);
        milestone.Description.Should().Be(MilestoneCommandData.UpdatedDescription);
    }

    [Fact]
    public async Task Should_PersistMilestone_When_CommandIsValid()
    {
        // Arrange
        User admin = MilestoneCommandData.GetAdmin();
        MilestoneCommandData.GetProjectWithMilestone(admin, out Milestone milestone);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(MilestoneCommandData.UtcNow);

        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet([milestone]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.Milestones.Returns(milestonesMock);
        _dbContext.Users.Returns(usersMock);

        UpdateMilestoneCommand command = MilestoneCommandData.GetUpdateCommand(milestone.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnMilestoneNotFoundError_When_MilestoneDoesNotExist()
    {
        // Arrange
        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Milestone>());
        _dbContext.Milestones.Returns(milestonesMock);

        UpdateMilestoneCommand command = MilestoneCommandData.GetUpdateCommand(Guid.NewGuid());

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(MilestoneErrors.NotFound);
    }

    [Fact]
    public async Task Should_NotPersist_When_MilestoneDoesNotExist()
    {
        // Arrange
        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Milestone>());
        _dbContext.Milestones.Returns(milestonesMock);

        UpdateMilestoneCommand command = MilestoneCommandData.GetUpdateCommand(Guid.NewGuid());

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
        MilestoneCommandData.GetProjectWithMilestone(admin, out Milestone milestone);
        _userContext.UserId.Returns(Guid.NewGuid());

        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet([milestone]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.Milestones.Returns(milestonesMock);
        _dbContext.Users.Returns(usersMock);

        UpdateMilestoneCommand command = MilestoneCommandData.GetUpdateCommand(milestone.Id);

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
        User admin = MilestoneCommandData.GetAdmin();
        MilestoneCommandData.GetProjectWithMilestone(admin, out Milestone milestone);
        _userContext.UserId.Returns(Guid.NewGuid());

        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet([milestone]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.Milestones.Returns(milestonesMock);
        _dbContext.Users.Returns(usersMock);

        UpdateMilestoneCommand command = MilestoneCommandData.GetUpdateCommand(milestone.Id);

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
        MilestoneCommandData.GetProjectWithMilestone(admin, out Milestone milestone);
        _userContext.UserId.Returns(nonAdmin.Id);
        _dateTimeProvider.UtcNow.Returns(MilestoneCommandData.UtcNow);

        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet([milestone]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([nonAdmin]);
        _dbContext.Milestones.Returns(milestonesMock);
        _dbContext.Users.Returns(usersMock);

        UpdateMilestoneCommand command = MilestoneCommandData.GetUpdateCommand(milestone.Id);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

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
        MilestoneCommandData.GetProjectWithMilestone(admin, out Milestone milestone);
        _userContext.UserId.Returns(nonAdmin.Id);
        _dateTimeProvider.UtcNow.Returns(MilestoneCommandData.UtcNow);

        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet([milestone]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([nonAdmin]);
        _dbContext.Milestones.Returns(milestonesMock);
        _dbContext.Users.Returns(usersMock);

        UpdateMilestoneCommand command = MilestoneCommandData.GetUpdateCommand(milestone.Id);

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
        MilestoneCommandData.GetProjectWithMilestone(admin, out Milestone milestone);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(MilestoneCommandData.UtcNow);

        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet([milestone]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.Milestones.Returns(milestonesMock);
        _dbContext.Users.Returns(usersMock);

        var command = new UpdateMilestoneCommand(milestone.Id, string.Empty, null, null, null);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(MilestoneErrors.NameRequired);
    }

    [Fact]
    public async Task Should_NotPersist_When_NameIsEmpty()
    {
        // Arrange
        User admin = MilestoneCommandData.GetAdmin();
        MilestoneCommandData.GetProjectWithMilestone(admin, out Milestone milestone);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(MilestoneCommandData.UtcNow);

        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet([milestone]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.Milestones.Returns(milestonesMock);
        _dbContext.Users.Returns(usersMock);

        var command = new UpdateMilestoneCommand(milestone.Id, string.Empty, null, null, null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnDomainError_When_MilestoneIsCompleted()
    {
        // Arrange
        User admin = MilestoneCommandData.GetAdmin();
        MilestoneCommandData.GetProjectWithMilestone(admin, out Milestone milestone);
        milestone.ChangeStatus(MilestoneStatus.Active, admin, 0, MilestoneCommandData.UtcNow);
        milestone.ChangeStatus(MilestoneStatus.Completed, admin, 0, MilestoneCommandData.UtcNow);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(MilestoneCommandData.UtcNow);

        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet([milestone]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.Milestones.Returns(milestonesMock);
        _dbContext.Users.Returns(usersMock);

        UpdateMilestoneCommand command = MilestoneCommandData.GetUpdateCommand(milestone.Id);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(MilestoneErrors.OperationNotAllowedInCurrentStatus);
    }
}
