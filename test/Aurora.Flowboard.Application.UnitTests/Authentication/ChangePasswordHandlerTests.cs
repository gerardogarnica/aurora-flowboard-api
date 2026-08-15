using Aurora.Flowboard.Application.Authentication;
using Aurora.Flowboard.Application.Authentication.ChangePassword;

namespace Aurora.Flowboard.Application.UnitTests.Authentication;

public sealed class ChangePasswordHandlerTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUserContext _userContext;
    private readonly ChangePasswordHandler _handler;

    public ChangePasswordHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _userContext = Substitute.For<IUserContext>();

        _handler = new ChangePasswordHandler(
            _dbContext,
            _passwordHasher,
            _dateTimeProvider,
            _userContext);

        _dateTimeProvider.UtcNow.Returns(ChangePasswordCommandData.UtcNow);
        _passwordHasher
            .HashPassword(ChangePasswordCommandData.NewPlainPassword)
            .Returns(ChangePasswordCommandData.NewPasswordHash);
    }

    [Fact]
    public async Task Should_ChangePassword_When_CredentialsAreValid()
    {
        // Arrange
        User user = ChangePasswordCommandData.GetUser();
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        _dbContext.Users.Returns(usersMock);
        _userContext.UserId.Returns(user.Id);

        _passwordHasher
            .VerifyHashedPassword(ChangePasswordCommandData.CurrentPasswordHash, ChangePasswordCommandData.CurrentPlainPassword)
            .Returns(true);

        ChangePasswordCommand command = ChangePasswordCommandData.GetCommand();

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        _passwordHasher.Received(1).HashPassword(ChangePasswordCommandData.NewPlainPassword);
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_RevokeAllActiveTokens_When_PasswordChanged()
    {
        // Arrange
        User user = ChangePasswordCommandData.GetUser();
        user.IssueToken("access-1", "refresh-1", ChangePasswordCommandData.UtcNow.AddMinutes(60), ChangePasswordCommandData.UtcNow.AddDays(7), ChangePasswordCommandData.UtcNow);
        user.IssueToken("access-2", "refresh-2", ChangePasswordCommandData.UtcNow.AddMinutes(60), ChangePasswordCommandData.UtcNow.AddDays(7), ChangePasswordCommandData.UtcNow);

        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        _dbContext.Users.Returns(usersMock);
        _userContext.UserId.Returns(user.Id);

        _passwordHasher
            .VerifyHashedPassword(ChangePasswordCommandData.CurrentPasswordHash, ChangePasswordCommandData.CurrentPlainPassword)
            .Returns(true);

        ChangePasswordCommand command = ChangePasswordCommandData.GetCommand();

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        user.Tokens.Should().OnlyContain(t => t.IsRevoked);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_UserDoesNotExist()
    {
        // Arrange
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<User>());
        _dbContext.Users.Returns(usersMock);

        ChangePasswordCommand command = ChangePasswordCommandData.GetCommand();

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(UserErrors.NotFound);
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnInactive_When_UserIsInactive()
    {
        // Arrange
        User user = ChangePasswordCommandData.GetUser();
        user.Deactivate(ChangePasswordCommandData.UtcNow);

        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        _dbContext.Users.Returns(usersMock);
        _userContext.UserId.Returns(user.Id);

        ChangePasswordCommand command = ChangePasswordCommandData.GetCommand();

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(UserErrors.Inactive);
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnInvalidCredentials_When_CurrentPasswordIsWrong()
    {
        // Arrange
        User user = ChangePasswordCommandData.GetUser();
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        _dbContext.Users.Returns(usersMock);
        _userContext.UserId.Returns(user.Id);

        _passwordHasher
            .VerifyHashedPassword(ChangePasswordCommandData.CurrentPasswordHash, Arg.Any<string>())
            .Returns(false);

        ChangePasswordCommand command = ChangePasswordCommandData.GetCommand(currentPassword: "wrong-password");

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(AuthenticationErrors.InvalidCredentials);
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnNewPasswordMustDiffer_When_NewPasswordEqualsCurrentPassword()
    {
        // Arrange
        User user = ChangePasswordCommandData.GetUser();
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        _dbContext.Users.Returns(usersMock);
        _userContext.UserId.Returns(user.Id);

        _passwordHasher
            .VerifyHashedPassword(ChangePasswordCommandData.CurrentPasswordHash, ChangePasswordCommandData.CurrentPlainPassword)
            .Returns(true);

        ChangePasswordCommand command = ChangePasswordCommandData.GetCommand(
            newPassword: ChangePasswordCommandData.CurrentPlainPassword);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(AuthenticationErrors.NewPasswordMustDiffer);
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
