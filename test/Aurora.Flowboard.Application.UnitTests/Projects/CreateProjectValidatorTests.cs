namespace Aurora.Flowboard.Application.UnitTests.Projects;

public sealed class CreateProjectValidatorTests
{
    private readonly CreateProjectValidator _validator;

    public CreateProjectValidatorTests()
    {
        IDateTimeProvider dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.Today.Returns(ProjectCommandData.Today);
        _validator = new CreateProjectValidator(dateTimeProvider);
    }

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        CreateProjectCommand command = ProjectCommandData.GetCreateCommand();

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_NameIsEmpty()
    {
        var command = new CreateProjectCommand(
            string.Empty, null, ProjectCommandData.Code, ProjectCommandData.Color, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_NameExceedsMaxLength()
    {
        string longName = new('A', Project.MaxNameLength + 1);
        var command = new CreateProjectCommand(
            longName, null, ProjectCommandData.Code, ProjectCommandData.Color, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_CodeIsEmpty()
    {
        var command = new CreateProjectCommand(
            ProjectCommandData.Name, null, string.Empty, ProjectCommandData.Color, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_CodeExceedsMaxLength()
    {
        string longCode = new('A', ProjectCode.MaxLength + 1);
        var command = new CreateProjectCommand(
            ProjectCommandData.Name, null, longCode, ProjectCommandData.Color, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_CodeContainsNonAlphabeticCharacters()
    {
        var command = new CreateProjectCommand(
            ProjectCommandData.Name, null, "A1B", ProjectCommandData.Color, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_DescriptionExceedsMaxLength()
    {
        string longDescription = new('A', Project.MaxDescriptionLength + 1);
        var command = new CreateProjectCommand(
            ProjectCommandData.Name, longDescription, ProjectCommandData.Code, ProjectCommandData.Color, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_EstimatedCompletionDateIsInThePast()
    {
        DateOnly pastDate = ProjectCommandData.Today.AddDays(-1);
        var command = new CreateProjectCommand(
            ProjectCommandData.Name, null, ProjectCommandData.Code, ProjectCommandData.Color, pastDate);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Pass_When_EstimatedCompletionDateIsToday()
    {
        var command = new CreateProjectCommand(
            ProjectCommandData.Name, null, ProjectCommandData.Code, ProjectCommandData.Color, ProjectCommandData.Today);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Pass_When_EstimatedCompletionDateIsNull()
    {
        var command = new CreateProjectCommand(
            ProjectCommandData.Name, null, ProjectCommandData.Code, ProjectCommandData.Color, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
