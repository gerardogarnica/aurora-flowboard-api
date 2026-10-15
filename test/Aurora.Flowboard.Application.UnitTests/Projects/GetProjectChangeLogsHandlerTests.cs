using Aurora.Flowboard.Application.Abstractions.Pagination;

namespace Aurora.Flowboard.Application.UnitTests.Projects;

public sealed class GetProjectChangeLogsHandlerTests
{
    private const int Page = PaginationDefaults.DefaultPage;
    private const int PageSize = PaginationDefaults.DefaultPageSize;

    private readonly IApplicationDbContext _dbContext;
    private readonly IUserContext _userContext;
    private readonly GetProjectChangeLogsHandler _handler;

    public GetProjectChangeLogsHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _userContext = Substitute.For<IUserContext>();
        _handler = new GetProjectChangeLogsHandler(_dbContext, _userContext);
    }

    [Fact]
    public async Task Should_ReturnNotFoundError_When_ProjectDoesNotExist()
    {
        // Arrange
        User admin = ProjectQueryData.GetAdminUser();
        _userContext.UserId.Returns(admin.Id);
        ArrangeDbSets(Array.Empty<Project>(), [admin]);

        // Act
        Result<PagedResponse<ProjectChangeLogResponse>> result =
            await _handler.Handle(new GetProjectChangeLogsQuery(Guid.NewGuid(), Page, PageSize), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.NotFound);
    }

    [Fact]
    public async Task Should_ReturnNotFoundError_When_UserIsNotMember()
    {
        // Arrange
        User admin = ProjectQueryData.GetAdminUser();
        User member = ProjectQueryData.GetMemberUser();
        Project project = ProjectQueryData.GetProjectWithChangeLogHistory(admin, member);
        _userContext.UserId.Returns(member.Id);
        ArrangeDbSets([project], [admin, member]);

        // Act
        Result<PagedResponse<ProjectChangeLogResponse>> result =
            await _handler.Handle(new GetProjectChangeLogsQuery(project.Id, Page, PageSize), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.NotFound);
    }

    [Fact]
    public async Task Should_MapChangeLogFields_When_ProjectHasChangeLogs()
    {
        // Arrange
        User admin = ProjectQueryData.GetAdminUser();
        User member = ProjectQueryData.GetMemberUser();
        Project project = ProjectQueryData.GetProjectWithChangeLogHistory(admin, member);
        _userContext.UserId.Returns(admin.Id);
        ArrangeDbSets([project], [admin, member]);

        // Act
        Result<PagedResponse<ProjectChangeLogResponse>> result =
            await _handler.Handle(new GetProjectChangeLogsQuery(project.Id, Page, PageSize), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        ProjectChangeLogResponse created = result.Value.Items.Single(cl => cl.ChangeType == ProjectChangeType.Created);
        created.Id.Should().Be(project.ChangeLogs.Single(cl => cl.ChangeType == ProjectChangeType.Created).Id);
        created.ChangedById.Should().Be(admin.Id);
        created.ChangedByFullName.Should().Be(admin.FullName);
        created.ChangedByInitials.Should().Be(admin.Initials);
        created.AffectedEntityId.Should().BeNull();
        created.AffectedEntityName.Should().BeNull();
        created.NewStatus.Should().BeNull();
        created.ChangedOnUtc.Should().Be(ProjectQueryData.UtcNow);

        ProjectChangeLogResponse statusChanged = result.Value.Items.Single(cl => cl.ChangeType == ProjectChangeType.StatusChanged);
        statusChanged.NewStatus.Should().Be(ProjectStatus.Maintenance);
    }

    [Fact]
    public async Task Should_ResolveAffectedUserName_When_MemberWasAddedAndLaterRemoved()
    {
        // Arrange
        User admin = ProjectQueryData.GetAdminUser();
        User member = ProjectQueryData.GetMemberUser();
        Project project = ProjectQueryData.GetProjectWithChangeLogHistory(admin, member);
        _userContext.UserId.Returns(admin.Id);
        ArrangeDbSets([project], [admin, member]);

        // Act
        Result<PagedResponse<ProjectChangeLogResponse>> result =
            await _handler.Handle(new GetProjectChangeLogsQuery(project.Id, Page, PageSize), CancellationToken.None);

        // Assert
        ProjectChangeLogResponse added = result.Value.Items.Single(cl => cl.ChangeType == ProjectChangeType.MemberAdded);
        ProjectChangeLogResponse removed = result.Value.Items.Single(cl => cl.ChangeType == ProjectChangeType.MemberRemoved);
        added.AffectedEntityId.Should().Be(member.Id);
        added.AffectedEntityName.Should().Be(member.FullName);
        removed.AffectedEntityId.Should().Be(member.Id);
        removed.AffectedEntityName.Should().Be(member.FullName);
    }

    [Fact]
    public async Task Should_ReturnDifferentNewestFirstItems_When_PagingAcrossPageBoundary()
    {
        // Arrange
        User admin = ProjectQueryData.GetAdminUser();
        User member = ProjectQueryData.GetMemberUser();
        Project project = ProjectQueryData.GetProjectWithChangeLogHistory(admin, member);
        _userContext.UserId.Returns(admin.Id);
        ArrangeDbSets([project], [admin, member]);

        // Act
        Result<PagedResponse<ProjectChangeLogResponse>> firstPage =
            await _handler.Handle(new GetProjectChangeLogsQuery(project.Id, 1, 2), CancellationToken.None);
        Result<PagedResponse<ProjectChangeLogResponse>> secondPage =
            await _handler.Handle(new GetProjectChangeLogsQuery(project.Id, 2, 2), CancellationToken.None);

        // Assert
        firstPage.Value.Items.Select(cl => cl.ChangeType).Should()
            .Equal(ProjectChangeType.StatusChanged, ProjectChangeType.MemberRemoved);
        secondPage.Value.Items.Select(cl => cl.ChangeType).Should()
            .Equal(ProjectChangeType.MemberAdded, ProjectChangeType.Created);
        firstPage.Value.TotalCount.Should().Be(4);
        secondPage.Value.TotalCount.Should().Be(4);
        firstPage.Value.PageSize.Should().Be(2);
        secondPage.Value.Page.Should().Be(2);
    }

    [Fact]
    public async Task Should_ReturnEmptyItemsWithTotalCount_When_PageIsPastTheEnd()
    {
        // Arrange
        User admin = ProjectQueryData.GetAdminUser();
        User member = ProjectQueryData.GetMemberUser();
        Project project = ProjectQueryData.GetProjectWithChangeLogHistory(admin, member);
        _userContext.UserId.Returns(admin.Id);
        ArrangeDbSets([project], [admin, member]);

        // Act
        Result<PagedResponse<ProjectChangeLogResponse>> result =
            await _handler.Handle(new GetProjectChangeLogsQuery(project.Id, 5, 2), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(4);
    }

    [Fact]
    public async Task Should_ReturnOnlyChangeLogsOfRequestedProject()
    {
        // Arrange
        User admin = ProjectQueryData.GetAdminUser();
        User member = ProjectQueryData.GetMemberUser();
        Project project = ProjectQueryData.GetProjectWithChangeLogHistory(admin, member);
        Project otherProject = ProjectQueryData.GetProjectWithChangeLogHistory(admin, member);
        _userContext.UserId.Returns(admin.Id);
        ArrangeDbSets([project, otherProject], [admin, member]);

        // Act
        Result<PagedResponse<ProjectChangeLogResponse>> result =
            await _handler.Handle(new GetProjectChangeLogsQuery(project.Id, Page, PageSize), CancellationToken.None);

        // Assert
        result.Value.TotalCount.Should().Be(project.ChangeLogs.Count);
        result.Value.Items.Select(cl => cl.Id).Should().BeEquivalentTo(project.ChangeLogs.Select(cl => cl.Id));
    }

    private void ArrangeDbSets(IReadOnlyCollection<Project> projects, User[] users)
    {
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet(projects);
        DbSet<ProjectChangeLog> changeLogsMock = MockDbSetHelper.CreateMockDbSet(projects.SelectMany(p => p.ChangeLogs));
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(users);
        _dbContext.Projects.Returns(projectsMock);
        _dbContext.ProjectChangeLogs.Returns(changeLogsMock);
        _dbContext.Users.Returns(usersMock);
    }
}
