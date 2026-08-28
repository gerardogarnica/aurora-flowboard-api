namespace Aurora.Flowboard.Application.UnitTests.Projects;

public sealed class GetProjectFlowHandlerTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IUserContext _userContext;
    private readonly GetProjectFlowHandler _handler;

    public GetProjectFlowHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _userContext = Substitute.For<IUserContext>();
        _handler = new GetProjectFlowHandler(_dbContext, _userContext);
    }

    [Fact]
    public async Task Should_ReturnNotFoundError_When_ProjectDoesNotExist()
    {
        // Arrange
        _userContext.UserId.Returns(Guid.NewGuid());
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Project>());
        _dbContext.Projects.Returns(projectsMock);

        GetProjectFlowQuery query = new(Guid.NewGuid());

        // Act
        Result<ProjectFlowResponse> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.NotFound);
    }

    [Fact]
    public async Task Should_ReturnNotFoundError_When_UserIsNotMember()
    {
        // Arrange
        User admin = ProjectQueryData.GetAdminUser();
        User other = ProjectQueryData.GetOtherUser();
        Project project = ProjectQueryData.GetActiveProject(admin);
        _userContext.UserId.Returns(other.Id);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        _dbContext.Projects.Returns(projectsMock);

        GetProjectFlowQuery query = new(project.Id);

        // Act
        Result<ProjectFlowResponse> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.NotFound);
    }

    [Fact]
    public async Task Should_ReturnEmptyStatesAndTransitions_When_ProjectHasNoFlowStates()
    {
        // Arrange
        User admin = ProjectQueryData.GetAdminUser();
        Project project = ProjectQueryData.GetActiveProject(admin);

        _userContext.UserId.Returns(admin.Id);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        _dbContext.Projects.Returns(projectsMock);

        GetProjectFlowQuery query = new(project.Id);

        // Act
        Result<ProjectFlowResponse> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.ProjectId.Should().Be(project.Id);
        result.Value.States.Should().BeEmpty();
        result.Value.Transitions.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_MapFlowStates_When_ProjectHasFlowStates()
    {
        // Arrange
        User admin = ProjectQueryData.GetAdminUser();
        Project project = ProjectQueryData.GetProjectWithFlowStates(admin);

        _userContext.UserId.Returns(admin.Id);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        _dbContext.Projects.Returns(projectsMock);

        GetProjectFlowQuery query = new(project.Id);

        // Act
        Result<ProjectFlowResponse> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.States.Should().HaveCount(3);
        result.Value.States.Should().Contain(s => s.Name == "Backlog" && s.Category == FlowStateCategory.Active);
        result.Value.States.Should().Contain(s => s.Name == "Done" && s.Category == FlowStateCategory.Completed);
        result.Value.States.Should().Contain(s => s.Name == "Cancelled" && s.Category == FlowStateCategory.Cancelled);
    }

    [Fact]
    public async Task Should_OrderStatesByCategoryThenSortOrder_When_ProjectHasFlowStates()
    {
        // Arrange
        User admin = ProjectQueryData.GetAdminUser();
        Project project = ProjectQueryData.GetProjectWithFlowStates(admin);

        _userContext.UserId.Returns(admin.Id);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        _dbContext.Projects.Returns(projectsMock);

        GetProjectFlowQuery query = new(project.Id);

        // Act
        Result<ProjectFlowResponse> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.States.Select(s => s.Category).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Should_ResolveTransitionStateNames_When_ProjectHasTransitions()
    {
        // Arrange
        User admin = ProjectQueryData.GetAdminUser();
        Project project = ProjectQueryData.GetProjectWithFlowStates(admin);

        _userContext.UserId.Returns(admin.Id);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        _dbContext.Projects.Returns(projectsMock);

        GetProjectFlowQuery query = new(project.Id);

        // Act
        Result<ProjectFlowResponse> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.Transitions.Should().NotBeEmpty();
        result.Value.Transitions.Should().OnlyContain(t =>
            !string.IsNullOrEmpty(t.FromStateName) && !string.IsNullOrEmpty(t.ToStateName));
    }

    [Fact]
    public async Task Should_MapAllowedRoles_When_TransitionHasRoles()
    {
        // Arrange
        User admin = ProjectQueryData.GetAdminUser();
        Project project = ProjectQueryData.GetProjectWithFlowStates(admin);

        _userContext.UserId.Returns(admin.Id);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        _dbContext.Projects.Returns(projectsMock);

        GetProjectFlowQuery query = new(project.Id);

        // Act
        Result<ProjectFlowResponse> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.Transitions.Should().OnlyContain(t => t.AllowedRoles.Contains(ProjectRole.Developer));
    }
}
