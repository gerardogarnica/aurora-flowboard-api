namespace Aurora.Flowboard.Domain.UnitTests.Users;

public sealed class PasswordTests
{
    [Fact]
    public void Should_CreatePassword_When_HashIsValid()
    {
        // Act
        Result<Password> result = Password.Create("hashed_password_123");

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Hash.Should().Be("hashed_password_123");
    }

    [Fact]
    public void Should_BeEqual_When_TwoPasswordsHaveSameHash()
    {
        // Arrange
        Password password1 = Password.Create("hashed_password_123").Value;
        Password password2 = Password.Create("hashed_password_123").Value;

        // Assert
        password1.Should().Be(password2);
    }

    [Fact]
    public void Should_ReturnHash_When_ToStringCalled()
    {
        // Arrange
        Password password = Password.Create("hashed_password_123").Value;

        // Act
        string value = password.ToString();

        // Assert
        value.Should().Be("hashed_password_123");
    }

    [Fact]
    public void Should_Fail_When_HashIsEmpty()
    {
        // Act
        Result<Password> result = Password.Create(string.Empty);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(PasswordErrors.HashRequired);
    }

    [Fact]
    public void Should_Fail_When_HashIsWhitespace()
    {
        // Act
        Result<Password> result = Password.Create("   ");

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(PasswordErrors.HashRequired);
    }

    [Fact]
    public void Should_Fail_When_HashIsBelowMinLength()
    {
        // Arrange
        string shortHash = new('a', Password.MinHashLength - 1);

        // Act
        Result<Password> result = Password.Create(shortHash);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(PasswordErrors.HashTooShort);
    }

    [Fact]
    public void Should_CreatePassword_When_HashIsExactlyMinLength()
    {
        // Arrange
        string minHash = new('a', Password.MinHashLength);

        // Act
        Result<Password> result = Password.Create(minHash);

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_HashExceedsMaxLength()
    {
        // Arrange
        string longHash = new('a', Password.MaxHashLength + 1);

        // Act
        Result<Password> result = Password.Create(longHash);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(PasswordErrors.HashTooLong);
    }

    [Fact]
    public void Should_CreatePassword_When_HashIsExactlyMaxLength()
    {
        // Arrange
        string maxHash = new('a', Password.MaxHashLength);

        // Act
        Result<Password> result = Password.Create(maxHash);

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }
}
