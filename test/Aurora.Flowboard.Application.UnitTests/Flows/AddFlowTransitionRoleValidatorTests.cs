namespace Aurora.Flowboard.Application.UnitTests.Flows;

public sealed class AddFlowTransitionRoleValidatorTests
{
    private readonly AddFlowTransitionRoleValidator _validator = new();

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        AddFlowTransitionRoleCommand command = new(Guid.NewGuid(), Guid.NewGuid(), ProjectRole.Developer);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_FlowIdIsEmpty()
    {
        AddFlowTransitionRoleCommand command = new(Guid.Empty, Guid.NewGuid(), ProjectRole.Developer);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_TransitionIdIsEmpty()
    {
        AddFlowTransitionRoleCommand command = new(Guid.NewGuid(), Guid.Empty, ProjectRole.Developer);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_RoleIsNotValidEnumValue()
    {
        AddFlowTransitionRoleCommand command = new(Guid.NewGuid(), Guid.NewGuid(), (ProjectRole)999);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
