using Aurora.Flowboard.Application.Abstractions.Pagination;
using static Aurora.Flowboard.Application.UnitTests.WorkItems.GetWorkItemsQueryData;

namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class GetWorkItemsHandlerTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly User _caller = GetUser("Casey", "Caller");
    private readonly User _teammate = GetUser("Taylor", "Teammate");
    private readonly GetWorkItemsHandler _handler;

    public GetWorkItemsHandlerTests()
    {
        _userContext.UserId.Returns(_caller.Id);
        _dateTimeProvider.UtcNow.Returns(Now);
        _handler = new GetWorkItemsHandler(_dbContext, _userContext, _dateTimeProvider);
    }

    [Fact]
    public async Task Should_ReturnEmptyPage_When_UserHasNoActiveProjects()
    {
        // Arrange
        Project foreign = GetThreeStageProject("Foreign", "FRG", _teammate);
        Arrange([foreign], [CreateInState(foreign, InProgressState, _teammate, Days(1))]);

        // Act
        Result<PagedResponse<WorkItemListItemResponse>> result = await _handler.Handle(Search(), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
        _ = _dbContext.DidNotReceive().WorkItems;
    }

    [Fact]
    public async Task Should_ReturnOnlyOpenItems_OfMyActiveAndMaintenanceProjects()
    {
        // Arrange
        Project active = GetThreeStageProject("Active", "ACT", _caller);
        Project maintenance = GetContinuousThreeStageProject("Maintenance", "MNT", _caller);
        Project completed = GetThreeStageProject("Completed", "CMP", _caller);
        Project archived = GetThreeStageProject("Archived", "ARC", _caller);
        Project foreign = GetThreeStageProject("Foreign", "FRG", _teammate);
        WorkItem activeInFlight = CreateInState(active, InProgressState, _caller, Days(1));
        WorkItem activeBacklog = CreateInBacklog(active, _caller, Days(1));
        WorkItem activeDone = CreateInState(active, DoneState, _caller, Days(1));
        WorkItem activeCancelled = CreateInState(active, CancelledState, _caller, Days(1));
        WorkItem maintenanceInFlight = CreateInState(maintenance, InProgressState, _caller, Days(1));
        WorkItem completedInFlight = CreateInState(completed, InProgressState, _caller, Days(1));
        WorkItem archivedInFlight = CreateInState(archived, InProgressState, _caller, Days(1));
        WorkItem foreignInFlight = CreateInState(foreign, InProgressState, _teammate, Days(1));
        ChangeStatus(maintenance, ProjectStatus.Maintenance, _caller);
        ChangeStatus(completed, ProjectStatus.Completed, _caller);
        ChangeStatus(archived, ProjectStatus.Archived, _caller);
        Arrange(
            [active, maintenance, completed, archived, foreign],
            [activeInFlight, activeBacklog, activeDone, activeCancelled, maintenanceInFlight, completedInFlight, archivedInFlight, foreignInFlight]);

        // Act
        Result<PagedResponse<WorkItemListItemResponse>> result = await _handler.Handle(Search(), CancellationToken.None);

        // Assert
        result.Value.Items.Select(i => i.WorkItemId).Should().BeEquivalentTo([activeInFlight.Id, activeBacklog.Id, maintenanceInFlight.Id]);
    }

    [Fact]
    public async Task Should_ReturnOnlyItemsAssignedToMe_When_AssigneeIsMe()
    {
        // Arrange
        Project project = GetThreeStageProject("Work", "WRK", _caller, _teammate);
        WorkItem mine = CreateInState(project, InProgressState, _caller, Days(1), assignee: _caller);
        WorkItem theirs = CreateInState(project, InProgressState, _caller, Days(1), assignee: _teammate);
        WorkItem unassigned = CreateInState(project, InProgressState, _caller, Days(1));
        Arrange([project], [mine, theirs, unassigned]);

        // Act
        Result<PagedResponse<WorkItemListItemResponse>> result = await _handler.Handle(Search(assignee: "me"), CancellationToken.None);

        // Assert
        result.Value.Items.Select(i => i.WorkItemId).Should().Equal(mine.Id);
    }

    [Fact]
    public async Task Should_ReturnOpenItemsICreated_ForAnyAssignee_When_CreatedByIsMe()
    {
        // Arrange
        Project project = GetThreeStageProject("Work", "WRK", _caller, _teammate);
        WorkItem forTeammate = CreateInState(project, InProgressState, _caller, Days(2), assignee: _teammate);
        WorkItem unassigned = CreateInBacklog(project, _caller, Days(1));
        WorkItem byTeammate = CreateInState(project, InProgressState, _teammate, Days(1), assignee: _caller);
        WorkItem closed = CreateInState(project, DoneState, _caller, Days(1));
        Arrange([project], [forTeammate, unassigned, byTeammate, closed]);

        // Act
        Result<PagedResponse<WorkItemListItemResponse>> result = await _handler.Handle(Search(createdBy: "me"), CancellationToken.None);

        // Assert
        result.Value.Items.Select(i => i.WorkItemId).Should().BeEquivalentTo([forTeammate.Id, unassigned.Id]);
    }

    [Fact]
    public async Task Should_ReturnOnlyBacklogItems_When_StageIsBacklog()
    {
        // Arrange
        Project threeStage = GetThreeStageProject("Three", "THR", _caller);
        Project singleState = GetSingleStateProject("Single", "SGL", _caller);
        WorkItem backlog = CreateInBacklog(threeStage, _caller, Days(1));
        WorkItem inProgress = CreateInState(threeStage, InProgressState, _caller, Days(1));
        WorkItem single = CreateInBacklog(singleState, _caller, Days(1));
        Arrange([threeStage, singleState], [backlog, inProgress, single]);

        // Act
        Result<PagedResponse<WorkItemListItemResponse>> result = await _handler.Handle(Search(stage: "Backlog"), CancellationToken.None);

        // Assert
        result.Value.Items.Select(i => i.WorkItemId).Should().Equal(backlog.Id);
    }

    [Fact]
    public async Task Should_IncludeSingleStateProjects_When_StageIsInFlight()
    {
        // Arrange
        Project threeStage = GetThreeStageProject("Three", "THR", _caller);
        Project singleState = GetSingleStateProject("Single", "SGL", _caller);
        WorkItem backlog = CreateInBacklog(threeStage, _caller, Days(1));
        WorkItem inProgress = CreateInState(threeStage, InProgressState, _caller, Days(1));
        WorkItem single = CreateInBacklog(singleState, _caller, Days(1));
        Arrange([threeStage, singleState], [backlog, inProgress, single]);

        // Act
        Result<PagedResponse<WorkItemListItemResponse>> result = await _handler.Handle(Search(stage: "InFlight"), CancellationToken.None);

        // Assert
        result.Value.Items.Select(i => i.WorkItemId).Should().BeEquivalentTo([inProgress.Id, single.Id]);
        result.Value.Items.Should().OnlyContain(i => i.Stage == FlowStateStage.InFlight);
    }

    [Fact]
    public async Task Should_ReturnInFlightItemsWithSevenDaysOrMore_When_StalledIsTrue()
    {
        // Arrange
        Project project = GetThreeStageProject("Work", "WRK", _caller);
        Project singleState = GetSingleStateProject("Single", "SGL", _caller);
        WorkItem stalled = CreateInState(project, InProgressState, _caller, Days(7));
        WorkItem almostStalled = CreateInState(project, ReviewState, _caller, Days(7) - TimeSpan.FromMinutes(1));
        WorkItem oldBacklog = CreateInBacklog(project, _caller, Days(30));
        WorkItem singleStateStalled = CreateInBacklog(singleState, _caller, Days(8));
        Arrange([project, singleState], [stalled, almostStalled, oldBacklog, singleStateStalled]);

        // Act
        Result<PagedResponse<WorkItemListItemResponse>> result = await _handler.Handle(Search(stalled: "true"), CancellationToken.None);

        // Assert
        result.Value.Items.Select(i => i.WorkItemId).Should().BeEquivalentTo([stalled.Id, singleStateStalled.Id]);
    }

    [Fact]
    public async Task Should_ReturnEmptyPage_When_StageIsBacklogAndStalledIsTrue()
    {
        // Arrange
        Project project = GetThreeStageProject("Work", "WRK", _caller);
        WorkItem oldBacklog = CreateInBacklog(project, _caller, Days(30));
        WorkItem stalled = CreateInState(project, InProgressState, _caller, Days(9));
        Arrange([project], [oldBacklog, stalled]);

        // Act
        Result<PagedResponse<WorkItemListItemResponse>> result =
            await _handler.Handle(Search(stage: "Backlog", stalled: "true"), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_FilterByEveryListedPriority()
    {
        // Arrange
        Project project = GetThreeStageProject("Work", "WRK", _caller);
        WorkItem low = CreateInState(project, InProgressState, _caller, Days(1), Priority.Low);
        WorkItem high = CreateInState(project, InProgressState, _caller, Days(1), Priority.High);
        WorkItem critical = CreateInState(project, InProgressState, _caller, Days(1), Priority.Critical);
        Arrange([project], [low, high, critical]);

        // Act
        Result<PagedResponse<WorkItemListItemResponse>> result =
            await _handler.Handle(Search(priority: "High,Critical"), CancellationToken.None);

        // Assert
        result.Value.Items.Select(i => i.WorkItemId).Should().BeEquivalentTo([high.Id, critical.Id]);
    }

    [Fact]
    public async Task Should_OrderInFlightFirst_ThenByLongestTimeInState()
    {
        // Arrange
        Project project = GetThreeStageProject("Work", "WRK", _caller);
        WorkItem backlogOldest = CreateInBacklog(project, _caller, Days(40));
        WorkItem inFlightNewer = CreateInState(project, InProgressState, _caller, Days(2));
        WorkItem inFlightOlder = CreateInState(project, ReviewState, _caller, Days(9));
        WorkItem backlogNewer = CreateInBacklog(project, _caller, Days(1));
        Arrange([project], [backlogOldest, inFlightNewer, inFlightOlder, backlogNewer]);

        // Act
        Result<PagedResponse<WorkItemListItemResponse>> result = await _handler.Handle(Search(), CancellationToken.None);

        // Assert
        result.Value.Items.Select(i => i.WorkItemId).Should().Equal(inFlightOlder.Id, inFlightNewer.Id, backlogOldest.Id, backlogNewer.Id);
    }

    [Fact]
    public async Task Should_ReturnDifferentItems_OnConsecutivePages()
    {
        // Arrange
        Project project = GetThreeStageProject("Work", "WRK", _caller);
        WorkItem oldest = CreateInState(project, InProgressState, _caller, Days(3));
        WorkItem middle = CreateInState(project, InProgressState, _caller, Days(2));
        WorkItem newest = CreateInState(project, InProgressState, _caller, Days(1));
        Arrange([project], [newest, oldest, middle]);

        // Act
        Result<PagedResponse<WorkItemListItemResponse>> firstPage = await _handler.Handle(Search(page: 1, pageSize: 2), CancellationToken.None);
        Result<PagedResponse<WorkItemListItemResponse>> secondPage = await _handler.Handle(Search(page: 2, pageSize: 2), CancellationToken.None);

        // Assert
        firstPage.Value.Items.Select(i => i.WorkItemId).Should().Equal(oldest.Id, middle.Id);
        secondPage.Value.Items.Select(i => i.WorkItemId).Should().Equal(newest.Id);
        secondPage.Value.TotalCount.Should().Be(3);
        secondPage.Value.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task Should_MapEveryListItemField()
    {
        // Arrange
        Project project = GetThreeStageProject("Aurora Web", "AWB", _caller, _teammate);
        Milestone milestone = CreateActiveMilestone(project, _caller, "Sprint 1", null);
        WorkItem workItem = CreateInState(project, InProgressState, _caller, Days(3), Priority.High, _teammate, milestone);
        Arrange([project], [workItem]);

        // Act
        Result<PagedResponse<WorkItemListItemResponse>> result = await _handler.Handle(Search(), CancellationToken.None);

        // Assert
        result.Value.Items.Should().ContainSingle().Which.Should().Be(new WorkItemListItemResponse(
            workItem.Id,
            workItem.Code,
            workItem.Title,
            workItem.Type,
            Priority.High,
            project.Id,
            "Aurora Web",
            project.Color.Value,
            workItem.FlowStateId,
            InProgressState,
            FlowStateStage.InFlight,
            Now - Days(3),
            _teammate.Id,
            "TT",
            "Taylor Teammate",
            "Sprint 1",
            MilestoneColor.Value));
    }

    [Fact]
    public async Task Should_ReturnNullAssigneeAndMilestone_When_ItemHasNone()
    {
        // Arrange
        Project project = GetThreeStageProject("Work", "WRK", _caller);
        WorkItem workItem = CreateInBacklog(project, _caller, Days(1));
        Arrange([project], [workItem]);

        // Act
        Result<PagedResponse<WorkItemListItemResponse>> result = await _handler.Handle(Search(), CancellationToken.None);

        // Assert
        WorkItemListItemResponse item = result.Value.Items.Single();
        item.Stage.Should().Be(FlowStateStage.Backlog);
        item.AssigneeId.Should().BeNull();
        item.AssigneeInitials.Should().BeNull();
        item.AssigneeFullName.Should().BeNull();
        item.MilestoneName.Should().BeNull();
        item.MilestoneColor.Should().BeNull();
    }

    private static TimeSpan Days(int days) => TimeSpan.FromDays(days);

    private static GetWorkItemsQuery Search(
        string? assignee = null,
        string? createdBy = null,
        string? stage = null,
        string? stalled = null,
        string? priority = null,
        int page = PaginationDefaults.DefaultPage,
        int pageSize = PaginationDefaults.DefaultPageSize) =>
        new(assignee, createdBy, stage, stalled, priority, null, page, pageSize);

    private void Arrange(IReadOnlyCollection<Project> projects, IReadOnlyCollection<WorkItem> workItems)
    {
        _dbContext.Seed(c => c.Projects, projects);
        _dbContext.Seed(c => c.WorkItems, workItems);
        _dbContext.Seed(c => c.Users, _caller, _teammate);
    }
}
