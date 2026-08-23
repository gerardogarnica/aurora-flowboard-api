namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class UpdateWorkItemTypeValidatorTests
{
    private const int UndefinedEnumValue = 999;

    private readonly UpdateWorkItemTypeValidator _validator = new();

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        UpdateWorkItemTypeCommand command = new(Guid.NewGuid(), WorkItemType.Bug);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_IdIsEmpty()
    {
        UpdateWorkItemTypeCommand command = new(Guid.Empty, WorkItemType.Bug);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_TypeIsNotDefined()
    {
        UpdateWorkItemTypeCommand command = new(Guid.NewGuid(), (WorkItemType)UndefinedEnumValue);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
