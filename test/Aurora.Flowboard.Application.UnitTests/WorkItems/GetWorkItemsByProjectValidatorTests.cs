namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class GetWorkItemsByProjectValidatorTests
{
    private readonly GetWorkItemsByProjectValidator _validator = new();

    [Fact]
    public void Should_Pass_When_ProjectIdIsNotEmpty()
    {
        // Arrange
        var query = new GetWorkItemsByProjectQuery(Guid.NewGuid());

        // Act
        FluentValidation.Results.ValidationResult result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_ProjectIdIsEmpty()
    {
        // Arrange
        var query = new GetWorkItemsByProjectQuery(Guid.Empty);

        // Act
        FluentValidation.Results.ValidationResult result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
    }
}
