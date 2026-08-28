namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class GetWorkItemByCodeValidatorTests
{
    private readonly GetWorkItemByCodeValidator _validator = new();

    [Fact]
    public void Should_Pass_When_CodeIsNotEmpty()
    {
        // Arrange
        var query = new GetWorkItemByCodeQuery("WIP-1");

        // Act
        FluentValidation.Results.ValidationResult result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_CodeIsEmpty()
    {
        // Arrange
        var query = new GetWorkItemByCodeQuery(string.Empty);

        // Act
        FluentValidation.Results.ValidationResult result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
    }

    [Fact]
    public void Should_Fail_When_CodeExceedsMaxLength()
    {
        // Arrange
        var query = new GetWorkItemByCodeQuery(new string('A', WorkItem.MaxCodeLength + 1));

        // Act
        FluentValidation.Results.ValidationResult result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
    }
}
