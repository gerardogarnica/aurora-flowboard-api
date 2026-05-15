namespace Aurora.Flowboard.Application.UnitTests.Projects;

public sealed class UpdateProjectValidatorTests
{
    private readonly UpdateProjectValidator _validator;

    public UpdateProjectValidatorTests()
    {
        IDateTimeProvider dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.Today.Returns(ProjectCommandData.Today);
        _validator = new UpdateProjectValidator(dateTimeProvider);
    }

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        UpdateProjectCommand command = ProjectCommandData.GetUpdateCommand(Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_IdIsEmpty()
    {
        UpdateProjectCommand command = ProjectCommandData.GetUpdateCommand(Guid.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_NameIsEmpty()
    {
        var command = new UpdateProjectCommand(
            Guid.NewGuid(), string.Empty, null, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_NameExceedsMaxLength()
    {
        string longName = new('A', Project.MaxNameLength + 1);
        var command = new UpdateProjectCommand(
            Guid.NewGuid(), longName, null, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_DescriptionExceedsMaxLength()
    {
        string longDescription = new('A', Project.MaxDescriptionLength + 1);
        var command = new UpdateProjectCommand(
            Guid.NewGuid(), ProjectCommandData.UpdatedName, longDescription, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_EstimatedCompletionDateIsInThePast()
    {
        DateOnly pastDate = ProjectCommandData.Today.AddDays(-1);
        var command = new UpdateProjectCommand(
            Guid.NewGuid(), ProjectCommandData.UpdatedName, null, pastDate);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Pass_When_EstimatedCompletionDateIsToday()
    {
        var command = new UpdateProjectCommand(
            Guid.NewGuid(), ProjectCommandData.UpdatedName, null, ProjectCommandData.Today);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Pass_When_EstimatedCompletionDateIsNull()
    {
        var command = new UpdateProjectCommand(
            Guid.NewGuid(), ProjectCommandData.UpdatedName, null, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
