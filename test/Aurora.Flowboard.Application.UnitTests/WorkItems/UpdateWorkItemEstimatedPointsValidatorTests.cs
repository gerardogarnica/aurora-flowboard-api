namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class UpdateWorkItemEstimatedPointsValidatorTests
{
    private const int ValidPoints = 8;

    private readonly UpdateWorkItemEstimatedPointsValidator _validator = new();

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        UpdateWorkItemEstimatedPointsCommand command = new(Guid.NewGuid(), ValidPoints);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Pass_When_EstimatedPointsIsNull()
    {
        UpdateWorkItemEstimatedPointsCommand command = new(Guid.NewGuid(), null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_IdIsEmpty()
    {
        UpdateWorkItemEstimatedPointsCommand command = new(Guid.Empty, ValidPoints);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_EstimatedPointsIsZero()
    {
        UpdateWorkItemEstimatedPointsCommand command = new(Guid.NewGuid(), 0);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
