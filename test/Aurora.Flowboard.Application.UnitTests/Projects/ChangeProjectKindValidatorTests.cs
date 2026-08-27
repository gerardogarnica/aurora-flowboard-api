namespace Aurora.Flowboard.Application.UnitTests.Projects;

public sealed class ChangeProjectKindValidatorTests
{
    private readonly ChangeProjectKindValidator _validator;

    public ChangeProjectKindValidatorTests()
    {
        _validator = new ChangeProjectKindValidator();
    }

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        ChangeProjectKindCommand command = ProjectCommandData.GetChangeKindCommand(Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_IdIsEmpty()
    {
        ChangeProjectKindCommand command = ProjectCommandData.GetChangeKindCommand(Guid.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_NewKindIsInvalid()
    {
        var command = new ChangeProjectKindCommand(Guid.NewGuid(), (ProjectKind)99);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
