using Aurora.Flowboard.Application.Users.CreateUser;

namespace Aurora.Flowboard.Application.UnitTests.Users;

public sealed class CreateUserHandlerTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly CreateUserHandler _handler;

    public CreateUserHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();

        _handler = new CreateUserHandler(_dbContext, _passwordHasher, _dateTimeProvider);

        _dateTimeProvider.UtcNow.Returns(CreateUserCommandData.UtcNow);
        _passwordHasher
            .HashPassword(CreateUserCommandData.PlainPassword)
            .Returns(CreateUserCommandData.PasswordHash);

        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<User>());
        _dbContext.Users.Returns(usersMock);
    }

    [Fact]
    public async Task Should_ReturnUserId_When_CommandIsValid()
    {
        // Arrange
        CreateUserCommand command = CreateUserCommandData.GetCreateCommand();

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Should_PersistUser_When_CommandIsValid()
    {
        // Arrange
        CreateUserCommand command = CreateUserCommandData.GetCreateCommand();

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _dbContext.Users.Received(1).Add(Arg.Is<User>(u =>
            u.Id == result.Value &&
            u.Email.Value == CreateUserCommandData.EmailAddress &&
            u.FullName == "Jane Doe" &&
            u.IsActive &&
            u.CreatedOnUtc == CreateUserCommandData.UtcNow));
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_HashPassword_Before_Persisting()
    {
        // Arrange
        CreateUserCommand command = CreateUserCommandData.GetCreateCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _passwordHasher.Received(1).HashPassword(CreateUserCommandData.PlainPassword);
    }

    [Fact]
    public async Task Should_DefaultToMemberRole_When_RoleIsNotProvided()
    {
        // Arrange
        CreateUserCommand command = CreateUserCommandData.GetCreateCommand(role: null);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        _dbContext.Users.Received(1).Add(Arg.Is<User>(u =>
            u.Roles.Count == 1 &&
            u.Roles.Any(r => r.Name == Role.Member.Name)));
    }

    [Fact]
    public async Task Should_AssignSpecifiedRole_When_RoleIsProvided()
    {
        // Arrange
        CreateUserCommand command = CreateUserCommandData.GetCreateCommand(role: Role.Administrator.Name);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        _dbContext.Users.Received(1).Add(Arg.Is<User>(u =>
            u.Roles.Count == 1 &&
            u.Roles.Any(r => r.Name == Role.Administrator.Name)));
    }

    [Fact]
    public async Task Should_ReturnRoleNotFound_When_RoleIsInvalid()
    {
        // Arrange
        CreateUserCommand command = CreateUserCommandData.GetCreateCommand(role: "SuperUser");

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(RoleErrors.NotFound);
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnEmailAlreadyInUse_When_EmailExists()
    {
        // Arrange
        User existing = CreateUserCommandData.GetExistingUser();
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([existing]);
        _dbContext.Users.Returns(usersMock);

        CreateUserCommand command = CreateUserCommandData.GetCreateCommand();

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(UserErrors.EmailAlreadyInUse);
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnValidationError_When_EmailFormatIsInvalid()
    {
        // Arrange
        var command = new CreateUserCommand(
            CreateUserCommandData.FirstName,
            CreateUserCommandData.LastName,
            "not-an-email",
            CreateUserCommandData.PlainPassword,
            null);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
