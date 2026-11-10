using Aurora.Flowboard.Application.Users.GetMySummary;
using static Aurora.Flowboard.Application.UnitTests.WorkItems.GetWorkItemsQueryData;

namespace Aurora.Flowboard.Application.UnitTests.Dashboard;

public sealed class GetDashboardHandlerTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly User _caller = GetUser("Casey", "Caller");
    private readonly User _teammate = GetUser("Taylor", "Teammate");
    private readonly GetDashboardHandler _handler;

    public GetDashboardHandlerTests()
    {
        _userContext.UserId.Returns(_caller.Id);
        _dateTimeProvider.UtcNow.Returns(Now);
        _handler = new GetDashboardHandler(_dbContext, _userContext, _dateTimeProvider);
    }

    [Fact]
    public async Task Should_ReturnTheEmptyDashboard_When_UserHasNoProjects()
    {
        // Arrange
        Project foreign = GetThreeStageProject("Foreign", "FRG", _teammate);
        Arrange([foreign], [CreateInState(foreign, InProgressState, _teammate, Days(9))]);

        // Act
        Result<DashboardResponse> result = await _handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Me.Should().Be(new DashboardMeResponse(0, 0, 0, 0));
        result.Value.Aging.Buckets.Should().HaveCount(4).And.OnlyContain(b => b.Count == 0);
        result.Value.Portfolio.Should().BeEmpty();
        _ = _dbContext.DidNotReceive().WorkItems;
    }

    [Fact]
    public async Task Should_PlaceInFlightItems_InAgeBuckets_AtTheExactLimits()
    {
        // Arrange
        Project project = GetThreeStageProject("Aurora", "AUR", _caller);
        TimeSpan[] timesInState =
        [
            Days(3) + Hours(23),
            Days(4),
            Days(6) + Hours(23),
            Days(7),
            Days(13) + Hours(23),
            Days(14)
        ];
        Arrange([project], [.. timesInState.Select(age => CreateInState(project, InProgressState, _caller, age))]);

        // Act
        Result<DashboardResponse> result = await _handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        // Assert
        result.Value.Aging.Buckets.Select(b => b.Count).Should().Equal(1, 2, 2, 1);
        result.Value.Team.Stalled.Should().Be(3);
    }

    [Fact]
    public async Task Should_NotCountBacklogAsStalled_WhateverItsAge()
    {
        // Arrange
        Project project = GetThreeStageProject("Aurora", "AUR", _caller);
        Arrange([project], [CreateInBacklog(project, _caller, Days(30), assignee: _caller)]);

        // Act
        Result<DashboardResponse> result = await _handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        // Assert
        result.Value.Me.Should().Be(new DashboardMeResponse(1, 0, 0, 0));
        result.Value.Team.Stalled.Should().Be(0);
        result.Value.Aging.OldestInFlight.Should().BeNull();
    }

    [Fact]
    public async Task Should_CountASingleActiveState_AsInFlight()
    {
        // Arrange
        Project project = GetSingleStateProject("Single", "SGL", _caller);
        Arrange([project], [CreateInBacklog(project, _caller, Days(7))]);

        // Act
        Result<DashboardResponse> result = await _handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        // Assert
        result.Value.Team.Stalled.Should().Be(1);
        result.Value.Portfolio.Single().Stalled.Should().Be(1);
    }

    [Fact]
    public async Task Should_MatchMyOpenIssues_OfMySummary()
    {
        // Arrange
        Project active = GetThreeStageProject("Active", "ACT", _caller, _teammate);
        Project completed = GetThreeStageProject("Completed", "CMP", _caller);
        Project archived = GetThreeStageProject("Archived", "ARC", _caller);
        WorkItem mineInFlight = CreateInState(active, InProgressState, _caller, Days(2), assignee: _caller);
        WorkItem mineInBacklog = CreateInBacklog(active, _caller, Days(1), assignee: _caller);
        WorkItem mineDone = CreateInState(active, DoneState, _caller, Days(1), assignee: _caller);
        WorkItem theirs = CreateInState(active, InProgressState, _caller, Days(1), assignee: _teammate);
        WorkItem mineInCompleted = CreateInState(completed, InProgressState, _caller, Days(1), assignee: _caller);
        WorkItem mineInArchived = CreateInState(archived, InProgressState, _caller, Days(1), assignee: _caller);
        ChangeStatus(completed, ProjectStatus.Completed, _caller);
        ChangeStatus(archived, ProjectStatus.Archived, _caller);
        AttachWorkItems(active, mineInFlight, mineInBacklog, mineDone, theirs);
        AttachWorkItems(completed, mineInCompleted);
        AttachWorkItems(archived, mineInArchived);
        Arrange(
            [active, completed, archived],
            [mineInFlight, mineInBacklog, mineDone, theirs, mineInCompleted, mineInArchived]);

        // Act
        Result<DashboardResponse> dashboard = await _handler.Handle(new GetDashboardQuery(), CancellationToken.None);
        Result<MySummaryResponse> summary = await new GetMySummaryHandler(_dbContext)
            .Handle(new GetMySummaryQuery(_caller.Id), CancellationToken.None);

        // Assert
        dashboard.Value.Me.Open.Should().Be(2);
        summary.Value.Counts.MyOpenIssues.Should().Be(dashboard.Value.Me.Open);
    }

    [Fact]
    public async Task Should_LeaveOutACompletedProject_Entirely()
    {
        // Arrange
        Project active = GetThreeStageProject("Active", "ACT", _caller);
        Project completed = GetThreeStageProject("Closed", "CLS", _caller, _teammate);
        Milestone milestone = CreateActiveMilestone(completed, _caller, "Release", new DateOnly(2026, 2, 1));
        WorkItem stalled = CreateInState(completed, InProgressState, _caller, Days(20), assignee: _caller, milestone: milestone);
        WorkItem closed = CreateInState(completed, DoneState, _caller, Days(1));
        ChangeStatus(completed, ProjectStatus.Completed, _caller);
        Arrange([active, completed], [stalled, closed]);

        // Act
        Result<DashboardResponse> result = await _handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        // Assert
        result.Value.Me.Should().Be(new DashboardMeResponse(0, 0, 0, 0));
        result.Value.Team.Should().Be(new DashboardTeamResponse(0, 0, 0));
        result.Value.Aging.OldestInFlight.Should().BeNull();
        result.Value.Milestones.Should().BeEmpty();
        DashboardProjectResponse row = result.Value.Portfolio.Should().ContainSingle().Subject;
        row.ProjectId.Should().Be(active.Id);
        row.Members.Should().ContainSingle(m => m.UserId == _caller.Id);
    }

    [Fact]
    public async Task Should_ReturnTheEmptyDashboard_When_UserOnlyHasCompletedProjects()
    {
        // Arrange
        Project completed = GetThreeStageProject("Closed", "CLS", _caller);
        WorkItem open = CreateInState(completed, InProgressState, _caller, Days(9), assignee: _caller);
        WorkItem closed = CreateInState(completed, DoneState, _caller, Days(1));
        ChangeStatus(completed, ProjectStatus.Completed, _caller);
        Arrange([completed], [open, closed]);

        // Act
        Result<DashboardResponse> result = await _handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        // Assert
        result.Value.Me.Should().Be(new DashboardMeResponse(0, 0, 0, 0));
        result.Value.Team.Should().Be(new DashboardTeamResponse(0, 0, 0));
        result.Value.Portfolio.Should().BeEmpty();
        _ = _dbContext.DidNotReceive().WorkItems;
    }

    [Fact]
    public async Task Should_CountClosedLast7Days_ByCategory_OnlyInActiveProjects()
    {
        // Arrange
        Project active = GetThreeStageProject("Active", "ACT", _caller);
        Project completed = GetThreeStageProject("Completed", "CMP", _caller);
        WorkItem closedRecently = CreateInState(active, DoneState, _caller, Days(2));
        WorkItem closedAtTheWindowEdge = CreateInState(active, DoneState, _caller, Days(7));
        WorkItem closedLongAgo = CreateInState(active, DoneState, _caller, Days(8));
        WorkItem cancelledRecently = CreateInState(active, CancelledState, _caller, Days(1));
        WorkItem reopened = CreateInState(active, InProgressState, _caller, Days(1));
        SetCompletedOnUtc(reopened, Now - Days(2));
        WorkItem closedInCompletedProject = CreateInState(completed, DoneState, _caller, Days(3));
        ChangeStatus(completed, ProjectStatus.Completed, _caller);
        Arrange(
            [active, completed],
            [closedRecently, closedAtTheWindowEdge, closedLongAgo, cancelledRecently, reopened, closedInCompletedProject]);

        // Act
        Result<DashboardResponse> result = await _handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        // Assert
        result.Value.Team.ClosedLast7Days.Should().Be(2);
    }

    [Fact]
    public async Task Should_ReturnTheOldestInFlightItem_IgnoringTheBacklog()
    {
        // Arrange
        Project project = GetThreeStageProject("Aurora", "AUR", _caller);
        WorkItem oldBacklog = CreateInBacklog(project, _caller, Days(40));
        WorkItem oldest = CreateInState(project, ReviewState, _caller, Days(12));
        WorkItem newer = CreateInState(project, InProgressState, _caller, Days(3));
        Arrange([project], [oldBacklog, oldest, newer]);

        // Act
        Result<DashboardResponse> result = await _handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        // Assert
        result.Value.Aging.OldestInFlight.Should().Be(new DashboardOldestInFlightResponse(
            oldest.Code, oldest.Title, project.Id, "Aurora", Now - Days(12)));
    }

    [Fact]
    public async Task Should_ComputeMilestoneCounts_FromOpenAndCompletedItems()
    {
        // Arrange
        Project project = GetThreeStageProject("Aurora", "AUR", _caller);
        Milestone milestone = CreateActiveMilestone(project, _caller, "Sprint 1", new DateOnly(2026, 3, 15));
        Milestone draft = Milestone.Create("Draft", null, MilestoneColor, null, null, project, _caller, Now).Value;
        WorkItem stalledLinked = CreateInState(project, InProgressState, _caller, Days(9), milestone: milestone);
        WorkItem backlogLinked = CreateInBacklog(project, _caller, Days(1), milestone: milestone);
        WorkItem doneLinked = CreateInState(project, DoneState, _caller, Days(30), milestone: milestone);
        WorkItem cancelledLinked = CreateInState(project, CancelledState, _caller, Days(1), milestone: milestone);
        WorkItem draftLinked = CreateInBacklog(project, _caller, Days(1), milestone: draft);
        WorkItem unlinked = CreateInState(project, InProgressState, _caller, Days(1));
        Arrange([project], [stalledLinked, backlogLinked, doneLinked, cancelledLinked, draftLinked, unlinked]);

        // Act
        Result<DashboardResponse> result = await _handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        // Assert
        DashboardMilestoneResponse linked = result.Value.Milestones.Should().ContainSingle().Subject;
        linked.Id.Should().Be(milestone.Id);
        linked.ProjectName.Should().Be("Aurora");
        linked.LinkedTotal.Should().Be(3);
        linked.LinkedClosed.Should().Be(1);
        linked.LinkedStalled.Should().Be(1);
        result.Value.Portfolio.Single().ActiveMilestone!.LinkedTotal.Should().Be(3);
    }

    [Fact]
    public async Task Should_MapThePortfolioRow_WithItsMembers()
    {
        // Arrange
        Project project = GetThreeStageProject("Aurora", "AUR", _caller, _teammate);
        Arrange([project], []);

        // Act
        Result<DashboardResponse> result = await _handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        // Assert
        DashboardProjectResponse row = result.Value.Portfolio.Single();
        row.ProjectId.Should().Be(project.Id);
        row.Name.Should().Be("Aurora");
        row.Color.Should().Be(project.Color.Value);
        row.Kind.Should().Be(ProjectKind.Client);
        row.Status.Should().Be(ProjectStatus.Active);
        row.Distribution.Select(d => d.Name).Should().Equal(BacklogState, InProgressState, ReviewState);
        row.Members.Should().Equal(
            new DashboardMemberResponse(_caller.Id, "Casey Caller", "CC"),
            new DashboardMemberResponse(_teammate.Id, "Taylor Teammate", "TT"));
    }

    private static TimeSpan Days(int days) => TimeSpan.FromDays(days);

    private static TimeSpan Hours(int hours) => TimeSpan.FromHours(hours);

    private void Arrange(IReadOnlyCollection<Project> projects, IReadOnlyCollection<WorkItem> workItems)
    {
        _dbContext.Seed(c => c.Projects, projects);
        _dbContext.Seed(c => c.WorkItems, workItems);
        _dbContext.Seed(c => c.Users, _caller, _teammate);
    }
}
