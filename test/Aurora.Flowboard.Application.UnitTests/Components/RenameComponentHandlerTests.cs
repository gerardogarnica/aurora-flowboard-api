namespace Aurora.Flowboard.Application.UnitTests.Components;

public sealed class RenameComponentHandlerTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUserContext _userContext;
    private readonly RenameComponentHandler _handler;

    public RenameComponentHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _userContext = Substitute.For<IUserContext>();
        _handler = new RenameComponentHandler(_dbContext, _dateTimeProvider, _userContext);
    }

    [Fact]
    public async Task Should_RenameComponent_When_CommandIsValid()
    {
        // Arrange
        User admin = ComponentCommandData.GetAdmin();
        ComponentCommandData.GetProjectWithComponent(admin, out Component component);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(ComponentCommandData.UtcNow);

        DbSet<Component> componentsMock = MockDbSetHelper.CreateMockDbSet([component]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.Components.Returns(componentsMock);
        _dbContext.Users.Returns(usersMock);

        RenameComponentCommand command = ComponentCommandData.GetRenameCommand(component.Id);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        component.Name.Should().Be(ComponentCommandData.RenamedTo);
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
        _dbContext.Components.Returns(componentsMock);
        _dbContext.Users.Returns(usersMock);

        RenameComponentCommand command = ComponentCommandData.GetRenameCommand(component.Id);

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

        RenameComponentCommand command = ComponentCommandData.GetRenameCommand(Guid.NewGuid());

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

        RenameComponentCommand command = ComponentCommandData.GetRenameCommand(Guid.NewGuid());

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
        _dbContext.Components.Returns(componentsMock);
        _dbContext.Users.Returns(usersMock);

        RenameComponentCommand command = ComponentCommandData.GetRenameCommand(component.Id);

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
        _dbContext.Components.Returns(componentsMock);
        _dbContext.Users.Returns(usersMock);

        RenameComponentCommand command = ComponentCommandData.GetRenameCommand(component.Id);

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
        _dbContext.Components.Returns(componentsMock);
        _dbContext.Users.Returns(usersMock);

        RenameComponentCommand command = ComponentCommandData.GetRenameCommand(component.Id);

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
        _dbContext.Components.Returns(componentsMock);
        _dbContext.Users.Returns(usersMock);

        RenameComponentCommand command = ComponentCommandData.GetRenameCommand(component.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnDuplicateComponentNameError_When_RenamingToExistingSiblingName()
    {
        // Arrange
        User admin = ComponentCommandData.GetAdmin();
        Project project = ComponentCommandData.GetProjectWithComponent(admin, out Component componentToRename);
        Component sibling = Component.Create(ComponentCommandData.RenamedTo, project, admin, ComponentCommandData.UtcNow).Value;
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(ComponentCommandData.UtcNow);

        DbSet<Component> componentsMock = MockDbSetHelper.CreateMockDbSet([componentToRename, sibling]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.Components.Returns(componentsMock);
        _dbContext.Users.Returns(usersMock);

        RenameComponentCommand command = ComponentCommandData.GetRenameCommand(componentToRename.Id);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ComponentErrors.DuplicateName);
    }

    [Fact]
    public async Task Should_NotPersist_When_RenamingToExistingSiblingName()
    {
        // Arrange
        User admin = ComponentCommandData.GetAdmin();
        Project project = ComponentCommandData.GetProjectWithComponent(admin, out Component componentToRename);
        Component sibling = Component.Create(ComponentCommandData.RenamedTo, project, admin, ComponentCommandData.UtcNow).Value;
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(ComponentCommandData.UtcNow);

        DbSet<Component> componentsMock = MockDbSetHelper.CreateMockDbSet([componentToRename, sibling]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.Components.Returns(componentsMock);
        _dbContext.Users.Returns(usersMock);

        RenameComponentCommand command = ComponentCommandData.GetRenameCommand(componentToRename.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnDomainError_When_NameIsEmpty()
    {
        // Arrange
        User admin = ComponentCommandData.GetAdmin();
        ComponentCommandData.GetProjectWithComponent(admin, out Component component);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(ComponentCommandData.UtcNow);

        DbSet<Component> componentsMock = MockDbSetHelper.CreateMockDbSet([component]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.Components.Returns(componentsMock);
        _dbContext.Users.Returns(usersMock);

        var command = new RenameComponentCommand(component.Id, string.Empty);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ComponentErrors.NameRequired);
    }

    [Fact]
    public async Task Should_NotPersist_When_NameIsEmpty()
    {
        // Arrange
        User admin = ComponentCommandData.GetAdmin();
        ComponentCommandData.GetProjectWithComponent(admin, out Component component);
        _userContext.UserId.Returns(admin.Id);
        _dateTimeProvider.UtcNow.Returns(ComponentCommandData.UtcNow);

        DbSet<Component> componentsMock = MockDbSetHelper.CreateMockDbSet([component]);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([admin]);
        _dbContext.Components.Returns(componentsMock);
        _dbContext.Users.Returns(usersMock);

        var command = new RenameComponentCommand(component.Id, string.Empty);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
