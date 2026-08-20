using Aurora.Flowboard.Application.Users.ChangeRole;

namespace Aurora.Flowboard.Application.UnitTests.Users;

public sealed class ChangeUserRoleHandlerTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ChangeUserRoleHandler _handler;

    public ChangeUserRoleHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _handler = new ChangeUserRoleHandler(_dbContext, _dateTimeProvider);
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_UserHasNoRoleYet()
    {
        // Arrange
        User user = CreateUserCommandData.GetExistingUser();
        _dateTimeProvider.UtcNow.Returns(CreateUserCommandData.UtcNow);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        _dbContext.Users.Returns(usersMock);

        var command = new ChangeUserRoleCommand(user.Id, Role.Member.Name);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        user.Roles.Should().ContainSingle(r => r.Name == Role.Member.Name);
    }

    [Fact]
    public async Task Should_ReplaceExistingRole_When_UserAlreadyHasADifferentRole()
    {
        // Arrange
        User user = CreateUserCommandData.GetExistingUser();
        user.AssignRole(Role.Member);
        _dateTimeProvider.UtcNow.Returns(CreateUserCommandData.UtcNow);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        _dbContext.Users.Returns(usersMock);

        var command = new ChangeUserRoleCommand(user.Id, Role.Administrator.Name);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        user.Roles.Should().ContainSingle(r => r.Name == Role.Administrator.Name);
    }

    [Fact]
    public async Task Should_PersistChanges_When_CommandIsValid()
    {
        // Arrange
        User user = CreateUserCommandData.GetExistingUser();
        _dateTimeProvider.UtcNow.Returns(CreateUserCommandData.UtcNow);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        _dbContext.Users.Returns(usersMock);

        var command = new ChangeUserRoleCommand(user.Id, Role.Member.Name);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnUserNotFoundError_When_UserDoesNotExist()
    {
        // Arrange
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<User>());
        _dbContext.Users.Returns(usersMock);

        var command = new ChangeUserRoleCommand(Guid.NewGuid(), Role.Member.Name);

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
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<User>());
        _dbContext.Users.Returns(usersMock);

        var command = new ChangeUserRoleCommand(Guid.NewGuid(), Role.Member.Name);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnRoleNotFoundError_When_RoleNameIsUnknown()
    {
        // Arrange
        User user = CreateUserCommandData.GetExistingUser();
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        _dbContext.Users.Returns(usersMock);

        var command = new ChangeUserRoleCommand(user.Id, "NotARole");

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(RoleErrors.NotFound);
    }

    [Fact]
    public async Task Should_ReturnRoleAlreadyAssignedError_When_NewRoleEqualsCurrentRole()
    {
        // Arrange
        User user = CreateUserCommandData.GetExistingUser();
        user.AssignRole(Role.Member);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        _dbContext.Users.Returns(usersMock);

        var command = new ChangeUserRoleCommand(user.Id, Role.Member.Name);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(UserErrors.RoleAlreadyAssigned);
    }

    [Fact]
    public async Task Should_NotPersist_When_NewRoleEqualsCurrentRole()
    {
        // Arrange
        User user = CreateUserCommandData.GetExistingUser();
        user.AssignRole(Role.Member);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        _dbContext.Users.Returns(usersMock);

        var command = new ChangeUserRoleCommand(user.Id, Role.Member.Name);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
