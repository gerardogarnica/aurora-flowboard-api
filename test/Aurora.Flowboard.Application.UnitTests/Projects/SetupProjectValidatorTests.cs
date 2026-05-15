namespace Aurora.Flowboard.Application.UnitTests.Projects;

public sealed class SetupProjectValidatorTests
{
    private readonly SetupProjectValidator _validator;

    public SetupProjectValidatorTests()
    {
        IDateTimeProvider dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.Today.Returns(ProjectCommandData.Today);
        _validator = new SetupProjectValidator(dateTimeProvider);
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
            string.Empty, null, ProjectCommandData.Code, null, ProjectCommandData.GetSetupFlowDto());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_NameExceedsMaxLength()
    {
        string longName = new('A', Project.MaxNameLength + 1);
        var command = new SetupProjectCommand(
            longName, null, ProjectCommandData.Code, null, ProjectCommandData.GetSetupFlowDto());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_CodeIsEmpty()
    {
        var command = new SetupProjectCommand(
            ProjectCommandData.Name, null, string.Empty, null, ProjectCommandData.GetSetupFlowDto());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_CodeExceedsMaxLength()
    {
        string longCode = new('A', ProjectCode.MaxLength + 1);
        var command = new SetupProjectCommand(
            ProjectCommandData.Name, null, longCode, null, ProjectCommandData.GetSetupFlowDto());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_CodeContainsNonAlphabeticCharacters()
    {
        var command = new SetupProjectCommand(
            ProjectCommandData.Name, null, "A1B", null, ProjectCommandData.GetSetupFlowDto());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_DescriptionExceedsMaxLength()
    {
        string longDescription = new('A', Project.MaxDescriptionLength + 1);
        var command = new SetupProjectCommand(
            ProjectCommandData.Name, longDescription, ProjectCommandData.Code, null, ProjectCommandData.GetSetupFlowDto());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_EstimatedCompletionDateIsInThePast()
    {
        DateOnly pastDate = ProjectCommandData.Today.AddDays(-1);
        var command = new SetupProjectCommand(
            ProjectCommandData.Name, null, ProjectCommandData.Code, pastDate, ProjectCommandData.GetSetupFlowDto());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Pass_When_EstimatedCompletionDateIsNull()
    {
        var command = new SetupProjectCommand(
            ProjectCommandData.Name, null, ProjectCommandData.Code, null, ProjectCommandData.GetSetupFlowDto());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_FlowNameIsEmpty()
    {
        var flow = new SetupProjectFlowDto(string.Empty, null, ProjectCommandData.GetSetupFlowDto().States);
        var command = new SetupProjectCommand(
            ProjectCommandData.Name, null, ProjectCommandData.Code, null, flow);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_FlowNameExceedsMaxLength()
    {
        string longName = new('A', Flow.MaxNameLength + 1);
        var flow = new SetupProjectFlowDto(longName, null, ProjectCommandData.GetSetupFlowDto().States);
        var command = new SetupProjectCommand(
            ProjectCommandData.Name, null, ProjectCommandData.Code, null, flow);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_FlowDescriptionExceedsMaxLength()
    {
        string longDescription = new('A', Flow.MaxDescriptionLength + 1);
        var flow = new SetupProjectFlowDto("Sprint Flow", longDescription, ProjectCommandData.GetSetupFlowDto().States);
        var command = new SetupProjectCommand(
            ProjectCommandData.Name, null, ProjectCommandData.Code, null, flow);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_FlowStatesIsEmpty()
    {
        var flow = new SetupProjectFlowDto("Sprint Flow", null, []);
        var command = new SetupProjectCommand(
            ProjectCommandData.Name, null, ProjectCommandData.Code, null, flow);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_FlowStatesHasNoCompletedState()
    {
        var flow = new SetupProjectFlowDto("Sprint Flow", null,
        [
            new("Todo", FlowStateCategory.Active, [ProjectRole.Developer]),
            new("Cancelled", FlowStateCategory.Cancelled, [ProjectRole.Developer])
        ]);
        var command = new SetupProjectCommand(
            ProjectCommandData.Name, null, ProjectCommandData.Code, null, flow);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_FlowStatesHasNoCancelledState()
    {
        var flow = new SetupProjectFlowDto("Sprint Flow", null,
        [
            new("Todo", FlowStateCategory.Active, [ProjectRole.Developer]),
            new("Done", FlowStateCategory.Completed, [ProjectRole.Developer])
        ]);
        var command = new SetupProjectCommand(
            ProjectCommandData.Name, null, ProjectCommandData.Code, null, flow);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_FlowStatesAreOutOfOrder()
    {
        var flow = new SetupProjectFlowDto("Sprint Flow", null,
        [
            new("Done", FlowStateCategory.Completed, [ProjectRole.Developer]),
            new("Todo", FlowStateCategory.Active, [ProjectRole.Developer]),
            new("Cancelled", FlowStateCategory.Cancelled, [ProjectRole.Developer])
        ]);
        var command = new SetupProjectCommand(
            ProjectCommandData.Name, null, ProjectCommandData.Code, null, flow);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_StateNameIsEmpty()
    {
        var flow = new SetupProjectFlowDto("Sprint Flow", null,
        [
            new(string.Empty, FlowStateCategory.Active, [ProjectRole.Developer]),
            new("Done", FlowStateCategory.Completed, [ProjectRole.Developer]),
            new("Cancelled", FlowStateCategory.Cancelled, [ProjectRole.Developer])
        ]);
        var command = new SetupProjectCommand(
            ProjectCommandData.Name, null, ProjectCommandData.Code, null, flow);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_StateNameExceedsMaxLength()
    {
        string longStateName = new('A', FlowState.MaxNameLength + 1);
        var flow = new SetupProjectFlowDto("Sprint Flow", null,
        [
            new(longStateName, FlowStateCategory.Active, [ProjectRole.Developer]),
            new("Done", FlowStateCategory.Completed, [ProjectRole.Developer]),
            new("Cancelled", FlowStateCategory.Cancelled, [ProjectRole.Developer])
        ]);
        var command = new SetupProjectCommand(
            ProjectCommandData.Name, null, ProjectCommandData.Code, null, flow);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_StateRolesIsEmpty()
    {
        var flow = new SetupProjectFlowDto("Sprint Flow", null,
        [
            new("Todo", FlowStateCategory.Active, []),
            new("Done", FlowStateCategory.Completed, [ProjectRole.Developer]),
            new("Cancelled", FlowStateCategory.Cancelled, [ProjectRole.Developer])
        ]);
        var command = new SetupProjectCommand(
            ProjectCommandData.Name, null, ProjectCommandData.Code, null, flow);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
