using Aurora.Flowboard.Application.Users.GetAll;

namespace Aurora.Flowboard.Application.UnitTests.Users;

public sealed class GetAllUsersHandlerTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly GetAllUsersHandler _handler;

    public GetAllUsersHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _handler = new GetAllUsersHandler(_dbContext);
    }

    [Fact]
    public async Task Should_ReturnEmptyCollection_When_NoUsersExist()
    {
        // Arrange
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<User>());
        _dbContext.Users.Returns(usersMock);

        // Act
        Result<IReadOnlyCollection<UserSummaryResponse>> result =
            await _handler.Handle(new GetAllUsersQuery(), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_ReturnAllUsers_When_UsersExist()
    {
        // Arrange
        User first = CreateUserCommandData.GetExistingUser("first@example.com");
        first.AssignRole(Role.Member);
        User second = CreateUserCommandData.GetExistingUser("second@example.com");
        second.AssignRole(Role.Member);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([first, second]);
        _dbContext.Users.Returns(usersMock);

        // Act
        Result<IReadOnlyCollection<UserSummaryResponse>> result =
            await _handler.Handle(new GetAllUsersQuery(), CancellationToken.None);

        // Assert
        result.Value.Should().HaveCount(2);
        result.Value.Select(u => u.UserId).Should().BeEquivalentTo([first.Id, second.Id]);
    }

    [Fact]
    public async Task Should_MapAllScalarFields_When_UserExists()
    {
        // Arrange
        User user = CreateUserCommandData.GetExistingUser();
        user.AssignRole(Role.Member);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        _dbContext.Users.Returns(usersMock);

        // Act
        Result<IReadOnlyCollection<UserSummaryResponse>> result =
            await _handler.Handle(new GetAllUsersQuery(), CancellationToken.None);

        // Assert
        UserSummaryResponse response = result.Value.Single();
        response.UserId.Should().Be(user.Id);
        response.FirstName.Should().Be(user.FirstName);
        response.LastName.Should().Be(user.LastName);
        response.FullName.Should().Be(user.FullName);
        response.Initials.Should().Be(user.Initials);
        response.Email.Should().Be(user.Email.Value);
        response.IsActive.Should().Be(user.IsActive);
        response.Role.Should().Be(Role.Member.Name);
        response.CreatedOnUtc.Should().Be(user.CreatedOnUtc);
        response.UpdatedOnUtc.Should().Be(user.UpdatedOnUtc);
    }
}
