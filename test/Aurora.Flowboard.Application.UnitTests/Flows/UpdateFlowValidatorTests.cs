namespace Aurora.Flowboard.Application.UnitTests.Flows;

public sealed class UpdateFlowValidatorTests
{
    private readonly UpdateFlowValidator _validator = new();

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        UpdateFlowCommand command = new(Guid.NewGuid(), FlowCommandData.UpdatedName, FlowCommandData.UpdatedDescription);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_IdIsEmpty()
    {
        UpdateFlowCommand command = new(Guid.Empty, FlowCommandData.UpdatedName, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_NameIsEmpty()
    {
        UpdateFlowCommand command = new(Guid.NewGuid(), string.Empty, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_NameExceedsMaxLength()
    {
        string longName = new('A', Flow.MaxNameLength + 1);
        UpdateFlowCommand command = new(Guid.NewGuid(), longName, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_DescriptionExceedsMaxLength()
    {
        string longDescription = new('A', Flow.MaxDescriptionLength + 1);
        UpdateFlowCommand command = new(Guid.NewGuid(), FlowCommandData.UpdatedName, longDescription);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Pass_When_DescriptionIsNull()
    {
        UpdateFlowCommand command = new(Guid.NewGuid(), FlowCommandData.UpdatedName, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
