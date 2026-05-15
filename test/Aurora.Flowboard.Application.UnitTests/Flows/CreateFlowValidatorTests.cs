namespace Aurora.Flowboard.Application.UnitTests.Flows;

public sealed class CreateFlowValidatorTests
{
    private readonly CreateFlowValidator _validator = new();

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        CreateFlowCommand command = new(FlowCommandData.FlowName, FlowCommandData.FlowDescription, Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_ProjectIdIsEmpty()
    {
        CreateFlowCommand command = new(FlowCommandData.FlowName, null, Guid.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_NameIsEmpty()
    {
        CreateFlowCommand command = new(string.Empty, null, Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_NameExceedsMaxLength()
    {
        string longName = new('A', Flow.MaxNameLength + 1);
        CreateFlowCommand command = new(longName, null, Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_DescriptionExceedsMaxLength()
    {
        string longDescription = new('A', Flow.MaxDescriptionLength + 1);
        CreateFlowCommand command = new(FlowCommandData.FlowName, longDescription, Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Pass_When_DescriptionIsNull()
    {
        CreateFlowCommand command = new(FlowCommandData.FlowName, null, Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
