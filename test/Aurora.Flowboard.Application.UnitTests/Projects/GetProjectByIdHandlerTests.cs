namespace Aurora.Flowboard.Application.UnitTests.Projects;

public sealed class GetProjectByIdHandlerTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly GetProjectByIdHandler _handler;

    public GetProjectByIdHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _handler = new GetProjectByIdHandler(_dbContext);
    }

    [Fact]
    public async Task Should_ReturnNotFoundError_When_ProjectDoesNotExist()
    {
        // Arrange
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Project>());
        _dbContext.Projects.Returns(projectsMock);

        // Act
        Result<ProjectResponse> result =
            await _handler.Handle(new GetProjectByIdQuery(Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.NotFound);
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_ProjectExists()
    {
        // Arrange
        User admin = ProjectQueryData.GetAdminUser();
        Project project = ProjectQueryData.GetProjectWithNavProperties(admin);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        _dbContext.Projects.Returns(projectsMock);

        // Act
        Result<ProjectResponse> result =
            await _handler.Handle(new GetProjectByIdQuery(project.Id), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task Should_MapAllScalarFields_When_ProjectExists()
    {
        // Arrange
        User admin = ProjectQueryData.GetAdminUser();
        Project project = ProjectQueryData.GetProjectWithNavProperties(admin);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        _dbContext.Projects.Returns(projectsMock);

        // Act
        Result<ProjectResponse> result =
            await _handler.Handle(new GetProjectByIdQuery(project.Id), CancellationToken.None);

        // Assert
        ProjectResponse response = result.Value;
        response.ProjectId.Should().Be(project.Id);
        response.Name.Should().Be(project.Name);
        response.Description.Should().Be(project.Description);
        response.EstimatedCompletionDate.Should().Be(project.EstimatedCompletionDate);
        response.Status.Should().Be(project.Status);
        response.CreatedById.Should().Be(admin.Id);
        response.CreatedByFullName.Should().Be(admin.FullName);
        response.CreatedOnUtc.Should().Be(project.CreatedOnUtc);
        response.UpdatedOnUtc.Should().Be(project.UpdatedOnUtc);
    }

    [Fact]
    public async Task Should_MapMembersCollection_When_ProjectHasMembers()
    {
        // Arrange
        User admin = ProjectQueryData.GetAdminUser();
        User member = ProjectQueryData.GetMemberUser();
        Project project = ProjectQueryData.GetProjectWithMemberNavProperties(admin, member);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        _dbContext.Projects.Returns(projectsMock);

        // Act
        Result<ProjectResponse> result =
            await _handler.Handle(new GetProjectByIdQuery(project.Id), CancellationToken.None);

        // Assert
        result.Value.Members.Should().HaveCount(2);
        ProjectMemberResponse developer = result.Value.Members.Single(m => m.UserId == member.Id);
        developer.FullName.Should().Be(member.FullName);
        developer.Role.Should().Be(ProjectRole.Developer);
        developer.JoinedOnUtc.Should().Be(ProjectQueryData.UtcNow);
    }

    [Fact]
    public async Task Should_MapChangeLogsCollection_When_ProjectHasChangeLogs()
    {
        // Arrange
        User admin = ProjectQueryData.GetAdminUser();
        User member = ProjectQueryData.GetMemberUser();
        Project project = ProjectQueryData.GetProjectWithMemberNavProperties(admin, member);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        _dbContext.Projects.Returns(projectsMock);

        // Act
        Result<ProjectResponse> result =
            await _handler.Handle(new GetProjectByIdQuery(project.Id), CancellationToken.None);

        // Assert
        result.Value.ChangeLogs.Should().HaveCount(2);
        ProjectChangeLogResponse firstLog = result.Value.ChangeLogs.First();
        firstLog.ChangedByFullName.Should().Be(admin.FullName);
        firstLog.ChangeType.Should().Be(ProjectChangeType.Created);
    }

    [Fact]
    public async Task Should_ReturnMembersOrderedByFullName()
    {
        // Arrange
        User admin = ProjectQueryData.GetAdminUser();   // "Query Admin"
        User alpha = ProjectQueryData.GetAlphaUser();   // "Alpha User"
        User zeta = ProjectQueryData.GetZetaUser();     // "Zeta User"
        Project project = ProjectQueryData.GetProjectWithOrderedMembers(admin, alpha, zeta);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        _dbContext.Projects.Returns(projectsMock);

        // Act
        Result<ProjectResponse> result =
            await _handler.Handle(new GetProjectByIdQuery(project.Id), CancellationToken.None);

        // Assert
        result.Value.Members.Select(m => m.FullName).Should().BeInAscendingOrder();
        result.Value.Members.First().FullName.Should().Be("Alpha User");
    }

    [Fact]
    public async Task Should_ReturnChangeLogsOrderedByChangedOnUtc()
    {
        // Arrange
        User admin = ProjectQueryData.GetAdminUser();
        User member = ProjectQueryData.GetMemberUser();
        Project project = ProjectQueryData.GetProjectWithOrderedChangeLogs(admin, member);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        _dbContext.Projects.Returns(projectsMock);

        // Act
        Result<ProjectResponse> result =
            await _handler.Handle(new GetProjectByIdQuery(project.Id), CancellationToken.None);

        // Assert
        result.Value.ChangeLogs.Select(cl => cl.ChangedOnUtc).Should().BeInAscendingOrder();
        result.Value.ChangeLogs.First().ChangedOnUtc.Should().Be(ProjectQueryData.UtcNow);
        result.Value.ChangeLogs.Last().ChangedOnUtc.Should().Be(ProjectQueryData.UtcNow.AddHours(1));
    }
}
