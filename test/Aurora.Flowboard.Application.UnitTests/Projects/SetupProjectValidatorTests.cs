namespace Aurora.Flowboard.Application.UnitTests.Projects;

public sealed class SetupProjectValidatorTests
{
    private readonly SetupProjectValidator _validator;

    public SetupProjectValidatorTests()
    {
        _validator = new SetupProjectValidator();
    }

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        SetupProjectCommand command = ProjectCommandData.GetSetupCommand();

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_NameIsEmpty()
    {
        var command = new SetupProjectCommand(
            string.Empty, null, ProjectCommandData.Prefix, ProjectCommandData.Kind, ProjectCommandData.Color, ProjectCommandData.GetSetupFlowStateDtos());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_NameExceedsMaxLength()
    {
        string longName = new('A', Project.MaxNameLength + 1);
        var command = new SetupProjectCommand(
            longName, null, ProjectCommandData.Prefix, ProjectCommandData.Kind, ProjectCommandData.Color, ProjectCommandData.GetSetupFlowStateDtos());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_PrefixIsEmpty()
    {
        var command = new SetupProjectCommand(
            ProjectCommandData.Name, null, string.Empty, ProjectCommandData.Kind, ProjectCommandData.Color, ProjectCommandData.GetSetupFlowStateDtos());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_PrefixExceedsMaxLength()
    {
        string longPrefix = new('A', ProjectCode.MaxLength + 1);
        var command = new SetupProjectCommand(
            ProjectCommandData.Name, null, longPrefix, ProjectCommandData.Kind, ProjectCommandData.Color, ProjectCommandData.GetSetupFlowStateDtos());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_PrefixContainsNonAlphabeticCharacters()
    {
        var command = new SetupProjectCommand(
            ProjectCommandData.Name, null, "A1B", ProjectCommandData.Kind, ProjectCommandData.Color, ProjectCommandData.GetSetupFlowStateDtos());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_KindIsOutOfRange()
    {
        var command = new SetupProjectCommand(
            ProjectCommandData.Name, null, ProjectCommandData.Prefix, (ProjectKind)99, ProjectCommandData.Color, ProjectCommandData.GetSetupFlowStateDtos());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_DescriptionExceedsMaxLength()
    {
        string longDescription = new('A', Project.MaxDescriptionLength + 1);
        var command = new SetupProjectCommand(
            ProjectCommandData.Name, longDescription, ProjectCommandData.Prefix, ProjectCommandData.Kind, ProjectCommandData.Color, ProjectCommandData.GetSetupFlowStateDtos());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_FlowStatesIsEmpty()
    {
        IReadOnlyCollection<SetupProjectFlowStateDto> flowStates = [];
        var command = new SetupProjectCommand(
            ProjectCommandData.Name, null, ProjectCommandData.Prefix, ProjectCommandData.Kind, ProjectCommandData.Color, flowStates);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_FlowStatesHasNoCompletedState()
    {
        IReadOnlyCollection<SetupProjectFlowStateDto> flowStates =
        [
            new("Todo", FlowStateCategory.Active, "white", [ProjectRole.Developer]),
            new("Cancelled", FlowStateCategory.Cancelled, "white", [ProjectRole.Developer])
        ];
        var command = new SetupProjectCommand(
            ProjectCommandData.Name, null, ProjectCommandData.Prefix, ProjectCommandData.Kind, ProjectCommandData.Color, flowStates);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_FlowStatesHasNoCancelledState()
    {
        IReadOnlyCollection<SetupProjectFlowStateDto> flowStates =
        [
            new("Todo", FlowStateCategory.Active, "white", [ProjectRole.Developer]),
            new("Done", FlowStateCategory.Completed, "white", [ProjectRole.Developer])
        ];
        var command = new SetupProjectCommand(
            ProjectCommandData.Name, null, ProjectCommandData.Prefix, ProjectCommandData.Kind, ProjectCommandData.Color, flowStates);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_FlowStatesAreOutOfOrder()
    {
        IReadOnlyCollection<SetupProjectFlowStateDto> flowStates =
        [
            new("Done", FlowStateCategory.Completed, "white", [ProjectRole.Developer]),
            new("Todo", FlowStateCategory.Active, "white", [ProjectRole.Developer]),
            new("Cancelled", FlowStateCategory.Cancelled, "white", [ProjectRole.Developer])
        ];
        var command = new SetupProjectCommand(
            ProjectCommandData.Name, null, ProjectCommandData.Prefix, ProjectCommandData.Kind, ProjectCommandData.Color, flowStates);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_StateNameIsEmpty()
    {
        IReadOnlyCollection<SetupProjectFlowStateDto> flowStates =
        [
            new(string.Empty, FlowStateCategory.Active, "white", [ProjectRole.Developer]),
            new("Done", FlowStateCategory.Completed, "white", [ProjectRole.Developer]),
            new("Cancelled", FlowStateCategory.Cancelled, "white", [ProjectRole.Developer])
        ];
        var command = new SetupProjectCommand(
            ProjectCommandData.Name, null, ProjectCommandData.Prefix, ProjectCommandData.Kind, ProjectCommandData.Color, flowStates);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_StateNameExceedsMaxLength()
    {
        string longStateName = new('A', FlowState.MaxNameLength + 1);
        IReadOnlyCollection<SetupProjectFlowStateDto> flowStates =
        [
            new(longStateName, FlowStateCategory.Active, "white", [ProjectRole.Developer]),
            new("Done", FlowStateCategory.Completed, "white", [ProjectRole.Developer]),
            new("Cancelled", FlowStateCategory.Cancelled, "white", [ProjectRole.Developer])
        ];
        var command = new SetupProjectCommand(
            ProjectCommandData.Name, null, ProjectCommandData.Prefix, ProjectCommandData.Kind, ProjectCommandData.Color, flowStates);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_StateRolesIsEmpty()
    {
        IReadOnlyCollection<SetupProjectFlowStateDto> flowStates =
        [
            new("Todo", FlowStateCategory.Active, "white", []),
            new("Done", FlowStateCategory.Completed, "white", [ProjectRole.Developer]),
            new("Cancelled", FlowStateCategory.Cancelled, "white", [ProjectRole.Developer])
        ];
        var command = new SetupProjectCommand(
            ProjectCommandData.Name, null, ProjectCommandData.Prefix, ProjectCommandData.Kind, ProjectCommandData.Color, flowStates);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
