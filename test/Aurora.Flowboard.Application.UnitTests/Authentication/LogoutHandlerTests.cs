using Aurora.Flowboard.Application.Authentication.Logout;

namespace Aurora.Flowboard.Application.UnitTests.Authentication;

public sealed class LogoutHandlerTests
{
    private const string HashedPassword = "hashed_password_123";
    private static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly IApplicationDbContext _dbContext;
    private readonly IUserContext _userContext;
    private readonly LogoutHandler _handler;

    public LogoutHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _userContext = Substitute.For<IUserContext>();

        _handler = new LogoutHandler(_dbContext, _userContext);
    }

    [Fact]
    public async Task Should_RevokeToken_When_RefreshTokenBelongsToCurrentUser()
    {
        // Arrange
        User user = CreateUser();
        UserToken token = IssueToken(user);

        DbSet<UserToken> userTokensMock = MockDbSetHelper.CreateMockDbSet([token]);
        _dbContext.UserTokens.Returns(userTokensMock);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        _dbContext.Users.Returns(usersMock);
        _userContext.UserId.Returns(user.Id);

        var command = new LogoutCommand(token.RefreshToken);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        token.IsRevoked.Should().BeTrue();
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_RefreshTokenIsUnknown()
    {
        // Arrange
        DbSet<UserToken> userTokensMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<UserToken>());
        _dbContext.UserTokens.Returns(userTokensMock);
        _userContext.UserId.Returns(Guid.NewGuid());

        var command = new LogoutCommand("unknown-refresh-token");

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_RefreshTokenBelongsToAnotherUser()
    {
        // Arrange
        User user = CreateUser();
        UserToken token = IssueToken(user);

        DbSet<UserToken> userTokensMock = MockDbSetHelper.CreateMockDbSet([token]);
        _dbContext.UserTokens.Returns(userTokensMock);
        _userContext.UserId.Returns(Guid.NewGuid());

        var command = new LogoutCommand(token.RefreshToken);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        token.IsRevoked.Should().BeFalse();
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_TokenIsAlreadyRevoked()
    {
        // Arrange
        User user = CreateUser();
        UserToken token = IssueToken(user);
        user.RevokeToken(token.UserTokenId);

        DbSet<UserToken> userTokensMock = MockDbSetHelper.CreateMockDbSet([token]);
        _dbContext.UserTokens.Returns(userTokensMock);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        _dbContext.Users.Returns(usersMock);
        _userContext.UserId.Returns(user.Id);

        var command = new LogoutCommand(token.RefreshToken);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        token.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Should_RevokeToken_When_UserIsInactive()
    {
        // Arrange
        User user = CreateUser();
        UserToken token = IssueToken(user);
        user.Deactivate(UtcNow);

        DbSet<UserToken> userTokensMock = MockDbSetHelper.CreateMockDbSet([token]);
        _dbContext.UserTokens.Returns(userTokensMock);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        _dbContext.Users.Returns(usersMock);
        _userContext.UserId.Returns(user.Id);

        var command = new LogoutCommand(token.RefreshToken);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        token.IsRevoked.Should().BeTrue();
    }

    private static User CreateUser()
    {
        Email email = Email.Create("john.doe@example.com").Value;
        Password password = Password.Create(HashedPassword).Value;
        return User.Create("John", "Doe", email, password, UtcNow).Value;
    }

    private static UserToken IssueToken(User user) => user.IssueToken(
        "access-token",
        "refresh-token",
        UtcNow.AddMinutes(60),
        UtcNow.AddDays(7),
        UtcNow).Value;
}
