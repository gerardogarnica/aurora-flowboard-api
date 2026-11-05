namespace Aurora.Flowboard.Application.UnitTests.Abstractions;

public sealed class DbContextSeedExtensionsTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();

    [Fact]
    public async Task Should_ExposeTheItems_When_SeededWithSeveralItems()
    {
        // Arrange
        User first = CreateUser("first@example.com");
        User second = CreateUser("second@example.com");

        // Act
        _dbContext.Seed(c => c.Users, first, second);

        // Assert
        List<User> users = await _dbContext.Users.ToListAsync(CancellationToken.None);
        users.Should().Equal(first, second);
    }

    [Fact]
    public async Task Should_ExposeAnEmptySet_When_SeededWithoutItems()
    {
        // Act
        _dbContext.Seed(c => c.Users);

        // Assert
        bool any = await _dbContext.Users.AnyAsync(CancellationToken.None);
        any.Should().BeFalse();
    }

    [Fact]
    public async Task Should_ExposeTheItems_When_SeededWithAnExistingCollection()
    {
        // Arrange
        List<User> seeded = [CreateUser("first@example.com"), CreateUser("second@example.com")];

        // Act
        _dbContext.Seed(c => c.Users, seeded);

        // Assert
        List<User> users = await _dbContext.Users.ToListAsync(CancellationToken.None);
        users.Should().Equal(seeded);
    }

    [Fact]
    public void Should_ReturnTheSetTheContextExposes_When_Seeded()
    {
        // Act
        DbSet<User> usersMock = _dbContext.Seed(c => c.Users, CreateUser("first@example.com"));

        // Assert
        _dbContext.Users.Should().BeSameAs(usersMock);
    }

    private static User CreateUser(string email) =>
        User.Create("Test", "User", Email.Create(email).Value, Password.Create("hashed_password_123").Value, UtcNow).Value;
}
