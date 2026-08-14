namespace Aurora.Flowboard.Application.UnitTests.Components;

public sealed class CreateComponentValidatorTests
{
    private readonly CreateComponentValidator _validator;

    public CreateComponentValidatorTests()
    {
        _validator = new CreateComponentValidator();
    }

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        CreateComponentCommand command = ComponentCommandData.GetCreateCommand(Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_ProjectIdIsEmpty()
    {
        var command = new CreateComponentCommand(Guid.Empty, ComponentCommandData.Name);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_NameIsEmpty()
    {
        var command = new CreateComponentCommand(Guid.NewGuid(), string.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_NameExceedsMaxLength()
    {
        string longName = new('A', Component.MaxNameLength + 1);
        var command = new CreateComponentCommand(Guid.NewGuid(), longName);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
