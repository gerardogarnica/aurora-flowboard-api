namespace Aurora.Flowboard.Domain.UnitTests.Projects;

public sealed class ProjectCodeTests
{
    [Fact]
    public void Should_CreateProjectCode_When_InputIsValid()
    {
        // Act
        Result<ProjectCode> result = ProjectCode.Create("AFB");

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Value.Should().Be("AFB");
    }

    [Fact]
    public void Should_UppercaseValue_When_InputIsLowerCase()
    {
        // Act
        Result<ProjectCode> result = ProjectCode.Create("afb");

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Value.Should().Be("AFB");
    }

    [Fact]
    public void Should_TrimWhitespace_When_Creating()
    {
        // Act
        Result<ProjectCode> result = ProjectCode.Create(" AB ");

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Value.Should().Be("AB");
    }

    [Fact]
    public void Should_ReturnValue_When_ConvertedToString()
    {
        // Arrange
        ProjectCode prefix = ProjectCode.Create("AFB").Value;

        // Act & Assert
        prefix.ToString().Should().Be("AFB");
        ((string)prefix).Should().Be("AFB");
    }

    [Fact]
    public void Should_Fail_When_CodeIsEmpty()
    {
        // Act
        Result<ProjectCode> result = ProjectCode.Create(string.Empty);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.PrefixRequired);
    }

    [Fact]
    public void Should_Fail_When_CodeIsWhitespace()
    {
        // Act
        Result<ProjectCode> result = ProjectCode.Create("   ");

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.PrefixRequired);
    }

    [Fact]
    public void Should_Fail_When_CodeExceedsMaxLength()
    {
        // Act
        Result<ProjectCode> result = ProjectCode.Create("ABCD");

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.PrefixTooLong);
    }

    [Fact]
    public void Should_Fail_When_CodeContainsDigits()
    {
        // Act
        Result<ProjectCode> result = ProjectCode.Create("A1B");

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.PrefixInvalidCharacters);
    }

    [Fact]
    public void Should_Fail_When_CodeContainsSpecialCharacters()
    {
        // Act
        Result<ProjectCode> result = ProjectCode.Create("A@B");

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.PrefixInvalidCharacters);
    }
}
