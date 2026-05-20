namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class GetWorkItemByIdValidatorTests
{
    private readonly GetWorkItemByIdValidator _validator = new();

    [Fact]
    public void Should_Pass_When_WorkItemIdIsNotEmpty()
    {
        // Arrange
        var query = new GetWorkItemByIdQuery(Guid.NewGuid());

        // Act
        FluentValidation.Results.ValidationResult result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_WorkItemIdIsEmpty()
    {
        // Arrange
        var query = new GetWorkItemByIdQuery(Guid.Empty);

        // Act
        FluentValidation.Results.ValidationResult result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
    }
}
