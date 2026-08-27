namespace Aurora.Flowboard.Application.UnitTests.Components;

public sealed class RenameComponentValidatorTests
{
    private readonly RenameComponentValidator _validator;

    public RenameComponentValidatorTests()
    {
        _validator = new RenameComponentValidator();
    }

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        RenameComponentCommand command = ComponentCommandData.GetRenameCommand(Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_ComponentIdIsEmpty()
    {
        var command = new RenameComponentCommand(Guid.Empty, ComponentCommandData.RenamedTo);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_NameIsEmpty()
    {
        var command = new RenameComponentCommand(Guid.NewGuid(), string.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_NameExceedsMaxLength()
    {
        string longName = new('A', Component.MaxNameLength + 1);
        var command = new RenameComponentCommand(Guid.NewGuid(), longName);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
