namespace Aurora.Flowboard.Application.UnitTests.Projects;

public sealed class GetProjectByIdValidatorTests
{
    private readonly GetProjectByIdValidator _validator = new();

    [Fact]
    public void Should_Pass_When_ProjectIdIsNotEmpty()
    {
        // Arrange
        var query = new GetProjectByIdQuery(Guid.NewGuid());

        // Act
        FluentValidation.Results.ValidationResult result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_ProjectIdIsEmpty()
    {
        // Arrange
        var query = new GetProjectByIdQuery(Guid.Empty);

        // Act
        FluentValidation.Results.ValidationResult result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Project ID is required");
    }
}
