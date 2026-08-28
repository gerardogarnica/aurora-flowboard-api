namespace Aurora.Flowboard.Application.UnitTests.Projects;

public sealed class RemoveFlowTransitionRoleValidatorTests
{
    private readonly RemoveFlowTransitionRoleValidator _validator = new();

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        RemoveFlowTransitionRoleCommand command = new(Guid.NewGuid(), Guid.NewGuid(), ProjectRole.Developer);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_ProjectIdIsEmpty()
    {
        RemoveFlowTransitionRoleCommand command = new(Guid.Empty, Guid.NewGuid(), ProjectRole.Developer);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_TransitionIdIsEmpty()
    {
        RemoveFlowTransitionRoleCommand command = new(Guid.NewGuid(), Guid.Empty, ProjectRole.Developer);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_RoleIsNotInEnum()
    {
        RemoveFlowTransitionRoleCommand command = new(Guid.NewGuid(), Guid.NewGuid(), (ProjectRole)99);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
