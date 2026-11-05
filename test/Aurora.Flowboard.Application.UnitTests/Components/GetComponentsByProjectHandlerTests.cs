namespace Aurora.Flowboard.Application.UnitTests.Components;

public sealed class GetComponentsByProjectHandlerTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IUserContext _userContext;
    private readonly GetComponentsByProjectHandler _handler;

    public GetComponentsByProjectHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _userContext = Substitute.For<IUserContext>();
        _handler = new GetComponentsByProjectHandler(_dbContext, _userContext);
    }

    [Fact]
    public async Task Should_ReturnNotFoundError_When_ProjectDoesNotExist()
    {
        // Arrange
        _userContext.UserId.Returns(Guid.NewGuid());
        _dbContext.Seed(c => c.Projects);

        // Act
        Result<IReadOnlyCollection<ComponentResponse>> result =
            await _handler.Handle(new GetComponentsByProjectQuery(Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.NotFound);
    }

    [Fact]
    public async Task Should_ReturnNotFoundError_When_UserIsNotMember()
    {
        // Arrange
        User admin = ComponentQueryData.GetAdminUser();
        User other = ComponentQueryData.GetOtherUser();
        Project project = ComponentQueryData.GetProjectWithComponents(admin, "Billing");
        _userContext.UserId.Returns(other.Id);
        _dbContext.Seed(c => c.Projects, project);

        // Act
        Result<IReadOnlyCollection<ComponentResponse>> result =
            await _handler.Handle(new GetComponentsByProjectQuery(project.Id), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.NotFound);
    }

    [Fact]
    public async Task Should_ReturnEmptyCollection_When_ProjectHasNoComponents()
    {
        // Arrange
        User admin = ComponentQueryData.GetAdminUser();
        Project project = ComponentQueryData.GetProject(admin);
        _userContext.UserId.Returns(admin.Id);
        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.Components);

        // Act
        Result<IReadOnlyCollection<ComponentResponse>> result =
            await _handler.Handle(new GetComponentsByProjectQuery(project.Id), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_MapAllFields_When_ProjectHasComponents()
    {
        // Arrange
        User admin = ComponentQueryData.GetAdminUser();
        Project project = ComponentQueryData.GetProjectWithComponents(admin, "Billing");
        Component component = project.Components.Single();
        _userContext.UserId.Returns(admin.Id);
        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.Components, project.Components);

        // Act
        Result<IReadOnlyCollection<ComponentResponse>> result =
            await _handler.Handle(new GetComponentsByProjectQuery(project.Id), CancellationToken.None);

        // Assert
        ComponentResponse response = result.Value.Single();
        response.Id.Should().Be(component.Id);
        response.Name.Should().Be("Billing");
        response.Status.Should().Be(ComponentStatus.Active);
        response.CreatedBy.Should().Be(admin.Id);
        response.CreatedOnUtc.Should().Be(ComponentQueryData.UtcNow);
        response.UpdatedOnUtc.Should().BeNull();
    }

    [Fact]
    public async Task Should_IncludeRetiredComponents_When_ProjectHasRetiredComponent()
    {
        // Arrange
        User admin = ComponentQueryData.GetAdminUser();
        Project project = ComponentQueryData.GetProjectWithComponents(admin, "Billing");
        Component component = project.Components.Single();
        component.Retire(admin, 0, ComponentQueryData.UtcNow);
        _userContext.UserId.Returns(admin.Id);
        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.Components, project.Components);

        // Act
        Result<IReadOnlyCollection<ComponentResponse>> result =
            await _handler.Handle(new GetComponentsByProjectQuery(project.Id), CancellationToken.None);

        // Assert
        ComponentResponse response = result.Value.Single();
        response.Status.Should().Be(ComponentStatus.Retired);
    }

    [Fact]
    public async Task Should_ReturnComponentsOrderedByName()
    {
        // Arrange
        User admin = ComponentQueryData.GetAdminUser();
        Project project = ComponentQueryData.GetProjectWithComponents(admin, "Portal", "Api", "Billing");
        _userContext.UserId.Returns(admin.Id);
        _dbContext.Seed(c => c.Projects, project);
        _dbContext.Seed(c => c.Components, project.Components);

        // Act
        Result<IReadOnlyCollection<ComponentResponse>> result =
            await _handler.Handle(new GetComponentsByProjectQuery(project.Id), CancellationToken.None);

        // Assert
        result.Value.Select(c => c.Name).Should().ContainInOrder("Api", "Billing", "Portal");
    }
}
