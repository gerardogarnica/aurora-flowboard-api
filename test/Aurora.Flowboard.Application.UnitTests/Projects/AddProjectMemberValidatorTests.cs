namespace Aurora.Flowboard.Application.UnitTests.Projects;

public sealed class AddProjectMemberValidatorTests
{
    private readonly AddProjectMemberValidator _validator;

    public AddProjectMemberValidatorTests()
    {
        _validator = new AddProjectMemberValidator();
    }

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        AddProjectMemberCommand command = AddProjectMemberCommandData.GetValidCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_ProjectIdIsEmpty()
    {
        AddProjectMemberCommand command = AddProjectMemberCommandData.GetValidCommand(Guid.Empty, Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_UserIdIsEmpty()
    {
        AddProjectMemberCommand command = AddProjectMemberCommandData.GetValidCommand(Guid.NewGuid(), Guid.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_RoleIsInvalid()
    {
        var command = new AddProjectMemberCommand(Guid.NewGuid(), Guid.NewGuid(), (ProjectRole)99);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
