using Aurora.Flowboard.Application.UnitTests.WorkItems;

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
        User admin = ProjectCommandData.GetAdmin();
        Project project = ProjectCommandData.GetProject(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.Users, admin);
        _dbContext.Seed(c => c.WorkItems);

        ChangeProjectStatusCommand command = ProjectCommandData.GetChangeStatusCommand(project.Id);

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
        _dbContext.Seed(c => c.WorkItems);

        ChangeProjectStatusCommand command = ProjectCommandData.GetChangeStatusCommand(project.Id);

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

        ChangeProjectStatusCommand command = ProjectCommandData.GetChangeStatusCommand(Guid.NewGuid());

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

        ChangeProjectStatusCommand command = ProjectCommandData.GetChangeStatusCommand(Guid.NewGuid());

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

        ChangeProjectStatusCommand command = ProjectCommandData.GetChangeStatusCommand(project.Id);

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

        ChangeProjectStatusCommand command = ProjectCommandData.GetChangeStatusCommand(project.Id);

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
        _dbContext.Seed(c => c.WorkItems);

        ChangeProjectStatusCommand command = ProjectCommandData.GetChangeStatusCommand(project.Id);

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
        User admin = ProjectCommandData.GetAdmin();
        Project project = ProjectCommandData.GetProject(admin);
        User nonAdmin = ProjectCommandData.GetNonAdmin();
        _userContext.UserId.Returns(nonAdmin.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.Users, nonAdmin);
        _dbContext.Seed(c => c.WorkItems);

        ChangeProjectStatusCommand command = ProjectCommandData.GetChangeStatusCommand(project.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnDomainError_When_StatusTransitionIsInvalid()
    {
        // Arrange
        User admin = ProjectCommandData.GetAdmin();
        Project project = ProjectCommandData.GetArchivedProject(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.Users, admin);
        _dbContext.Seed(c => c.WorkItems);

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
        User admin = ProjectCommandData.GetAdmin();
        Project project = ProjectCommandData.GetArchivedProject(admin);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.Users, admin);
        _dbContext.Seed(c => c.WorkItems);

        var command = new ChangeProjectStatusCommand(project.Id, ProjectStatus.Active);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnCannotCloseWithOpenWorkItemsError_When_ProjectHasOpenWorkItems()
    {
        // Arrange
        User admin = ProjectCommandData.GetAdmin();
        Project project = ProjectCommandData.GetProject(admin);
        WorkItem open = WorkItemCommandData.GetWorkItemInCategory(project, admin, FlowStateCategory.Active);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.Users, admin);
        _dbContext.Seed(c => c.WorkItems, open);

        var command = new ChangeProjectStatusCommand(project.Id, ProjectStatus.Archived);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.CannotCloseWithOpenWorkItems);
        project.Status.Should().Be(ProjectStatus.Active);
    }

    [Fact]
    public async Task Should_NotPersist_When_ProjectHasOpenWorkItems()
    {
        // Arrange
        User admin = ProjectCommandData.GetAdmin();
        Project project = ProjectCommandData.GetProject(admin);
        WorkItem open = WorkItemCommandData.GetWorkItemInCategory(project, admin, FlowStateCategory.Active);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.Users, admin);
        _dbContext.Seed(c => c.WorkItems, open);

        var command = new ChangeProjectStatusCommand(project.Id, ProjectStatus.Archived);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_CloseProject_When_ItsWorkItemsAreClosed()
    {
        // Arrange
        User admin = ProjectCommandData.GetAdmin();
        Project project = ProjectCommandData.GetProject(admin);
        WorkItem completed = WorkItemCommandData.GetWorkItemInCategory(project, admin, FlowStateCategory.Completed);
        WorkItem cancelled = WorkItemCommandData.GetWorkItemInCategory(project, admin, FlowStateCategory.Cancelled);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.Users, admin);
        _dbContext.Seed(c => c.WorkItems, completed, cancelled);

        var command = new ChangeProjectStatusCommand(project.Id, ProjectStatus.Archived);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        project.Status.Should().Be(ProjectStatus.Archived);
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_CloseProject_When_OpenWorkItemsBelongToAnotherProject()
    {
        // Arrange
        User admin = ProjectCommandData.GetAdmin();
        Project project = ProjectCommandData.GetProject(admin);
        Project otherProject = ProjectCommandData.GetExistingProjectWithPrefix(admin, "OTH");
        WorkItem openInOther = WorkItemCommandData.GetWorkItemInCategory(otherProject, admin, FlowStateCategory.Active);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        _dbContext.Seed(c => c.Projects, project, otherProject);
        _dbContext.Seed(c => c.Users, admin);
        _dbContext.Seed(c => c.WorkItems, openInOther);

        var command = new ChangeProjectStatusCommand(project.Id, ProjectStatus.Archived);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        project.Status.Should().Be(ProjectStatus.Archived);
    }
}
