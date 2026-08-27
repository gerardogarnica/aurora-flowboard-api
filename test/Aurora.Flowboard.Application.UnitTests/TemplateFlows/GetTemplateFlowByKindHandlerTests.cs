namespace Aurora.Flowboard.Application.UnitTests.TemplateFlows;

public sealed class GetTemplateFlowByKindHandlerTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly GetTemplateFlowByKindHandler _handler;

    public GetTemplateFlowByKindHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _handler = new GetTemplateFlowByKindHandler(_dbContext);
    }

    [Fact]
    public async Task Should_ReturnNotFoundError_When_TemplateFlowDoesNotExist()
    {
        // Arrange
        DbSet<TemplateFlow> templatesMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<TemplateFlow>());
        _dbContext.TemplateFlows.Returns(templatesMock);

        GetTemplateFlowByKindQuery query = new(ProjectKind.Product);

        // Act
        Result<TemplateFlowResponse> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(TemplateFlowErrors.NotFound);
    }

    [Fact]
    public async Task Should_ReturnEmptyStates_When_TemplateFlowHasNoStates()
    {
        // Arrange
        TemplateFlow template = TemplateFlowQueryData.GetTemplateFlow(ProjectKind.Client);

        DbSet<TemplateFlow> templatesMock = MockDbSetHelper.CreateMockDbSet([template]);
        _dbContext.TemplateFlows.Returns(templatesMock);

        GetTemplateFlowByKindQuery query = new(ProjectKind.Client);

        // Act
        Result<TemplateFlowResponse> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Id.Should().Be(template.Id);
        result.Value.Kind.Should().Be(ProjectKind.Client);
        result.Value.States.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_MapStates_When_TemplateFlowHasStates()
    {
        // Arrange
        TemplateFlow template = TemplateFlowQueryData.GetTemplateFlowWithStates();

        DbSet<TemplateFlow> templatesMock = MockDbSetHelper.CreateMockDbSet([template]);
        _dbContext.TemplateFlows.Returns(templatesMock);

        GetTemplateFlowByKindQuery query = new(ProjectKind.Product);

        // Act
        Result<TemplateFlowResponse> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.States.Should().HaveCount(3);
        result.Value.States.Should().Contain(s => s.Name == "Backlog" && s.Category == FlowStateCategory.Active);
        result.Value.States.Should().Contain(s => s.Name == "Done" && s.Category == FlowStateCategory.Completed);
        result.Value.States.Should().Contain(s => s.Name == "Cancelled" && s.Category == FlowStateCategory.Cancelled);
        result.Value.States.Should().OnlyContain(s => s.Color == TemplateFlowQueryData.Color.Value);
    }

    [Fact]
    public async Task Should_OrderStatesByCategoryThenSortOrder_When_TemplateFlowHasStates()
    {
        // Arrange
        TemplateFlow template = TemplateFlowQueryData.GetTemplateFlowWithStates();

        DbSet<TemplateFlow> templatesMock = MockDbSetHelper.CreateMockDbSet([template]);
        _dbContext.TemplateFlows.Returns(templatesMock);

        GetTemplateFlowByKindQuery query = new(ProjectKind.Product);

        // Act
        Result<TemplateFlowResponse> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.States.Select(s => s.Category).Should().BeInAscendingOrder();
    }
}
