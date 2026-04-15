namespace Aurora.Flowboard.Domain.UnitTests.Shared;

public sealed class EmailTests
{
    [Fact]
    public void Should_CreateEmail_When_InputIsValid()
    {
        // Act
        Result<Email> result = Email.Create("user@example.com");

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Value.Should().Be("user@example.com");
    }

    [Fact]
    public void Should_LowercaseValue_When_InputHasUpperCase()
    {
        // Act
        Result<Email> result = Email.Create("USER@EXAMPLE.COM");

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Value.Should().Be("user@example.com");
    }

    [Fact]
    public void Should_TrimWhitespace_When_Creating()
    {
        // Act
        Result<Email> result = Email.Create("  user@example.com  ");

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Value.Should().Be("user@example.com");
    }

    [Fact]
    public void Should_ReturnValue_When_ImplicitlyConvertedToString()
    {
        // Arrange
        Email email = Email.Create("user@example.com").Value;

        // Act
        string value = email;

        // Assert
        value.Should().Be("user@example.com");
    }

    [Fact]
    public void Should_ReturnValue_When_ToStringCalled()
    {
        // Arrange
        Email email = Email.Create("user@example.com").Value;

        // Act
        string value = email.ToString();

        // Assert
        value.Should().Be("user@example.com");
    }

    [Fact]
    public void Should_BeEqual_When_TwoEmailsHaveSameValue()
    {
        // Arrange
        Email email1 = Email.Create("user@example.com").Value;
        Email email2 = Email.Create("user@example.com").Value;

        // Assert
        email1.Should().Be(email2);
    }

    [Fact]
    public void Should_Fail_When_EmailIsEmpty()
    {
        // Act
        Result<Email> result = Email.Create(string.Empty);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(EmailErrors.Empty);
    }

    [Fact]
    public void Should_Fail_When_EmailIsWhitespace()
    {
        // Act
        Result<Email> result = Email.Create("   ");

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(EmailErrors.Empty);
    }

    [Fact]
    public void Should_Fail_When_EmailExceedsMaxLength()
    {
        // Arrange
        string longEmail = string.Concat(new string('a', 250), "@b.com"); // 256 chars

        // Act
        Result<Email> result = Email.Create(longEmail);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(EmailErrors.TooLong);
    }

    [Fact]
    public void Should_Fail_When_EmailHasNoAtSign()
    {
        // Act
        Result<Email> result = Email.Create("userexample.com");

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(EmailErrors.InvalidFormat);
    }

    [Fact]
    public void Should_Fail_When_AtSignIsAtStart()
    {
        // Act
        Result<Email> result = Email.Create("@example.com");

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(EmailErrors.InvalidFormat);
    }

    [Fact]
    public void Should_Fail_When_NoDotAfterAtSign()
    {
        // Act
        Result<Email> result = Email.Create("user@examplecom");

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(EmailErrors.InvalidFormat);
    }

    [Fact]
    public void Should_Fail_When_DotIsImmediatelyAfterAtSign()
    {
        // Act
        Result<Email> result = Email.Create("user@.com");

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(EmailErrors.InvalidFormat);
    }

    [Fact]
    public void Should_Fail_When_DotIsLastCharacter()
    {
        // Act
        Result<Email> result = Email.Create("user@example.");

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(EmailErrors.InvalidFormat);
    }
}
