using Aurora.Flowboard.Application.UnitTests.WorkItems;

namespace Aurora.Flowboard.Application.UnitTests.Components;

public sealed class RetireComponentHandlerTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUserContext _userContext;
    private readonly RetireComponentHandler _handler;

    public RetireComponentHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _userContext = Substitute.For<IUserContext>();
        _handler = new RetireComponentHandler(_dbContext, _dateTimeProvider, _userContext);
    }

    [Fact]
    public async Task Should_RetireComponent_When_CommandIsValid()
    {
        // Arrange
        User admin = ComponentCommandData.GetAdmin();
        ComponentCommandData.GetProjectWithComponent(admin, out Component component);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(ComponentCommandData.UtcNow);

        DbSet<Component> componentsMock = MockDbSetHelper.CreateMockDbSet([component]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<WorkItem>());
        _dbContext.Components.Returns(componentsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.WorkItems.Returns(workItemsMock);

        RetireComponentCommand command = ComponentCommandData.GetRetireCommand(component.Id);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        component.Status.Should().Be(ComponentStatus.Retired);
    }

    [Fact]
    public async Task Should_PersistComponent_When_CommandIsValid()
    {
        // Arrange
        User admin = ComponentCommandData.GetAdmin();
        ComponentCommandData.GetProjectWithComponent(admin, out Component component);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(ComponentCommandData.UtcNow);

        DbSet<Component> componentsMock = MockDbSetHelper.CreateMockDbSet([component]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<WorkItem>());
        _dbContext.Components.Returns(componentsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.WorkItems.Returns(workItemsMock);

        RetireComponentCommand command = ComponentCommandData.GetRetireCommand(component.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnComponentNotFoundError_When_ComponentDoesNotExist()
    {
        // Arrange
        DbSet<Component> componentsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Component>());
        _dbContext.Components.Returns(componentsMock);

        RetireComponentCommand command = ComponentCommandData.GetRetireCommand(Guid.NewGuid());

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ComponentErrors.NotFound);
    }

    [Fact]
    public async Task Should_NotPersist_When_ComponentDoesNotExist()
    {
        // Arrange
        DbSet<Component> componentsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Component>());
        _dbContext.Components.Returns(componentsMock);

        RetireComponentCommand command = ComponentCommandData.GetRetireCommand(Guid.NewGuid());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnUserNotFoundError_When_CallerDoesNotExist()
    {
        // Arrange
        User admin = ComponentCommandData.GetAdmin();
        ComponentCommandData.GetProjectWithComponent(admin, out Component component);
        _userContext.UserId.Returns(Guid.NewGuid());

        DbSet<Component> componentsMock = MockDbSetHelper.CreateMockDbSet([component]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<WorkItem>());
        _dbContext.Components.Returns(componentsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.WorkItems.Returns(workItemsMock);

        RetireComponentCommand command = ComponentCommandData.GetRetireCommand(component.Id);

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
        User admin = ComponentCommandData.GetAdmin();
        ComponentCommandData.GetProjectWithComponent(admin, out Component component);
        _userContext.UserId.Returns(Guid.NewGuid());

        DbSet<Component> componentsMock = MockDbSetHelper.CreateMockDbSet([component]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<WorkItem>());
        _dbContext.Components.Returns(componentsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.WorkItems.Returns(workItemsMock);

        RetireComponentCommand command = ComponentCommandData.GetRetireCommand(component.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnDomainError_When_UserIsNotProjectAdmin()
    {
        // Arrange
        User admin = ComponentCommandData.GetAdmin();
        User nonAdmin = ComponentCommandData.GetNonAdmin();
        ComponentCommandData.GetProjectWithComponent(admin, out Component component);
        _userContext.UserId.Returns(nonAdmin.Id);
        _dateTimeProvider.UtcNow.Returns(ComponentCommandData.UtcNow);

        DbSet<Component> componentsMock = MockDbSetHelper.CreateMockDbSet([component]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([nonAdmin]);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<WorkItem>());
        _dbContext.Components.Returns(componentsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.WorkItems.Returns(workItemsMock);

        RetireComponentCommand command = ComponentCommandData.GetRetireCommand(component.Id);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ComponentErrors.OnlyAdminCanManageComponent);
    }

    [Fact]
    public async Task Should_NotPersist_When_UserIsNotProjectAdmin()
    {
        // Arrange
        User admin = ComponentCommandData.GetAdmin();
        User nonAdmin = ComponentCommandData.GetNonAdmin();
        ComponentCommandData.GetProjectWithComponent(admin, out Component component);
        _userContext.UserId.Returns(nonAdmin.Id);
        _dateTimeProvider.UtcNow.Returns(ComponentCommandData.UtcNow);

        DbSet<Component> componentsMock = MockDbSetHelper.CreateMockDbSet([component]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([nonAdmin]);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<WorkItem>());
        _dbContext.Components.Returns(componentsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.WorkItems.Returns(workItemsMock);

        RetireComponentCommand command = ComponentCommandData.GetRetireCommand(component.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnDomainError_When_ComponentAlreadyRetired()
    {
        // Arrange
        User admin = ComponentCommandData.GetAdmin();
        ComponentCommandData.GetProjectWithComponent(admin, out Component component);
        component.Retire(admin, 0, ComponentCommandData.UtcNow);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(ComponentCommandData.UtcNow);

        DbSet<Component> componentsMock = MockDbSetHelper.CreateMockDbSet([component]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<WorkItem>());
        _dbContext.Components.Returns(componentsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.WorkItems.Returns(workItemsMock);

        RetireComponentCommand command = ComponentCommandData.GetRetireCommand(component.Id);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ComponentErrors.AlreadyRetired);
    }

    [Fact]
    public async Task Should_NotPersist_When_ComponentAlreadyRetired()
    {
        // Arrange
        User admin = ComponentCommandData.GetAdmin();
        ComponentCommandData.GetProjectWithComponent(admin, out Component component);
        component.Retire(admin, 0, ComponentCommandData.UtcNow);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(ComponentCommandData.UtcNow);

        DbSet<Component> componentsMock = MockDbSetHelper.CreateMockDbSet([component]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<WorkItem>());
        _dbContext.Components.Returns(componentsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.WorkItems.Returns(workItemsMock);

        RetireComponentCommand command = ComponentCommandData.GetRetireCommand(component.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnCannotRetireWithOpenWorkItemsError_When_ComponentHasOpenWorkItems()
    {
        // Arrange
        User admin = ComponentCommandData.GetAdmin();
        Project project = WorkItemCommandData.GetActiveProjectWithFlow(admin);
        Component component = Component.Create(ComponentCommandData.Name, project, admin, ComponentCommandData.UtcNow).Value;

        WorkItem workItem = WorkItem.Create(
            WorkItemCommandData.Title,
            null,
            WorkItemType.Story,
            Priority.Medium,
            project,
            admin,
            null,
            null,
            ComponentCommandData.UtcNow,
            component: component).Value;

        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(ComponentCommandData.UtcNow);

        DbSet<Component> componentsMock = MockDbSetHelper.CreateMockDbSet([component]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        _dbContext.Components.Returns(componentsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.WorkItems.Returns(workItemsMock);

        RetireComponentCommand command = ComponentCommandData.GetRetireCommand(component.Id);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ComponentErrors.CannotRetireWithOpenWorkItems);
    }

    [Fact]
    public async Task Should_NotPersist_When_ComponentHasOpenWorkItems()
    {
        // Arrange
        User admin = ComponentCommandData.GetAdmin();
        Project project = WorkItemCommandData.GetActiveProjectWithFlow(admin);
        Component component = Component.Create(ComponentCommandData.Name, project, admin, ComponentCommandData.UtcNow).Value;

        WorkItem workItem = WorkItem.Create(
            WorkItemCommandData.Title,
            null,
            WorkItemType.Story,
            Priority.Medium,
            project,
            admin,
            null,
            null,
            ComponentCommandData.UtcNow,
            component: component).Value;

        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(ComponentCommandData.UtcNow);

        DbSet<Component> componentsMock = MockDbSetHelper.CreateMockDbSet([component]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        DbSet<WorkItem> workItemsMock = MockDbSetHelper.CreateMockDbSet([workItem]);
        _dbContext.Components.Returns(componentsMock);
        _dbContext.Users.Returns(usersMock);
        _dbContext.WorkItems.Returns(workItemsMock);

        RetireComponentCommand command = ComponentCommandData.GetRetireCommand(component.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
