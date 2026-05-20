namespace Aurora.Flowboard.Application.UnitTests.Flows;

public sealed class GetAllFlowsHandlerTests
{
    private readonly IApplicationDbContext _dbContext;
    private readonly GetAllFlowsHandler _handler;

    public GetAllFlowsHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _handler = new GetAllFlowsHandler(_dbContext);
    }

    [Fact]
    public async Task Should_ReturnOnlyActiveFlows_When_IncludeDeactivatedIsFalse()
    {
        // Arrange
        User admin = FlowQueryData.GetAdminUser();
        Flow activeFlow = FlowQueryData.GetFlowForGetAll("Active Flow", admin);
        Flow deactivatedFlow = FlowQueryData.GetDeactivatedFlow("Deactivated Flow", admin);
        DbSet<Flow> flowsMock = MockDbSetHelper.CreateMockDbSet([activeFlow, deactivatedFlow]);
        _dbContext.Flows.Returns(flowsMock);

        // Act
        Result<IReadOnlyCollection<FlowSummaryResponse>> result =
            await _handler.Handle(new GetAllFlowsQuery(false, null), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.Single().FlowId.Should().Be(activeFlow.Id);
    }

    [Fact]
    public async Task Should_ReturnAllFlows_When_IncludeDeactivatedIsTrue()
    {
        // Arrange
        User admin = FlowQueryData.GetAdminUser();
        Flow activeFlow = FlowQueryData.GetFlowForGetAll("Active Flow", admin);
        Flow deactivatedFlow = FlowQueryData.GetDeactivatedFlow("Deactivated Flow", admin);
        DbSet<Flow> flowsMock = MockDbSetHelper.CreateMockDbSet([activeFlow, deactivatedFlow]);
        _dbContext.Flows.Returns(flowsMock);

        // Act
        Result<IReadOnlyCollection<FlowSummaryResponse>> result =
            await _handler.Handle(new GetAllFlowsQuery(true, null), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Should_ReturnFlowsFilteredByProjectId_When_ProjectIdIsProvided()
    {
        // Arrange
        User admin = FlowQueryData.GetAdminUser();
        Flow flowA = FlowQueryData.GetFlowForGetAll("Flow A", admin);
        Flow flowB = FlowQueryData.GetFlowForGetAll("Flow B", admin);
        DbSet<Flow> flowsMock = MockDbSetHelper.CreateMockDbSet([flowA, flowB]);
        _dbContext.Flows.Returns(flowsMock);

        // Act
        Result<IReadOnlyCollection<FlowSummaryResponse>> result =
            await _handler.Handle(new GetAllFlowsQuery(false, flowA.ProjectId), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.Single().ProjectId.Should().Be(flowA.ProjectId);
    }

    [Fact]
    public async Task Should_ReturnAllFlows_When_ProjectIdIsNull()
    {
        // Arrange
        User admin = FlowQueryData.GetAdminUser();
        Flow flowA = FlowQueryData.GetFlowForGetAll("Flow A", admin);
        Flow flowB = FlowQueryData.GetFlowForGetAll("Flow B", admin);
        DbSet<Flow> flowsMock = MockDbSetHelper.CreateMockDbSet([flowA, flowB]);
        _dbContext.Flows.Returns(flowsMock);

        // Act
        Result<IReadOnlyCollection<FlowSummaryResponse>> result =
            await _handler.Handle(new GetAllFlowsQuery(false, null), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Should_ReturnFlowsOrderedByName()
    {
        // Arrange
        User admin = FlowQueryData.GetAdminUser();
        Flow zetaFlow = FlowQueryData.GetFlowForGetAll("Zeta Flow", admin);
        Flow alphaFlow = FlowQueryData.GetFlowForGetAll("Alpha Flow", admin);
        Flow muFlow = FlowQueryData.GetFlowForGetAll("Mu Flow", admin);
        DbSet<Flow> flowsMock = MockDbSetHelper.CreateMockDbSet([zetaFlow, alphaFlow, muFlow]);
        _dbContext.Flows.Returns(flowsMock);

        // Act
        Result<IReadOnlyCollection<FlowSummaryResponse>> result =
            await _handler.Handle(new GetAllFlowsQuery(false, null), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.Select(f => f.Name).Should().BeInAscendingOrder();
        result.Value.First().Name.Should().Be("Alpha Flow");
    }

    [Fact]
    public async Task Should_MapAllResponseFields_When_FlowExists()
    {
        // Arrange
        User admin = FlowQueryData.GetAdminUser();
        Flow flow = FlowQueryData.GetFlowWithStatesForGetAll("Mapped Flow", admin);
        DbSet<Flow> flowsMock = MockDbSetHelper.CreateMockDbSet([flow]);
        _dbContext.Flows.Returns(flowsMock);

        // Act
        Result<IReadOnlyCollection<FlowSummaryResponse>> result =
            await _handler.Handle(new GetAllFlowsQuery(false, null), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        FlowSummaryResponse summary = result.Value.Single();
        summary.FlowId.Should().Be(flow.Id);
        summary.Name.Should().Be("Mapped Flow");
        summary.Description.Should().Be(FlowQueryData.Description);
        summary.ProjectId.Should().Be(flow.ProjectId);
        summary.IsDefault.Should().BeFalse();
        summary.IsActive.Should().BeTrue();
        summary.StateCount.Should().Be(3);
        summary.TransitionCount.Should().Be(2);
    }
}
