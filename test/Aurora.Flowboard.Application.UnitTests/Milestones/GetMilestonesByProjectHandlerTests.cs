namespace Aurora.Flowboard.Application.UnitTests.Milestones;

public sealed class GetMilestonesByProjectHandlerTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IUserContext _userContext;
    private readonly GetMilestonesByProjectHandler _handler;

    public GetMilestonesByProjectHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _userContext = Substitute.For<IUserContext>();
        _handler = new GetMilestonesByProjectHandler(_dbContext, _userContext);
    }

    [Fact]
    public async Task Should_ReturnNotFoundError_When_ProjectDoesNotExist()
    {
        // Arrange
        _userContext.UserId.Returns(Guid.NewGuid());
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Project>());
        _dbContext.Projects.Returns(projectsMock);

        // Act
        Result<IReadOnlyCollection<MilestoneResponse>> result =
            await _handler.Handle(new GetMilestonesByProjectQuery(Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.NotFound);
    }

    [Fact]
    public async Task Should_ReturnNotFoundError_When_UserIsNotMember()
    {
        // Arrange
        User admin = MilestoneQueryData.GetAdminUser();
        User other = MilestoneQueryData.GetOtherUser();
        Project project = MilestoneQueryData.GetProjectWithMilestones(admin, "Phase 1");
        _userContext.UserId.Returns(other.Id);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        _dbContext.Projects.Returns(projectsMock);

        // Act
        Result<IReadOnlyCollection<MilestoneResponse>> result =
            await _handler.Handle(new GetMilestonesByProjectQuery(project.Id), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.NotFound);
    }

    [Fact]
    public async Task Should_ReturnEmptyCollection_When_ProjectHasNoMilestones()
    {
        // Arrange
        User admin = MilestoneQueryData.GetAdminUser();
        Project project = MilestoneQueryData.GetProject(admin);
        _userContext.UserId.Returns(admin.Id);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Milestone>());
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Milestones.Returns(milestonesMock);

        // Act
        Result<IReadOnlyCollection<MilestoneResponse>> result =
            await _handler.Handle(new GetMilestonesByProjectQuery(project.Id), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_MapAllFields_When_ProjectHasMilestones()
    {
        // Arrange
        User admin = MilestoneQueryData.GetAdminUser();
        Project project = MilestoneQueryData.GetProjectWithMilestones(admin, "Phase 1");
        Milestone milestone = project.Milestones.Single();
        _userContext.UserId.Returns(admin.Id);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet(project.Milestones);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Milestones.Returns(milestonesMock);

        // Act
        Result<IReadOnlyCollection<MilestoneResponse>> result =
            await _handler.Handle(new GetMilestonesByProjectQuery(project.Id), CancellationToken.None);

        // Assert
        MilestoneResponse response = result.Value.Single();
        response.Id.Should().Be(milestone.Id);
        response.Name.Should().Be("Phase 1");
        response.Status.Should().Be(MilestoneStatus.Draft);
        response.CreatedBy.Should().Be(admin.Id);
        response.CreatedOnUtc.Should().Be(MilestoneQueryData.UtcNow);
        response.UpdatedOnUtc.Should().BeNull();
    }

    [Fact]
    public async Task Should_ReturnMilestonesOrderedByName()
    {
        // Arrange
        User admin = MilestoneQueryData.GetAdminUser();
        Project project = MilestoneQueryData.GetProjectWithMilestones(admin, "Phase 2", "Phase 0", "Phase 1");
        _userContext.UserId.Returns(admin.Id);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        DbSet<Milestone> milestonesMock = MockDbSetHelper.CreateMockDbSet(project.Milestones);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.Milestones.Returns(milestonesMock);

        // Act
        Result<IReadOnlyCollection<MilestoneResponse>> result =
            await _handler.Handle(new GetMilestonesByProjectQuery(project.Id), CancellationToken.None);

        // Assert
        result.Value.Select(m => m.Name).Should().ContainInOrder("Phase 0", "Phase 1", "Phase 2");
    }
}
