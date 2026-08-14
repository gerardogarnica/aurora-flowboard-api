using Aurora.Flowboard.Application.UnitTests.WorkItems;

namespace Aurora.Flowboard.Application.UnitTests.Milestones;

public sealed class ChangeMilestoneStatusHandlerTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUserContext _userContext;
    private readonly ChangeMilestoneStatusHandler _handler;

    public ChangeMilestoneStatusHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _userContext = Substitute.For<IUserContext>();
        _handler = new ChangeMilestoneStatusHandler(_dbContext, _dateTimeProvider, _userContext);
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_CommandIsValid()
    {
        // Arrange
        User admin = MilestoneCommandData.GetAdmin();
        MilestoneCommandData.GetProjectWithMilestone(admin, out Milestone milestone);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(MilestoneCommandData.UtcNow);

        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet([milestone]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<WorkItem>());
        _dbContext.Milestones.Returns(milestonesMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.WorkItems.Returns(workItemsMock);

        ChangeMilestoneStatusCommand command = MilestoneCommandData.GetChangeStatusCommand(milestone.Id, MilestoneStatus.Active);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        milestone.Status.Should().Be(MilestoneStatus.Active);
    }

    [Fact]
    public async Task Should_PersistChanges_When_CommandIsValid()
    {
        // Arrange
        User admin = MilestoneCommandData.GetAdmin();
        MilestoneCommandData.GetProjectWithMilestone(admin, out Milestone milestone);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(MilestoneCommandData.UtcNow);

        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet([milestone]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<WorkItem>());
        _dbContext.Milestones.Returns(milestonesMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.WorkItems.Returns(workItemsMock);

        ChangeMilestoneStatusCommand command = MilestoneCommandData.GetChangeStatusCommand(milestone.Id, MilestoneStatus.Active);

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

        ChangeMilestoneStatusCommand command = MilestoneCommandData.GetChangeStatusCommand(Guid.NewGuid(), MilestoneStatus.Active);

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

        ChangeMilestoneStatusCommand command = MilestoneCommandData.GetChangeStatusCommand(Guid.NewGuid(), MilestoneStatus.Active);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnUserNotFoundError_When_UserDoesNotExist()
    {
        // Arrange
        User admin = MilestoneCommandData.GetAdmin();
        MilestoneCommandData.GetProjectWithMilestone(admin, out Milestone milestone);
        _userContext.UserId.Returns(Guid.NewGuid());

        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet([milestone]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<User>());
        _dbContext.Milestones.Returns(milestonesMock);
        _dbContext.Users.Returns(usersMock);

        ChangeMilestoneStatusCommand command = MilestoneCommandData.GetChangeStatusCommand(milestone.Id, MilestoneStatus.Active);

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
        User admin = MilestoneCommandData.GetAdmin();
        MilestoneCommandData.GetProjectWithMilestone(admin, out Milestone milestone);
        _userContext.UserId.Returns(Guid.NewGuid());

        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet([milestone]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<User>());
        _dbContext.Milestones.Returns(milestonesMock);
        _dbContext.Users.Returns(usersMock);

        ChangeMilestoneStatusCommand command = MilestoneCommandData.GetChangeStatusCommand(milestone.Id, MilestoneStatus.Active);

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
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<WorkItem>());
        _dbContext.Milestones.Returns(milestonesMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.WorkItems.Returns(workItemsMock);

        ChangeMilestoneStatusCommand command = MilestoneCommandData.GetChangeStatusCommand(milestone.Id, MilestoneStatus.Active);

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
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<WorkItem>());
        _dbContext.Milestones.Returns(milestonesMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.WorkItems.Returns(workItemsMock);

        ChangeMilestoneStatusCommand command = MilestoneCommandData.GetChangeStatusCommand(milestone.Id, MilestoneStatus.Active);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnDomainError_When_StatusTransitionIsInvalid()
    {
        // Arrange
        User admin = MilestoneCommandData.GetAdmin();
        MilestoneCommandData.GetProjectWithMilestone(admin, out Milestone milestone);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(MilestoneCommandData.UtcNow);

        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet([milestone]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<WorkItem>());
        _dbContext.Milestones.Returns(milestonesMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.WorkItems.Returns(workItemsMock);

        ChangeMilestoneStatusCommand command = MilestoneCommandData.GetChangeStatusCommand(milestone.Id, MilestoneStatus.Completed);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(MilestoneErrors.InvalidStatusTransition);
    }

    [Fact]
    public async Task Should_NotPersist_When_StatusTransitionIsInvalid()
    {
        // Arrange
        User admin = MilestoneCommandData.GetAdmin();
        MilestoneCommandData.GetProjectWithMilestone(admin, out Milestone milestone);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(MilestoneCommandData.UtcNow);

        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet([milestone]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<WorkItem>());
        _dbContext.Milestones.Returns(milestonesMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.WorkItems.Returns(workItemsMock);

        ChangeMilestoneStatusCommand command = MilestoneCommandData.GetChangeStatusCommand(milestone.Id, MilestoneStatus.Completed);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnCannotCloseWithOpenWorkItemsError_When_MilestoneHasOpenWorkItems()
    {
        // Arrange
        User admin = MilestoneCommandData.GetAdmin();
        Project project = WorkItemCommandData.GetActiveProjectWithFlow(admin);
        Milestone milestone = Milestone.Create(
            MilestoneCommandData.Name,
            MilestoneCommandData.Description,
            null,
            null,
            project,
            admin,
            MilestoneCommandData.UtcNow).Value;
        milestone.ChangeStatus(MilestoneStatus.Active, admin, 0, MilestoneCommandData.UtcNow);

        WorkItem workItem = WorkItem.Create(
            WorkItemCommandData.Title,
            null,
            WorkItemType.Story,
            Priority.Medium,
            project,
            admin,
            null,
            null,
            MilestoneCommandData.UtcNow,
            milestone: milestone).Value;

        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(MilestoneCommandData.UtcNow);

        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet([milestone]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        _dbContext.Milestones.Returns(milestonesMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.WorkItems.Returns(workItemsMock);

        ChangeMilestoneStatusCommand command = MilestoneCommandData.GetChangeStatusCommand(milestone.Id, MilestoneStatus.Completed);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(MilestoneErrors.CannotCloseWithOpenWorkItems);
    }

    [Fact]
    public async Task Should_NotPersist_When_MilestoneHasOpenWorkItems()
    {
        // Arrange
        User admin = MilestoneCommandData.GetAdmin();
        Project project = WorkItemCommandData.GetActiveProjectWithFlow(admin);
        Milestone milestone = Milestone.Create(
            MilestoneCommandData.Name,
            MilestoneCommandData.Description,
            null,
            null,
            project,
            admin,
            MilestoneCommandData.UtcNow).Value;
        milestone.ChangeStatus(MilestoneStatus.Active, admin, 0, MilestoneCommandData.UtcNow);

        WorkItem workItem = WorkItem.Create(
            WorkItemCommandData.Title,
            null,
            WorkItemType.Story,
            Priority.Medium,
            project,
            admin,
            null,
            null,
            MilestoneCommandData.UtcNow,
            milestone: milestone).Value;

        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(MilestoneCommandData.UtcNow);

        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet([milestone]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        _dbContext.Milestones.Returns(milestonesMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.WorkItems.Returns(workItemsMock);

        ChangeMilestoneStatusCommand command = MilestoneCommandData.GetChangeStatusCommand(milestone.Id, MilestoneStatus.Completed);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
