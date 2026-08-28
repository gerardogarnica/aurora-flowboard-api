namespace Aurora.Flowboard.Application.UnitTests.Projects;

public sealed class GetProjectBoardValidatorTests
{
    private readonly GetProjectBoardValidator _validator = new();

    [Fact]
    public void Should_Pass_When_ProjectIdIsNotEmpty()
    {
        // Arrange
        var query = new GetProjectBoardQuery(Guid.NewGuid());

        // Act
        FluentValidation.Results.ValidationResult result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_ProjectIdIsEmpty()
    {
        // Arrange
        var query = new GetProjectBoardQuery(Guid.Empty);

        // Act
        FluentValidation.Results.ValidationResult result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
    }
}
