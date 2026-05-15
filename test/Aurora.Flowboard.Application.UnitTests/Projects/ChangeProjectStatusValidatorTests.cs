namespace Aurora.Flowboard.Application.UnitTests.Projects;

public sealed class ChangeProjectStatusValidatorTests
{
    private readonly ChangeProjectStatusValidator _validator;

    public ChangeProjectStatusValidatorTests()
    {
        _validator = new ChangeProjectStatusValidator();
    }

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        ChangeProjectStatusCommand command = ProjectCommandData.GetChangeStatusCommand(Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_IdIsEmpty()
    {
        ChangeProjectStatusCommand command = ProjectCommandData.GetChangeStatusCommand(Guid.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_NewStatusIsInvalid()
    {
        var command = new ChangeProjectStatusCommand(Guid.NewGuid(), (ProjectStatus)99);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
