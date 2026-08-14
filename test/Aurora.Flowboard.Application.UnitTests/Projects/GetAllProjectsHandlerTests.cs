using Aurora.Flowboard.Application.Projects.GetAll;

namespace Aurora.Flowboard.Application.UnitTests.Projects;

public sealed class GetAllProjectsHandlerTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IUserContext _userContext;
    private readonly GetAllProjectsHandler _handler;

    public GetAllProjectsHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _userContext = Substitute.For<IUserContext>();
        _handler = new GetAllProjectsHandler(_dbContext, _userContext);
    }

    [Fact]
    public async Task Should_ReturnOnlyUserProjects_When_UserIsMemberOfSomeProjects()
    {
        // Arrange
        User admin = ProjectQueryData.GetAdminUser();
        User other = ProjectQueryData.GetOtherUser();
        Project userProject = ProjectQueryData.GetProjectForGetAll("My Project", admin);
        Project otherProject = ProjectQueryData.GetProjectForGetAll("Other Project", other);
        _userContext.UserId.Returns(admin.Id);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([userProject, otherProject]);
        _dbContext.Projects.Returns(projectsMock);

        // Act
        Result<IReadOnlyCollection<ProjectSummaryResponse>> result =
            await _handler.Handle(new GetAllProjectsQuery(null), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.Single().ProjectId.Should().Be(userProject.Id);
    }

    [Fact]
    public async Task Should_ReturnFilteredProjects_When_StatusFilterIsProvided()
    {
        // Arrange
        User admin = ProjectQueryData.GetAdminUser();
        Project archivedProject = ProjectQueryData.GetArchivedProjectForGetAll("Archived Project", admin);
        Project activeProject = ProjectQueryData.GetActiveProjectForGetAll("Active Project", admin);
        _userContext.UserId.Returns(admin.Id);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([archivedProject, activeProject]);
        _dbContext.Projects.Returns(projectsMock);

        // Act
        Result<IReadOnlyCollection<ProjectSummaryResponse>> result =
            await _handler.Handle(new GetAllProjectsQuery(ProjectStatus.Active), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.Single().Status.Should().Be(ProjectStatus.Active);
    }

    [Fact]
    public async Task Should_ReturnAllMemberProjects_When_StatusFilterIsNull()
    {
        // Arrange
        User admin = ProjectQueryData.GetAdminUser();
        Project archivedProject = ProjectQueryData.GetArchivedProjectForGetAll("Archived Project", admin);
        Project activeProject = ProjectQueryData.GetActiveProjectForGetAll("Active Project", admin);
        _userContext.UserId.Returns(admin.Id);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([archivedProject, activeProject]);
        _dbContext.Projects.Returns(projectsMock);

        // Act
        Result<IReadOnlyCollection<ProjectSummaryResponse>> result =
            await _handler.Handle(new GetAllProjectsQuery(null), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Should_ReturnEmptyList_When_UserHasNoProjectMemberships()
    {
        // Arrange
        User admin = ProjectQueryData.GetAdminUser();
        User other = ProjectQueryData.GetOtherUser();
        Project otherProject = ProjectQueryData.GetProjectForGetAll("Other Project", other);
        _userContext.UserId.Returns(admin.Id);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([otherProject]);
        _dbContext.Projects.Returns(projectsMock);

        // Act
        Result<IReadOnlyCollection<ProjectSummaryResponse>> result =
            await _handler.Handle(new GetAllProjectsQuery(null), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_ReturnProjectsOrderedByName()
    {
        // Arrange
        User admin = ProjectQueryData.GetAdminUser();
        Project zetaProject = ProjectQueryData.GetProjectForGetAll("Zeta Project", admin);
        Project alphaProject = ProjectQueryData.GetProjectForGetAll("Alpha Project", admin);
        Project muProject = ProjectQueryData.GetProjectForGetAll("Mu Project", admin);
        _userContext.UserId.Returns(admin.Id);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([zetaProject, alphaProject, muProject]);
        _dbContext.Projects.Returns(projectsMock);

        // Act
        Result<IReadOnlyCollection<ProjectSummaryResponse>> result =
            await _handler.Handle(new GetAllProjectsQuery(null), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.Select(p => p.Name).Should().BeInAscendingOrder();
        result.Value.First().Name.Should().Be("Alpha Project");
    }

    [Fact]
    public async Task Should_MapAllResponseFields_When_ProjectExists()
    {
        // Arrange
        User admin = ProjectQueryData.GetAdminUser();
        Project project = ProjectQueryData.GetProjectForGetAll("Mapped Project", admin);
        _userContext.UserId.Returns(admin.Id);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        _dbContext.Projects.Returns(projectsMock);

        // Act
        Result<IReadOnlyCollection<ProjectSummaryResponse>> result =
            await _handler.Handle(new GetAllProjectsQuery(null), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        ProjectSummaryResponse summary = result.Value.Single();
        summary.ProjectId.Should().Be(project.Id);
        summary.Name.Should().Be("Mapped Project");
        summary.Description.Should().Be(ProjectQueryData.Description);
        summary.Kind.Should().Be(ProjectQueryData.Kind);
        summary.Status.Should().Be(ProjectStatus.Active);
        summary.Members.Should().HaveCount(1); // creator is added as admin member automatically
        summary.Members.Single().UserId.Should().Be(admin.Id);
        summary.Members.Single().FullName.Should().Be(admin.FullName);
        summary.Members.Single().Initials.Should().Be(admin.Initials);
        summary.Members.Single().Role.Should().Be(ProjectRole.Admin);
        summary.FlowStates.Should().BeEmpty();
        summary.OpenWorkItems.Should().Be(0);
        summary.ClosedWorkItems.Should().Be(0);
        summary.CanModifyFlowStates.Should().BeTrue();
        summary.CanAddOrUpdateWorkItems.Should().BeTrue();
    }

    [Fact]
    public async Task Should_AllowFlowsAndWorkItems_When_ProjectIsActive()
    {
        // Arrange
        User admin = ProjectQueryData.GetAdminUser();
        Project activeProject = ProjectQueryData.GetActiveProjectForGetAll("Active Project", admin);
        _userContext.UserId.Returns(admin.Id);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([activeProject]);
        _dbContext.Projects.Returns(projectsMock);

        // Act
        Result<IReadOnlyCollection<ProjectSummaryResponse>> result =
            await _handler.Handle(new GetAllProjectsQuery(null), CancellationToken.None);

        // Assert
        ProjectSummaryResponse summary = result.Value.Single();
        summary.CanModifyFlowStates.Should().BeTrue();
        summary.CanAddOrUpdateWorkItems.Should().BeTrue();
    }

    [Fact]
    public async Task Should_IncludeFlowStates_When_ProjectHasFlowStates()
    {
        // Arrange
        User admin = ProjectQueryData.GetAdminUser();
        Project project = ProjectQueryData.GetProjectForGetAllWithFlowStates("Flow Project", admin);
        _userContext.UserId.Returns(admin.Id);

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        _dbContext.Projects.Returns(projectsMock);

        // Act
        Result<IReadOnlyCollection<ProjectSummaryResponse>> result =
            await _handler.Handle(new GetAllProjectsQuery(null), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        IReadOnlyCollection<FlowStateResponse> flowStates = result.Value.Single().FlowStates;
        flowStates.Should().HaveCount(2);
        flowStates.Should().ContainSingle(s => s.Name == "Backlog" && s.Category == FlowStateCategory.Active);
        flowStates.Should().ContainSingle(s => s.Name == "Done" && s.Category == FlowStateCategory.Completed);
    }

    [Fact]
    public async Task Should_CountOpenAndClosedWorkItems_When_ProjectHasWorkItems()
    {
        // Arrange
        User admin = ProjectQueryData.GetAdminUser();
        Project project = ProjectQueryData.GetProjectForGetAllWithWorkItems("Count Project", admin, openCount: 3, closedCount: 2, cancelledCount: 1);
        _userContext.UserId.Returns(admin.Id);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        _dbContext.Projects.Returns(projectsMock);

        // Act
        Result<IReadOnlyCollection<ProjectSummaryResponse>> result =
            await _handler.Handle(new GetAllProjectsQuery(null), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        ProjectSummaryResponse summary = result.Value.Single();
        summary.OpenWorkItems.Should().Be(3);
        summary.ClosedWorkItems.Should().Be(2);
    }

    [Fact]
    public async Task Should_OnlyIncludeFlowStatesForMatchingProject()
    {
        // Arrange
        User admin = ProjectQueryData.GetAdminUser();
        Project projectA = ProjectQueryData.GetProjectForGetAllWithFlowStates("Project A", admin);
        Project projectB = ProjectQueryData.GetProjectForGetAll("Project B", admin);
        _userContext.UserId.Returns(admin.Id);

        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([projectA, projectB]);
        _dbContext.Projects.Returns(projectsMock);

        // Act
        Result<IReadOnlyCollection<ProjectSummaryResponse>> result =
            await _handler.Handle(new GetAllProjectsQuery(null), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        ProjectSummaryResponse summaryA = result.Value.First(p => p.Name == "Project A");
        ProjectSummaryResponse summaryB = result.Value.First(p => p.Name == "Project B");
        summaryA.FlowStates.Should().HaveCount(2);
        summaryB.FlowStates.Should().BeEmpty();
    }
}
