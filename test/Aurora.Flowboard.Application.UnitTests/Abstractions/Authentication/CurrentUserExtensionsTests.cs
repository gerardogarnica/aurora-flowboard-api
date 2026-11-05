using Aurora.Flowboard.Application.UnitTests.Projects;

namespace Aurora.Flowboard.Application.UnitTests.Abstractions.Authentication;

public sealed class CurrentUserExtensionsTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();

    [Fact]
    public async Task Should_ReturnCurrentUser_When_UserExists()
    {
        // Arrange
        User admin = ProjectCommandData.GetAdmin();
        User member = ProjectCommandData.GetMember();
        _userContext.UserId.Returns(member.Id);

        _dbContext.Seed(c => c.Users, admin, member);

        // Act
        Result<User> result = await _dbContext.GetCurrentUserAsync(_userContext, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeSameAs(member);
    }

    [Fact]
    public async Task Should_ReturnUserNotFoundError_When_UserDoesNotExist()
    {
        // Arrange
        User admin = ProjectCommandData.GetAdmin();
        _userContext.UserId.Returns(Guid.NewGuid());

        _dbContext.Seed(c => c.Users, admin);

        // Act
        Result<User> result = await _dbContext.GetCurrentUserAsync(_userContext, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(UserErrors.NotFound);
    }
}
