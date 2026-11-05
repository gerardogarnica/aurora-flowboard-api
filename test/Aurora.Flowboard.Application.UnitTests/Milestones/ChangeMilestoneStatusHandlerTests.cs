using System.Data;
using Aurora.Flowboard.Application.UnitTests.WorkItems;
using Microsoft.EntityFrameworkCore.Storage;

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

        _dbContext.Seed(c => c.Milestones, milestone);
        _dbContext.Seed(c => c.Users, admin);
        _dbContext.Seed(c => c.WorkItems);

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

        _dbContext.Seed(c => c.Milestones, milestone);
        _dbContext.Seed(c => c.Users, admin);
        _dbContext.Seed(c => c.WorkItems);

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
        _dbContext.Seed(c => c.Milestones);

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
        _dbContext.Seed(c => c.Milestones);

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

        _dbContext.Seed(c => c.Milestones, milestone);
        _dbContext.Seed(c => c.Users);

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

        _dbContext.Seed(c => c.Milestones, milestone);
        _dbContext.Seed(c => c.Users);

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

        _dbContext.Seed(c => c.Milestones, milestone);
        _dbContext.Seed(c => c.Users, nonAdmin);
        _dbContext.Seed(c => c.WorkItems);

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

        _dbContext.Seed(c => c.Milestones, milestone);
        _dbContext.Seed(c => c.Users, nonAdmin);
        _dbContext.Seed(c => c.WorkItems);

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

        _dbContext.Seed(c => c.Milestones, milestone);
        _dbContext.Seed(c => c.Users, admin);
        _dbContext.Seed(c => c.WorkItems);

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

        _dbContext.Seed(c => c.Milestones, milestone);
        _dbContext.Seed(c => c.Users, admin);
        _dbContext.Seed(c => c.WorkItems);

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
            MilestoneCommandData.MilestoneColor,
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

        _dbContext.Seed(c => c.Milestones, milestone);
        _dbContext.Seed(c => c.Users, admin);
        _dbContext.Seed(c => c.WorkItems, workItem);

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
            MilestoneCommandData.MilestoneColor,
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

        _dbContext.Seed(c => c.Milestones, milestone);
        _dbContext.Seed(c => c.Users, admin);
        _dbContext.Seed(c => c.WorkItems, workItem);

        ChangeMilestoneStatusCommand command = MilestoneCommandData.GetChangeStatusCommand(milestone.Id, MilestoneStatus.Completed);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_CommitSerializableTransaction_When_CommandIsValid()
    {
        // Arrange
        User admin = MilestoneCommandData.GetAdmin();
        MilestoneCommandData.GetProjectWithMilestone(admin, out Milestone milestone);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(MilestoneCommandData.UtcNow);
        IDbContextTransaction transaction = SetupSerializableTransaction();

        _dbContext.Seed(c => c.Milestones, milestone);
        _dbContext.Seed(c => c.Users, admin);
        _dbContext.Seed(c => c.WorkItems);

        ChangeMilestoneStatusCommand command = MilestoneCommandData.GetChangeStatusCommand(milestone.Id, MilestoneStatus.Active);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_NotCommit_When_StatusTransitionIsInvalid()
    {
        // Arrange
        User admin = MilestoneCommandData.GetAdmin();
        MilestoneCommandData.GetProjectWithMilestone(admin, out Milestone milestone);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(MilestoneCommandData.UtcNow);
        IDbContextTransaction transaction = SetupSerializableTransaction();

        _dbContext.Seed(c => c.Milestones, milestone);
        _dbContext.Seed(c => c.Users, admin);
        _dbContext.Seed(c => c.WorkItems);

        ChangeMilestoneStatusCommand command = MilestoneCommandData.GetChangeStatusCommand(milestone.Id, MilestoneStatus.Completed);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_CloseMilestone_When_ItsWorkItemsAreClosed()
    {
        // Arrange
        User admin = MilestoneCommandData.GetAdmin();
        Project project = WorkItemCommandData.GetActiveProjectWithFlow(admin);
        Milestone milestone = CreateActiveMilestone(project, admin, MilestoneCommandData.Name);
        WorkItem completed = WorkItemCommandData.GetWorkItemInCategory(project, admin, FlowStateCategory.Completed, milestone: milestone);
        WorkItem cancelled = WorkItemCommandData.GetWorkItemInCategory(project, admin, FlowStateCategory.Cancelled, milestone: milestone);

        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(MilestoneCommandData.UtcNow);

        _dbContext.Seed(c => c.Milestones, milestone);
        _dbContext.Seed(c => c.Users, admin);
        _dbContext.Seed(c => c.WorkItems, completed, cancelled);

        ChangeMilestoneStatusCommand command = MilestoneCommandData.GetChangeStatusCommand(milestone.Id, MilestoneStatus.Completed);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        milestone.Status.Should().Be(MilestoneStatus.Completed);
    }

    [Fact]
    public async Task Should_CloseMilestone_When_OpenWorkItemsBelongToAnotherMilestone()
    {
        // Arrange
        User admin = MilestoneCommandData.GetAdmin();
        Project project = WorkItemCommandData.GetActiveProjectWithFlow(admin);
        Milestone milestone = CreateActiveMilestone(project, admin, MilestoneCommandData.Name);
        Milestone otherMilestone = CreateActiveMilestone(project, admin, MilestoneCommandData.UpdatedName);
        WorkItem openInOther = WorkItemCommandData.GetWorkItemInCategory(project, admin, FlowStateCategory.Active, milestone: otherMilestone);

        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(MilestoneCommandData.UtcNow);

        _dbContext.Seed(c => c.Milestones, milestone, otherMilestone);
        _dbContext.Seed(c => c.Users, admin);
        _dbContext.Seed(c => c.WorkItems, openInOther);

        ChangeMilestoneStatusCommand command = MilestoneCommandData.GetChangeStatusCommand(milestone.Id, MilestoneStatus.Completed);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        milestone.Status.Should().Be(MilestoneStatus.Completed);
    }

    private static Milestone CreateActiveMilestone(Project project, User admin, string name)
    {
        Milestone milestone = Milestone.Create(
            name,
            MilestoneCommandData.Description,
            MilestoneCommandData.MilestoneColor,
            null,
            null,
            project,
            admin,
            MilestoneCommandData.UtcNow).Value;
        milestone.ChangeStatus(MilestoneStatus.Active, admin, 0, MilestoneCommandData.UtcNow);

        return milestone;
    }

    private IDbContextTransaction SetupSerializableTransaction()
    {
        IDbContextTransaction transaction = Substitute.For<IDbContextTransaction>();
        _dbContext.BeginTransactionAsync(Arg.Is(IsolationLevel.Serializable), Arg.Any<CancellationToken>())
            .Returns(transaction);
        return transaction;
    }
}
