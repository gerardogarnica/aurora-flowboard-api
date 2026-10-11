using Aurora.Flowboard.Application.Authentication;
using Aurora.Flowboard.Application.Authentication.ChangePassword;
using NSubstitute.ExceptionExtensions;

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
        user.IssueToken("access-token-id-1", "refresh-token-hash-1", ChangePasswordCommandData.UtcNow.AddMinutes(60), ChangePasswordCommandData.UtcNow.AddDays(7), ChangePasswordCommandData.UtcNow);
        user.IssueToken("access-token-id-2", "refresh-token-hash-2", ChangePasswordCommandData.UtcNow.AddMinutes(60), ChangePasswordCommandData.UtcNow.AddDays(7), ChangePasswordCommandData.UtcNow);

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
    public async Task Should_ReturnInvalidCurrentPassword_When_CurrentPasswordIsWrong()
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

        // Assert — 400, not 401: the caller is authenticated, so a client must not treat this as an expired session.
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(AuthenticationErrors.InvalidCurrentPassword);
        result.Error.ErrorType.Should().Be(BaseErrorType.Validation);
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_NotRevokeExpiredTokens_When_PasswordChanged()
    {
        // Arrange
        User user = ChangePasswordCommandData.GetUser();
        UserToken expiredToken = user.IssueToken(
            "access-token-id-old",
            "refresh-token-hash-old",
            ChangePasswordCommandData.UtcNow.AddDays(-9),
            ChangePasswordCommandData.UtcNow.AddDays(-3),
            ChangePasswordCommandData.UtcNow.AddDays(-10)).Value;

        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        _dbContext.Users.Returns(usersMock);
        _userContext.UserId.Returns(user.Id);

        _passwordHasher
            .VerifyHashedPassword(ChangePasswordCommandData.CurrentPasswordHash, ChangePasswordCommandData.CurrentPlainPassword)
            .Returns(true);

        // Act
        Result result = await _handler.Handle(ChangePasswordCommandData.GetCommand(), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        expiredToken.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task Should_ReturnSessionChangedConcurrently_When_TokensChangeDuringSave()
    {
        // Arrange — a refresh rotated one of the tokens between the read and the save.
        User user = ChangePasswordCommandData.GetUser();
        user.IssueToken(
            "access-token-id-1",
            "refresh-token-hash-1",
            ChangePasswordCommandData.UtcNow.AddMinutes(60),
            ChangePasswordCommandData.UtcNow.AddDays(7),
            ChangePasswordCommandData.UtcNow);

        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        _dbContext.Users.Returns(usersMock);
        _userContext.UserId.Returns(user.Id);

        _passwordHasher
            .VerifyHashedPassword(ChangePasswordCommandData.CurrentPasswordHash, ChangePasswordCommandData.CurrentPlainPassword)
            .Returns(true);
        _dbContext.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new DbUpdateConcurrencyException("user_tokens row changed"));

        // Act
        Result result = await _handler.Handle(ChangePasswordCommandData.GetCommand(), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(AuthenticationErrors.SessionChangedConcurrently);
        result.Error.ErrorType.Should().Be(BaseErrorType.Conflict);
    }

}
