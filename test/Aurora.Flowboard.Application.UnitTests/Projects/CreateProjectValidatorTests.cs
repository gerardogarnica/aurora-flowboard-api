namespace Aurora.Flowboard.Application.UnitTests.Projects;

public sealed class CreateProjectValidatorTests
{
    private readonly CreateProjectValidator _validator;

    public CreateProjectValidatorTests()
    {
        _validator = new CreateProjectValidator();
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
            string.Empty, null, ProjectCommandData.Prefix, ProjectCommandData.Kind, ProjectCommandData.Color, []);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_NameExceedsMaxLength()
    {
        string longName = new('A', Project.MaxNameLength + 1);
        var command = new CreateProjectCommand(
            longName, null, ProjectCommandData.Prefix, ProjectCommandData.Kind, ProjectCommandData.Color, []);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_PrefixIsEmpty()
    {
        var command = new CreateProjectCommand(
            ProjectCommandData.Name, null, string.Empty, ProjectCommandData.Kind, ProjectCommandData.Color, []);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_PrefixExceedsMaxLength()
    {
        string longPrefix = new('A', ProjectCode.MaxLength + 1);
        var command = new CreateProjectCommand(
            ProjectCommandData.Name, null, longPrefix, ProjectCommandData.Kind, ProjectCommandData.Color, []);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_PrefixContainsNonAlphabeticCharacters()
    {
        var command = new CreateProjectCommand(
            ProjectCommandData.Name, null, "A1B", ProjectCommandData.Kind, ProjectCommandData.Color, []);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_KindIsOutOfRange()
    {
        var command = new CreateProjectCommand(
            ProjectCommandData.Name, null, ProjectCommandData.Prefix, (ProjectKind)99, ProjectCommandData.Color, []);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_DescriptionExceedsMaxLength()
    {
        string longDescription = new('A', Project.MaxDescriptionLength + 1);
        var command = new CreateProjectCommand(
            ProjectCommandData.Name, longDescription, ProjectCommandData.Prefix, ProjectCommandData.Kind, ProjectCommandData.Color, []);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Pass_When_FlowStatesAreValid()
    {
        CreateProjectCommand command = ProjectCommandData.GetCreateCommand(ProjectCommandData.GetValidCreateProjectStates());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_FlowStateNameIsEmpty()
    {
        IReadOnlyCollection<CreateProjectState> flowStates =
            [new CreateProjectState(string.Empty, FlowStateCategory.Active, ProjectCommandData.FlowStateColor, [ProjectRole.Admin])];
        CreateProjectCommand command = ProjectCommandData.GetCreateCommand(flowStates);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_FlowStateNameExceedsMaxLength()
    {
        string longName = new('A', FlowState.MaxNameLength + 1);
        IReadOnlyCollection<CreateProjectState> flowStates =
            [new CreateProjectState(longName, FlowStateCategory.Active, ProjectCommandData.FlowStateColor, [ProjectRole.Admin])];
        CreateProjectCommand command = ProjectCommandData.GetCreateCommand(flowStates);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_FlowStateCategoryIsOutOfRange()
    {
        IReadOnlyCollection<CreateProjectState> flowStates =
            [new CreateProjectState("Backlog", (FlowStateCategory)99, ProjectCommandData.FlowStateColor, [ProjectRole.Admin])];
        CreateProjectCommand command = ProjectCommandData.GetCreateCommand(flowStates);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_FlowStateColorIsEmpty()
    {
        IReadOnlyCollection<CreateProjectState> flowStates =
            [new CreateProjectState("Backlog", FlowStateCategory.Active, string.Empty, [ProjectRole.Admin])];
        CreateProjectCommand command = ProjectCommandData.GetCreateCommand(flowStates);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_FlowStateAllowedRolesIsEmpty()
    {
        IReadOnlyCollection<CreateProjectState> flowStates =
            [new CreateProjectState("Backlog", FlowStateCategory.Active, ProjectCommandData.FlowStateColor, [])];
        CreateProjectCommand command = ProjectCommandData.GetCreateCommand(flowStates);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_FlowStateAllowedRoleIsOutOfRange()
    {
        IReadOnlyCollection<CreateProjectState> flowStates =
            [new CreateProjectState("Backlog", FlowStateCategory.Active, ProjectCommandData.FlowStateColor, [(ProjectRole)99])];
        CreateProjectCommand command = ProjectCommandData.GetCreateCommand(flowStates);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
