using Aurora.Flowboard.Application.Users.GetUserById;

namespace Aurora.Flowboard.Application.UnitTests.Users;

public sealed class GetUserByIdHandlerTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly GetUserByIdHandler _handler;

    public GetUserByIdHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _handler = new GetUserByIdHandler(_dbContext);
    }

    [Fact]
    public async Task Should_ReturnNotFoundError_When_UserDoesNotExist()
    {
        // Arrange
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<User>());
        _dbContext.Users.Returns(usersMock);

        // Act
        Result<UserProfileResponse> result =
            await _handler.Handle(new GetUserByIdQuery(Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(UserErrors.NotFound);
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_UserExists()
    {
        // Arrange
        User user = CreateUserCommandData.GetExistingUser();
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        _dbContext.Users.Returns(usersMock);

        // Act
        Result<UserProfileResponse> result =
            await _handler.Handle(new GetUserByIdQuery(user.Id), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task Should_MapAllScalarFields_When_UserExists()
    {
        // Arrange
        User user = CreateUserCommandData.GetExistingUser();
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        _dbContext.Users.Returns(usersMock);

        // Act
        Result<UserProfileResponse> result =
            await _handler.Handle(new GetUserByIdQuery(user.Id), CancellationToken.None);

        // Assert
        UserProfileResponse response = result.Value;
        response.UserId.Should().Be(user.Id);
        response.FirstName.Should().Be(user.FirstName);
        response.LastName.Should().Be(user.LastName);
        response.FullName.Should().Be(user.FullName);
        response.Initials.Should().Be(user.Initials);
        response.Email.Should().Be(user.Email.Value);
        response.IsActive.Should().Be(user.IsActive);
        response.CreatedOnUtc.Should().Be(user.CreatedOnUtc);
        response.UpdatedOnUtc.Should().Be(user.UpdatedOnUtc);
    }

    [Fact]
    public async Task Should_ReturnEmptyRoles_When_UserHasNoRoles()
    {
        // Arrange
        User user = CreateUserCommandData.GetExistingUser();
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        _dbContext.Users.Returns(usersMock);

        // Act
        Result<UserProfileResponse> result =
            await _handler.Handle(new GetUserByIdQuery(user.Id), CancellationToken.None);

        // Assert
        result.Value.Roles.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_MapRolesCollection_When_UserHasRoles()
    {
        // Arrange
        User user = CreateUserCommandData.GetExistingUser();
        user.AssignRole(Role.Member);
        user.AssignRole(Role.Administrator);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        _dbContext.Users.Returns(usersMock);

        // Act
        Result<UserProfileResponse> result =
            await _handler.Handle(new GetUserByIdQuery(user.Id), CancellationToken.None);

        // Assert
        result.Value.Roles.Should().HaveCount(2);
        result.Value.Roles.Should().Contain(Role.Member.Name);
        result.Value.Roles.Should().Contain(Role.Administrator.Name);
    }
}
