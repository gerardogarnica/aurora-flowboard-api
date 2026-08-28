namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class UpdateWorkItemPriorityValidatorTests
{
    private const int UndefinedEnumValue = 999;

    private readonly UpdateWorkItemPriorityValidator _validator = new();

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        UpdateWorkItemPriorityCommand command = new(Guid.NewGuid(), Priority.High);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_IdIsEmpty()
    {
        UpdateWorkItemPriorityCommand command = new(Guid.Empty, Priority.High);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_PriorityIsNotDefined()
    {
        UpdateWorkItemPriorityCommand command = new(Guid.NewGuid(), (Priority)UndefinedEnumValue);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
