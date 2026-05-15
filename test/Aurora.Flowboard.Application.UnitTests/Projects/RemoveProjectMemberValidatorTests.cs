namespace Aurora.Flowboard.Application.UnitTests.Projects;

public sealed class RemoveProjectMemberValidatorTests
{
    private readonly RemoveProjectMemberValidator _validator;

    public RemoveProjectMemberValidatorTests()
    {
        _validator = new RemoveProjectMemberValidator();
    }

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        RemoveProjectMemberCommand command = ProjectCommandData.GetRemoveCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_ProjectIdIsEmpty()
    {
        RemoveProjectMemberCommand command = ProjectCommandData.GetRemoveCommand(Guid.Empty, Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_UserIdIsEmpty()
    {
        RemoveProjectMemberCommand command = ProjectCommandData.GetRemoveCommand(Guid.NewGuid(), Guid.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
