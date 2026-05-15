namespace Aurora.Flowboard.Application.UnitTests.Projects;

public sealed class CreateProjectValidatorTests
{
    private readonly CreateProjectValidator _validator;

    public CreateProjectValidatorTests()
    {
        IDateTimeProvider dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.Today.Returns(CreateProjectCommandData.Today);
        _validator = new CreateProjectValidator(dateTimeProvider);
    }

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        CreateProjectCommand command = CreateProjectCommandData.GetValidCommand();

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_NameIsEmpty()
    {
        var command = new CreateProjectCommand(
            string.Empty, null, CreateProjectCommandData.Code, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_NameExceedsMaxLength()
    {
        string longName = new('A', Project.MaxNameLength + 1);
        var command = new CreateProjectCommand(
            longName, null, CreateProjectCommandData.Code, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_CodeIsEmpty()
    {
        var command = new CreateProjectCommand(
            CreateProjectCommandData.Name, null, string.Empty, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_CodeExceedsMaxLength()
    {
        string longCode = new('A', ProjectCode.MaxLength + 1);
        var command = new CreateProjectCommand(
            CreateProjectCommandData.Name, null, longCode, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_CodeContainsNonAlphabeticCharacters()
    {
        var command = new CreateProjectCommand(
            CreateProjectCommandData.Name, null, "A1B", null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_DescriptionExceedsMaxLength()
    {
        string longDescription = new('A', Project.MaxDescriptionLength + 1);
        var command = new CreateProjectCommand(
            CreateProjectCommandData.Name, longDescription, CreateProjectCommandData.Code, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_EstimatedCompletionDateIsInThePast()
    {
        DateOnly pastDate = CreateProjectCommandData.Today.AddDays(-1);
        var command = new CreateProjectCommand(
            CreateProjectCommandData.Name, null, CreateProjectCommandData.Code, pastDate);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Pass_When_EstimatedCompletionDateIsToday()
    {
        var command = new CreateProjectCommand(
            CreateProjectCommandData.Name, null, CreateProjectCommandData.Code, CreateProjectCommandData.Today);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Pass_When_EstimatedCompletionDateIsNull()
    {
        var command = new CreateProjectCommand(
            CreateProjectCommandData.Name, null, CreateProjectCommandData.Code, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
