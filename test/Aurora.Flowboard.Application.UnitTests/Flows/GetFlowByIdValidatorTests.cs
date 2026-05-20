namespace Aurora.Flowboard.Application.UnitTests.Flows;

public sealed class GetFlowByIdValidatorTests
{
    private readonly GetFlowByIdValidator _validator = new();

    [Fact]
    public void Should_Pass_When_FlowIdIsNotEmpty()
    {
        // Arrange
        var query = new GetFlowByIdQuery(Guid.NewGuid());

        // Act
        FluentValidation.Results.ValidationResult result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_FlowIdIsEmpty()
    {
        // Arrange
        var query = new GetFlowByIdQuery(Guid.Empty);

        // Act
        FluentValidation.Results.ValidationResult result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
    }
}
