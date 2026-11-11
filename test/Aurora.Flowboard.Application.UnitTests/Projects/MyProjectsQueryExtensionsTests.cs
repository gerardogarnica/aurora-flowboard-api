using Aurora.Flowboard.Application.Projects.Shared;
using Aurora.Flowboard.Application.UnitTests.Users;

namespace Aurora.Flowboard.Application.UnitTests.Projects;

public sealed class MyProjectsQueryExtensionsTests
{
    // The "accepts open work" rule is written three times: AcceptsOpenWork (in memory),
    // WhereMyActiveProjects (translated to SQL) and Project.IsModifiable (Domain, internal; observed here
    // through Component.Create). Iterating the enum makes a new ProjectStatus fail here until all three agree.
    [Fact]
    public void Should_AgreeWithProjectIsModifiable_ForEveryProjectStatus()
    {
        User admin = GetMySummaryQueryData.GetUser();

        foreach (ProjectStatus status in Enum.GetValues<ProjectStatus>())
        {
            // Arrange
            Project project = GetMySummaryQueryData.CreateProject("Rule Project", "RUL", status, admin);

            // Act
            bool acceptsOpenWork = MyProjectsQueryExtensions.AcceptsOpenWork(status);
            bool isMyActiveProject = new[] { project }.AsQueryable().WhereMyActiveProjects(admin.Id).Any();
            bool isModifiable = Component.Create("Rule Component", project, admin, GetMySummaryQueryData.UtcNow)
                .IsSuccessful;

            // Assert
            project.Status.Should().Be(status);
            isMyActiveProject.Should().Be(acceptsOpenWork, "WhereMyActiveProjects must match AcceptsOpenWork for {0}", status);
            isModifiable.Should().Be(acceptsOpenWork, "Project.IsModifiable must match AcceptsOpenWork for {0}", status);
        }
    }
}
