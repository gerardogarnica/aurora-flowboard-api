namespace Aurora.Flowboard.Application.UnitTests.Projects;

public sealed class AddFlowStateValidatorTests
{
    private readonly AddFlowStateValidator _validator = new();

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        AddFlowStateCommand command = new(Guid.NewGuid(), ProjectCommandData.FlowStateName, FlowStateCategory.Active, ProjectCommandData.FlowStateColor, [ProjectRole.Developer]);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_ProjectIdIsEmpty()
    {
        AddFlowStateCommand command = new(Guid.Empty, ProjectCommandData.FlowStateName, FlowStateCategory.Active, ProjectCommandData.FlowStateColor, []);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_NameIsEmpty()
    {
        AddFlowStateCommand command = new(Guid.NewGuid(), string.Empty, FlowStateCategory.Active, ProjectCommandData.FlowStateColor, []);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_NameExceedsMaxLength()
    {
        string longName = new('A', FlowState.MaxNameLength + 1);
        AddFlowStateCommand command = new(Guid.NewGuid(), longName, FlowStateCategory.Active, ProjectCommandData.FlowStateColor, []);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
