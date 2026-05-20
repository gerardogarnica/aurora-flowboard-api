namespace Aurora.Flowboard.Application.UnitTests.Flows;

public sealed class GetFlowByIdHandlerTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly GetFlowByIdHandler _handler;

    public GetFlowByIdHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _handler = new GetFlowByIdHandler(_dbContext);
    }

    [Fact]
    public async Task Should_ReturnNotFoundError_When_FlowDoesNotExist()
    {
        // Arrange
        DbSet<Flow> flowsMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<Flow>());
        _dbContext.Flows.Returns(flowsMock);

        // Act
        Result<FlowResponse> result =
            await _handler.Handle(new GetFlowByIdQuery(Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(FlowErrors.NotFound);
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_FlowExists()
    {
        // Arrange
        User admin = FlowQueryData.GetAdminUser();
        Flow flow = FlowQueryData.GetSimpleFlowForGetById(admin);
        DbSet<Flow> flowsMock = MockDbSetHelper.CreateMockDbSet([flow]);
        _dbContext.Flows.Returns(flowsMock);

        // Act
        Result<FlowResponse> result =
            await _handler.Handle(new GetFlowByIdQuery(flow.Id), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task Should_MapAllScalarFields_When_FlowExists()
    {
        // Arrange
        User admin = FlowQueryData.GetAdminUser();
        Flow flow = FlowQueryData.GetSimpleFlowForGetById(admin);
        DbSet<Flow> flowsMock = MockDbSetHelper.CreateMockDbSet([flow]);
        _dbContext.Flows.Returns(flowsMock);

        // Act
        Result<FlowResponse> result =
            await _handler.Handle(new GetFlowByIdQuery(flow.Id), CancellationToken.None);

        // Assert
        FlowResponse response = result.Value;
        response.FlowId.Should().Be(flow.Id);
        response.Name.Should().Be(flow.Name);
        response.Description.Should().Be(flow.Description);
        response.ProjectId.Should().Be(flow.ProjectId);
        response.IsDefault.Should().Be(flow.IsDefault);
        response.IsActive.Should().Be(flow.IsActive);
        response.CreatedOnUtc.Should().Be(flow.CreatedOnUtc);
        response.UpdatedOnUtc.Should().Be(flow.UpdatedOnUtc);
    }

    [Fact]
    public async Task Should_ReturnEmptyStatesAndTransitions_When_FlowHasNoStates()
    {
        // Arrange
        User admin = FlowQueryData.GetAdminUser();
        Flow flow = FlowQueryData.GetSimpleFlowForGetById(admin);
        DbSet<Flow> flowsMock = MockDbSetHelper.CreateMockDbSet([flow]);
        _dbContext.Flows.Returns(flowsMock);

        // Act
        Result<FlowResponse> result =
            await _handler.Handle(new GetFlowByIdQuery(flow.Id), CancellationToken.None);

        // Assert
        result.Value.States.Should().BeEmpty();
        result.Value.Transitions.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_MapStatesCollection_When_FlowHasStates()
    {
        // Arrange
        User admin = FlowQueryData.GetAdminUser();
        Flow flow = FlowQueryData.GetFlowWithStatesForGetById(admin);
        DbSet<Flow> flowsMock = MockDbSetHelper.CreateMockDbSet([flow]);
        _dbContext.Flows.Returns(flowsMock);

        // Act
        Result<FlowResponse> result =
            await _handler.Handle(new GetFlowByIdQuery(flow.Id), CancellationToken.None);

        // Assert
        result.Value.States.Should().HaveCount(3);
        FlowStateResponse todoState = result.Value.States.Single(s => s.Name == "Todo");
        todoState.Category.Should().Be(FlowStateCategory.Active);
        todoState.SortOrder.Should().Be(1);
    }

    [Fact]
    public async Task Should_MapTransitionsWithResolvedStateNames_When_FlowHasTransitions()
    {
        // Arrange
        User admin = FlowQueryData.GetAdminUser();
        Flow flow = FlowQueryData.GetFlowWithStatesForGetById(admin);
        DbSet<Flow> flowsMock = MockDbSetHelper.CreateMockDbSet([flow]);
        _dbContext.Flows.Returns(flowsMock);

        // Act
        Result<FlowResponse> result =
            await _handler.Handle(new GetFlowByIdQuery(flow.Id), CancellationToken.None);

        // Assert
        result.Value.Transitions.Should().HaveCount(2);
        FlowTransitionResponse todoDone = result.Value.Transitions
            .Single(t => t.FromStateName == "Todo" && t.ToStateName == "Done");
        todoDone.FromStateName.Should().Be("Todo");
        todoDone.ToStateName.Should().Be("Done");
        todoDone.AllowedRoles.Should().Contain(ProjectRole.Admin);
        todoDone.AllowedRoles.Should().Contain(ProjectRole.Developer);
    }
}
