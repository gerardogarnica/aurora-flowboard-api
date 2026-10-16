namespace Aurora.Flowboard.Domain.UnitTests.Projects;

public sealed class FlowStateDefinitionTests
{
    [Fact]
    public void Should_CreateDefinition_When_DataIsValid()
    {
        // Act
        Result<FlowStateDefinition> result = FlowStateDefinition.Create(
            ProjectData.FlowStateName,
            FlowStateCategory.Active,
            ProjectData.FlowStateColor,
            ProjectData.AllRoles);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Name.Should().Be(ProjectData.FlowStateName);
        result.Value.Category.Should().Be(FlowStateCategory.Active);
        result.Value.Color.Should().Be(ProjectData.FlowStateColor);
        result.Value.AllowedRoles.Should().BeEquivalentTo(ProjectData.AllRoles);
    }

    [Fact]
    public void Should_KeepAllowedRoles_When_SourceCollectionChangesAfterCreate()
    {
        // Arrange
        List<ProjectRole> allowedRoles = [ProjectRole.Developer];
        FlowStateDefinition definition = FlowStateDefinition.Create(
            ProjectData.FlowStateName,
            FlowStateCategory.Active,
            ProjectData.FlowStateColor,
            allowedRoles).Value;

        // Act
        allowedRoles.Add(ProjectRole.Viewer);

        // Assert
        definition.AllowedRoles.Should().Equal(ProjectRole.Developer);
    }

    [Fact]
    public void Should_Fail_When_AllowedRolesContainViewer()
    {
        // Act
        Result<FlowStateDefinition> result = FlowStateDefinition.Create(
            ProjectData.FlowStateName,
            FlowStateCategory.Active,
            ProjectData.FlowStateColor,
            [ProjectRole.Developer, ProjectRole.Viewer]);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.FlowViewerRoleNotAllowed);
    }
}
