using Aurora.Flowboard.Application.Users.GetMySummary;

namespace Aurora.Flowboard.Application.UnitTests.Users;

public sealed class GetMySummaryHandlerTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly GetMySummaryHandler _handler;

    public GetMySummaryHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _handler = new GetMySummaryHandler(_dbContext);
    }

    [Fact]
    public async Task Should_ReturnNotFoundError_When_UserDoesNotExist()
    {
        // Arrange
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<User>());
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Project>());
        _dbContext.Users.Returns(usersMock);
        _dbContext.Projects.Returns(projectsMock);

        // Act
        Result<MySummaryResponse> result =
            await _handler.Handle(new GetMySummaryQuery(Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(UserErrors.NotFound);
    }

    [Fact]
    public async Task Should_MapMeFields_When_UserExists()
    {
        // Arrange
        User caller = GetMySummaryQueryData.GetUser();
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([caller]);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Project>());
        _dbContext.Users.Returns(usersMock);
        _dbContext.Projects.Returns(projectsMock);

        // Act
        Result<MySummaryResponse> result =
            await _handler.Handle(new GetMySummaryQuery(caller.Id), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        MyProfileResponse me = result.Value.Me;
        me.UserId.Should().Be(caller.Id);
        me.FullName.Should().Be(caller.FullName);
        me.Initials.Should().Be(caller.Initials);
        me.Email.Should().Be(caller.Email.Value);
        me.Role.Should().Be(Role.Member.Name);
    }

    [Fact]
    public async Task Should_MapRole_When_UserIsAdministrator()
    {
        // Arrange
        User caller = GetMySummaryQueryData.GetUser(role: Role.Administrator);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([caller]);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Project>());
        _dbContext.Users.Returns(usersMock);
        _dbContext.Projects.Returns(projectsMock);

        // Act
        Result<MySummaryResponse> result =
            await _handler.Handle(new GetMySummaryQuery(caller.Id), CancellationToken.None);

        // Assert
        result.Value.Me.Role.Should().Be(Role.Administrator.Name);
    }

    [Fact]
    public async Task Should_ReturnEmptySummary_When_UserHasNoProjectMemberships()
    {
        // Arrange
        User caller = GetMySummaryQueryData.GetUser();
        User other = GetMySummaryQueryData.GetUser("Other", "User");
        Project otherProject = GetMySummaryQueryData.CreateProject("Other Project", "OTP", ProjectStatus.Active, other);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([caller]);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([otherProject]);
        _dbContext.Users.Returns(usersMock);
        _dbContext.Projects.Returns(projectsMock);

        // Act
        Result<MySummaryResponse> result =
            await _handler.Handle(new GetMySummaryQuery(caller.Id), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Counts.Projects.Should().Be(0);
        result.Value.Counts.Members.Should().Be(0);
        result.Value.Counts.MyOpenIssues.Should().Be(0);
        result.Value.Counts.InboxUnread.Should().Be(0);
        result.Value.Projects.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_AlwaysReturnZero_ForInboxUnread()
    {
        // Arrange
        User caller = GetMySummaryQueryData.GetUser();
        Project project = GetMySummaryQueryData.CreateProject("Aurora Web", "AWB", ProjectStatus.Active, caller);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([caller]);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        _dbContext.Users.Returns(usersMock);
        _dbContext.Projects.Returns(projectsMock);

        // Act
        Result<MySummaryResponse> result =
            await _handler.Handle(new GetMySummaryQuery(caller.Id), CancellationToken.None);

        // Assert
        result.Value.Counts.InboxUnread.Should().Be(0);
    }

    [Fact]
    public async Task Should_ExcludeArchivedProjects_FromCountsAndList()
    {
        // Arrange
        User caller = GetMySummaryQueryData.GetUser();
        Project activeProject = GetMySummaryQueryData.CreateProject("Active Project", "ACT", ProjectStatus.Active, caller);
        Project archivedProject = GetMySummaryQueryData.CreateProject("Archived Project", "ARC", ProjectStatus.Archived, caller);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([caller]);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([activeProject, archivedProject]);
        _dbContext.Users.Returns(usersMock);
        _dbContext.Projects.Returns(projectsMock);

        // Act
        Result<MySummaryResponse> result =
            await _handler.Handle(new GetMySummaryQuery(caller.Id), CancellationToken.None);

        // Assert
        result.Value.Counts.Projects.Should().Be(1);
        result.Value.Projects.Should().ContainSingle(p => p.ProjectId == activeProject.Id);
    }

    [Fact]
    public async Task Should_CountDistinctMembers_AcrossAllNonArchivedProjects_NotSummedPerProject()
    {
        // Arrange
        User caller = GetMySummaryQueryData.GetUser();
        User sharedMember = GetMySummaryQueryData.GetUser("Shared", "Member");
        User onlyInFirst = GetMySummaryQueryData.GetUser("Only", "First");
        Project firstProject = GetMySummaryQueryData.CreateProject(
            "First Project", "FST", ProjectStatus.Active, caller, sharedMember, onlyInFirst);
        Project secondProject = GetMySummaryQueryData.CreateProject(
            "Second Project", "SND", ProjectStatus.Active, caller, sharedMember);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([caller]);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([firstProject, secondProject]);
        _dbContext.Users.Returns(usersMock);
        _dbContext.Projects.Returns(projectsMock);

        // Act
        Result<MySummaryResponse> result =
            await _handler.Handle(new GetMySummaryQuery(caller.Id), CancellationToken.None);

        // Assert: caller + sharedMember + onlyInFirst = 3 distinct people, not 3 + 2 = 5
        result.Value.Counts.Members.Should().Be(3);
    }

    [Fact]
    public async Task Should_CountOpenIssues_OnlyForActiveFlowStateAssignedToCaller()
    {
        // Arrange
        User caller = GetMySummaryQueryData.GetUser();
        User other = GetMySummaryQueryData.GetUser("Other", "User");
        Project project = GetMySummaryQueryData.CreateProject("Work Project", "WRK", ProjectStatus.Active, caller, other);
        GetMySummaryQueryData.AddWorkItem(project, caller.Id, FlowStateCategory.Active);
        GetMySummaryQueryData.AddWorkItem(project, caller.Id, FlowStateCategory.Active);
        GetMySummaryQueryData.AddWorkItem(project, caller.Id, FlowStateCategory.Completed);
        GetMySummaryQueryData.AddWorkItem(project, caller.Id, FlowStateCategory.Cancelled);
        GetMySummaryQueryData.AddWorkItem(project, other.Id, FlowStateCategory.Active);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([caller]);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        _dbContext.Users.Returns(usersMock);
        _dbContext.Projects.Returns(projectsMock);

        // Act
        Result<MySummaryResponse> result =
            await _handler.Handle(new GetMySummaryQuery(caller.Id), CancellationToken.None);

        // Assert
        result.Value.Counts.MyOpenIssues.Should().Be(2);
    }

    [Fact]
    public async Task Should_ExcludeOpenIssues_InArchivedProjects()
    {
        // Arrange
        User caller = GetMySummaryQueryData.GetUser();
        Project archivedProject = GetMySummaryQueryData.CreateProject("Archived Project", "ARW", ProjectStatus.Archived, caller);
        GetMySummaryQueryData.AddWorkItem(archivedProject, caller.Id, FlowStateCategory.Active);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([caller]);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([archivedProject]);
        _dbContext.Users.Returns(usersMock);
        _dbContext.Projects.Returns(projectsMock);

        // Act
        Result<MySummaryResponse> result =
            await _handler.Handle(new GetMySummaryQuery(caller.Id), CancellationToken.None);

        // Assert
        result.Value.Counts.MyOpenIssues.Should().Be(0);
    }

    [Fact]
    public async Task Should_OrderProjects_ByStatusThenAlphabeticallyByName()
    {
        // Arrange
        User caller = GetMySummaryQueryData.GetUser();
        Project zetaActive = GetMySummaryQueryData.CreateProject("Zeta Active", "ZAC", ProjectStatus.Active, caller);
        Project alphaActive = GetMySummaryQueryData.CreateProject("Alpha Active", "AAC", ProjectStatus.Active, caller);
        Project maintenance = GetMySummaryQueryData.CreateProject("Middle Maintenance", "MMT", ProjectStatus.Maintenance, caller);
        Project completed = GetMySummaryQueryData.CreateProject("Done Completed", "DCM", ProjectStatus.Completed, caller);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([caller]);
        DbSet<Project> projectsMock =
            MockDbSetHelper.CreateMockDbSet([completed, maintenance, zetaActive, alphaActive]);
        _dbContext.Users.Returns(usersMock);
        _dbContext.Projects.Returns(projectsMock);

        // Act
        Result<MySummaryResponse> result =
            await _handler.Handle(new GetMySummaryQuery(caller.Id), CancellationToken.None);

        // Assert
        result.Value.Projects.Select(p => p.Name).Should().ContainInOrder(
            "Alpha Active", "Zeta Active", "Middle Maintenance", "Done Completed");
    }

    [Fact]
    public async Task Should_MapProjectFields_When_ProjectExists()
    {
        // Arrange
        User caller = GetMySummaryQueryData.GetUser();
        Project project = GetMySummaryQueryData.CreateProject("Aurora Web", "AWB", ProjectStatus.Active, caller);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([caller]);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([project]);
        _dbContext.Users.Returns(usersMock);
        _dbContext.Projects.Returns(projectsMock);

        // Act
        Result<MySummaryResponse> result =
            await _handler.Handle(new GetMySummaryQuery(caller.Id), CancellationToken.None);

        // Assert
        MyProjectSummaryResponse summary = result.Value.Projects.Single();
        summary.ProjectId.Should().Be(project.Id);
        summary.Name.Should().Be("Aurora Web");
        summary.Color.Should().Be(project.Color.Value);
        summary.Status.Should().Be(ProjectStatus.Active);
    }
}
