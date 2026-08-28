namespace Aurora.Flowboard.Application.UnitTests.Projects;

public sealed class CreateProjectHandlerTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUserContext _userContext;
    private readonly CreateProjectHandler _handler;

    public CreateProjectHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _userContext = Substitute.For<IUserContext>();
        _handler = new CreateProjectHandler(_dbContext, _dateTimeProvider, _userContext);
    }

    [Fact]
    public async Task Should_ReturnProjectId_When_CommandIsValid()
    {
        // Arrange
        User user = ProjectCommandData.GetUser();
        _userContext.UserId.Returns(user.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Project>());
        _dbContext.Users.Returns(usersMock);
        _dbContext.Projects.Returns(projectsMock);

        CreateProjectCommand command = ProjectCommandData.GetCreateCommand();

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Should_PersistProject_When_CommandIsValid()
    {
        // Arrange
        User user = ProjectCommandData.GetUser();
        _userContext.UserId.Returns(user.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Project>());
        _dbContext.Users.Returns(usersMock);
        _dbContext.Projects.Returns(projectsMock);

        CreateProjectCommand command = ProjectCommandData.GetCreateCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnUserNotFoundError_When_UserDoesNotExist()
    {
        // Arrange
        _userContext.UserId.Returns(Guid.NewGuid());

        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<User>());
        _dbContext.Users.Returns(usersMock);

        CreateProjectCommand command = ProjectCommandData.GetCreateCommand();

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(UserErrors.NotFound);
    }

    [Fact]
    public async Task Should_NotPersist_When_UserDoesNotExist()
    {
        // Arrange
        _userContext.UserId.Returns(Guid.NewGuid());

        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<User>());
        _dbContext.Users.Returns(usersMock);

        CreateProjectCommand command = ProjectCommandData.GetCreateCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnDomainError_When_ProjectCreationFails()
    {
        // Arrange
        User user = ProjectCommandData.GetUser();
        _userContext.UserId.Returns(user.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Project>());
        _dbContext.Users.Returns(usersMock);
        _dbContext.Projects.Returns(projectsMock);

        var command = new CreateProjectCommand(string.Empty, null, ProjectCommandData.Prefix, ProjectCommandData.Kind, ProjectCommandData.Color, []);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.NameRequired);
    }

    [Fact]
    public async Task Should_NotPersist_When_ProjectCreationFails()
    {
        // Arrange
        User user = ProjectCommandData.GetUser();
        _userContext.UserId.Returns(user.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Project>());
        _dbContext.Users.Returns(usersMock);
        _dbContext.Projects.Returns(projectsMock);

        var command = new CreateProjectCommand(string.Empty, null, ProjectCommandData.Prefix, ProjectCommandData.Kind, ProjectCommandData.Color, []);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnPrefixAlreadyExistsError_When_PrefixIsInUse()
    {
        // Arrange
        User user = ProjectCommandData.GetUser();
        _userContext.UserId.Returns(user.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        Project existingProject = ProjectCommandData.GetExistingProjectWithPrefix(user);

        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([existingProject]);
        _dbContext.Users.Returns(usersMock);
        _dbContext.Projects.Returns(projectsMock);

        CreateProjectCommand command = ProjectCommandData.GetCreateCommand();

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.PrefixAlreadyExists);
    }

    [Fact]
    public async Task Should_NotPersist_When_PrefixIsInUse()
    {
        // Arrange
        User user = ProjectCommandData.GetUser();
        _userContext.UserId.Returns(user.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        Project existingProject = ProjectCommandData.GetExistingProjectWithPrefix(user);

        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet([existingProject]);
        _dbContext.Users.Returns(usersMock);
        _dbContext.Projects.Returns(projectsMock);

        CreateProjectCommand command = ProjectCommandData.GetCreateCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_CreateProjectWithFlowStates_When_FlowStatesAreValid()
    {
        // Arrange
        User user = ProjectCommandData.GetUser();
        _userContext.UserId.Returns(user.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Project>());
        _dbContext.Users.Returns(usersMock);
        _dbContext.Projects.Returns(projectsMock);

        CreateProjectCommand command = ProjectCommandData.GetCreateCommand(ProjectCommandData.GetValidCreateProjectStates());

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();

        projectsMock.Received(1).Add(Arg.Is<Project>(p => p.FlowStates.Count == 3 && p.FlowTransitions.Count > 0));
    }

    [Fact]
    public async Task Should_ReturnDomainError_When_FlowStatesAreMissingRequiredCategory()
    {
        // Arrange
        User user = ProjectCommandData.GetUser();
        _userContext.UserId.Returns(user.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Project>());
        _dbContext.Users.Returns(usersMock);
        _dbContext.Projects.Returns(projectsMock);

        IReadOnlyCollection<CreateProjectState> flowStates =
            [new CreateProjectState("Backlog", FlowStateCategory.Active, ProjectCommandData.FlowStateColor, [ProjectRole.Admin])];
        CreateProjectCommand command = ProjectCommandData.GetCreateCommand(flowStates);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.InitialFlowStatesMissingCompletedState);
    }

    [Fact]
    public async Task Should_ReturnColorError_When_FlowStateColorIsInvalid()
    {
        // Arrange
        User user = ProjectCommandData.GetUser();
        _userContext.UserId.Returns(user.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Project>());
        _dbContext.Users.Returns(usersMock);
        _dbContext.Projects.Returns(projectsMock);

        string invalidColor = new('A', Color.MaxLength + 1);
        IReadOnlyCollection<CreateProjectState> flowStates =
        [
            new CreateProjectState("Backlog", FlowStateCategory.Active, invalidColor, [ProjectRole.Admin]),
            new CreateProjectState("Done", FlowStateCategory.Completed, ProjectCommandData.FlowStateColor, [ProjectRole.Admin]),
            new CreateProjectState("Cancelled", FlowStateCategory.Cancelled, ProjectCommandData.FlowStateColor, [ProjectRole.Admin])
        ];
        CreateProjectCommand command = ProjectCommandData.GetCreateCommand(flowStates);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ColorErrors.ColorTooLong);

        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnDuplicateFlowStateNameError_When_FlowStatesHaveDuplicateNames()
    {
        // Arrange
        User user = ProjectCommandData.GetUser();
        _userContext.UserId.Returns(user.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Project>());
        _dbContext.Users.Returns(usersMock);
        _dbContext.Projects.Returns(projectsMock);

        IReadOnlyCollection<CreateProjectState> flowStates =
        [
            new CreateProjectState("Backlog", FlowStateCategory.Active, ProjectCommandData.FlowStateColor, [ProjectRole.Admin]),
            new CreateProjectState("Backlog", FlowStateCategory.Completed, ProjectCommandData.FlowStateColor, [ProjectRole.Admin]),
            new CreateProjectState("Cancelled", FlowStateCategory.Cancelled, ProjectCommandData.FlowStateColor, [ProjectRole.Admin])
        ];
        CreateProjectCommand command = ProjectCommandData.GetCreateCommand(flowStates);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.DuplicateFlowStateName);

        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnMaxActiveFlowStatesReachedError_When_SeedingMoreThanMaxActiveFlowStates()
    {
        // Arrange
        User user = ProjectCommandData.GetUser();
        _userContext.UserId.Returns(user.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Project>());
        _dbContext.Users.Returns(usersMock);
        _dbContext.Projects.Returns(projectsMock);

        List<CreateProjectState> flowStates = [.. Enumerable.Range(1, Project.MaxActiveFlowStates + 1)
            .Select(i => new CreateProjectState($"Active {i}", FlowStateCategory.Active, ProjectCommandData.FlowStateColor, [ProjectRole.Admin]))];
        flowStates.Add(new CreateProjectState("Done", FlowStateCategory.Completed, ProjectCommandData.FlowStateColor, [ProjectRole.Admin]));
        flowStates.Add(new CreateProjectState("Cancelled", FlowStateCategory.Cancelled, ProjectCommandData.FlowStateColor, [ProjectRole.Admin]));

        CreateProjectCommand command = ProjectCommandData.GetCreateCommand(flowStates);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.MaxActiveFlowStatesReached);

        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_WireTransitionToCancelledState_When_ActiveStateIsSeededAfterCancelledState()
    {
        // Arrange
        User user = ProjectCommandData.GetUser();
        _userContext.UserId.Returns(user.Id);
        _dateTimeProvider.UtcNow.Returns(ProjectCommandData.UtcNow);

        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        DbSet<Project> projectsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Project>());
        _dbContext.Users.Returns(usersMock);
        _dbContext.Projects.Returns(projectsMock);

        // Submitted out of category order: Active, Cancelled, then a second Active added later.
        // The handler must reorder (Active -> Completed -> Cancelled) before seeding so the
        // second Active state still gets wired to Cancelled.
        IReadOnlyCollection<CreateProjectState> flowStates =
        [
            new CreateProjectState("Todo", FlowStateCategory.Active, ProjectCommandData.FlowStateColor, [ProjectRole.Admin]),
            new CreateProjectState("Cancelled", FlowStateCategory.Cancelled, ProjectCommandData.FlowStateColor, [ProjectRole.Admin]),
            new CreateProjectState("InProgress", FlowStateCategory.Active, ProjectCommandData.FlowStateColor, [ProjectRole.Admin]),
            new CreateProjectState("Done", FlowStateCategory.Completed, ProjectCommandData.FlowStateColor, [ProjectRole.Admin])
        ];
        CreateProjectCommand command = ProjectCommandData.GetCreateCommand(flowStates);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();

        projectsMock.Received(1).Add(Arg.Is<Project>(p =>
            p.FlowStates.Count == 4 &&
            p.FlowTransitions.Any(t =>
                p.FlowStates.First(s => s.Id == t.FromStateId).Name == "InProgress" &&
                p.FlowStates.First(s => s.Id == t.ToStateId).Name == "Cancelled")));
    }
}
