using static Aurora.Flowboard.Application.UnitTests.Dashboard.DashboardQueryData;

namespace Aurora.Flowboard.Application.UnitTests.Dashboard;

public sealed class DashboardCalculatorTests
{
    [Fact]
    public void Should_ReturnZerosAndFourEmptyBuckets_When_SnapshotIsEmpty()
    {
        // Act
        DashboardResponse response = DashboardCalculator.Build(DashboardSnapshot.Empty);

        // Assert
        response.Me.Should().Be(new DashboardMeResponse(0, 0, 0, 0));
        response.Team.Should().Be(new DashboardTeamResponse(0, 0, 0));
        response.Aging.Buckets.Should().Equal(
            new DashboardAgingBucketResponse(0, 3, 0),
            new DashboardAgingBucketResponse(4, 6, 0),
            new DashboardAgingBucketResponse(7, 13, 0),
            new DashboardAgingBucketResponse(14, null, 0));
        response.Aging.OldestInFlight.Should().BeNull();
        response.Milestones.Should().BeEmpty();
        response.Portfolio.Should().BeEmpty();
    }

    [Fact]
    public void Should_NeverCountBacklogAsStalled()
    {
        // Arrange
        DashboardProjectRow project = ProjectRow("Aurora");
        DashboardStateRow backlog = StateRow(project, "Backlog", 1);
        DashboardStateRow inProgress = StateRow(project, "In Progress", 2);
        DashboardSnapshot snapshot = Snapshot(
            [project],
            [backlog, inProgress],
            [GroupRow(backlog, 5, DashboardCalculator.OldBand, isMine: true)]);

        // Act
        DashboardResponse response = DashboardCalculator.Build(snapshot);

        // Assert
        response.Me.Should().Be(new DashboardMeResponse(5, 0, 0, 0));
        response.Team.Stalled.Should().Be(0);
        response.Aging.Buckets.Sum(b => b.Count).Should().Be(0);
        response.Portfolio.Single().Stalled.Should().Be(0);
    }

    [Fact]
    public void Should_TreatASingleActiveState_AsInFlight()
    {
        // Arrange
        DashboardProjectRow project = ProjectRow("Aurora");
        DashboardStateRow only = StateRow(project, "Backlog", 1);
        DashboardSnapshot snapshot = Snapshot(
            [project],
            [only],
            [GroupRow(only, 2, DashboardCalculator.StalledBand, isMine: true)]);

        // Act
        DashboardResponse response = DashboardCalculator.Build(snapshot);

        // Assert
        response.Me.Should().Be(new DashboardMeResponse(2, 2, 2, 0));
        response.Team.Stalled.Should().Be(2);
    }

    [Fact]
    public void Should_MakeTheStalledBucketsAddUpToTeamStalled()
    {
        // Arrange
        DashboardProjectRow project = ProjectRow("Aurora");
        DashboardStateRow backlog = StateRow(project, "Backlog", 1);
        DashboardStateRow inProgress = StateRow(project, "In Progress", 2);
        DashboardStateRow review = StateRow(project, "Review", 3);
        DashboardSnapshot snapshot = Snapshot(
            [project],
            [backlog, inProgress, review],
            [
                GroupRow(inProgress, 1, DashboardCalculator.FreshBand),
                GroupRow(inProgress, 2, DashboardCalculator.AgingBand),
                GroupRow(inProgress, 3, DashboardCalculator.StalledBand),
                GroupRow(review, 4, DashboardCalculator.OldBand),
                GroupRow(backlog, 10, DashboardCalculator.OldBand)
            ]);

        // Act
        DashboardResponse response = DashboardCalculator.Build(snapshot);

        // Assert
        List<DashboardAgingBucketResponse> buckets = [.. response.Aging.Buckets];
        buckets.Select(b => b.Count).Should().Equal(1, 2, 3, 4);
        (buckets[2].Count + buckets[3].Count).Should().Be(response.Team.Stalled);
        response.Team.Stalled.Should().Be(7);
    }

    [Fact]
    public void Should_CountMyWork_AndTheTeamUnassignedWork()
    {
        // Arrange
        DashboardProjectRow project = ProjectRow("Aurora");
        DashboardStateRow backlog = StateRow(project, "Backlog", 1);
        DashboardStateRow inProgress = StateRow(project, "In Progress", 2);
        DashboardSnapshot snapshot = Snapshot(
            [project],
            [backlog, inProgress],
            [
                GroupRow(backlog, 1, isMine: true, isCriticalHigh: true),
                GroupRow(inProgress, 2, isMine: true),
                GroupRow(inProgress, 3, DashboardCalculator.StalledBand, isMine: true, isCriticalHigh: true),
                GroupRow(inProgress, 4),
                GroupRow(backlog, 5, isUnassigned: true),
                GroupRow(inProgress, 6, DashboardCalculator.OldBand, isUnassigned: true)
            ]);

        // Act
        DashboardResponse response = DashboardCalculator.Build(snapshot);

        // Assert
        response.Me.Should().Be(new DashboardMeResponse(Open: 6, Stalled: 3, InFlight: 5, CriticalHigh: 4));
        response.Team.Stalled.Should().Be(9);
        response.Team.Unassigned.Should().Be(11);
    }

    [Fact]
    public void Should_LeaveOutACompletedProject_Entirely()
    {
        // Arrange
        DashboardProjectRow active = ProjectRow("Aurora");
        DashboardProjectRow completed = ProjectRow("Closed", ProjectStatus.Completed);
        DashboardStateRow backlog = StateRow(completed, "Backlog", 1);
        DashboardStateRow inProgress = StateRow(completed, "In Progress", 2);
        DashboardMilestoneRow milestone = MilestoneRow(completed, "Release", new DateOnly(2026, 2, 1));
        DashboardSnapshot snapshot = Snapshot(
            [active, completed],
            [backlog, inProgress],
            [GroupRow(inProgress, 3, DashboardCalculator.StalledBand, isMine: true, milestoneId: milestone.Id)],
            [milestone],
            members: [MemberRow(completed, "Ana", "Lopez")]);

        // Act
        DashboardResponse response = DashboardCalculator.Build(snapshot);

        // Assert
        response.Me.Should().Be(new DashboardMeResponse(0, 0, 0, 0));
        response.Team.Should().Be(new DashboardTeamResponse(0, 0, 0));
        response.Aging.Buckets.Sum(b => b.Count).Should().Be(0);
        response.Milestones.Should().BeEmpty();
        response.Portfolio.Should().ContainSingle().Which.ProjectId.Should().Be(active.Id);
    }

    [Fact]
    public void Should_ListTheDistribution_InSortOrder_IncludingEmptyStates()
    {
        // Arrange
        DashboardProjectRow project = ProjectRow("Aurora");
        DashboardStateRow review = StateRow(project, "Review", 3);
        DashboardStateRow backlog = StateRow(project, "Backlog", 1);
        DashboardStateRow inProgress = StateRow(project, "In Progress", 2);
        DashboardSnapshot snapshot = Snapshot([project], [review, backlog, inProgress], [GroupRow(inProgress, 2)]);

        // Act
        DashboardResponse response = DashboardCalculator.Build(snapshot);

        // Assert
        response.Portfolio.Single().Distribution.Select(d => (d.Name, d.Count)).Should().Equal(
            ("Backlog", 0),
            ("In Progress", 2),
            ("Review", 0));
    }

    [Fact]
    public void Should_OrderThePortfolio_LikeTheSidebar()
    {
        // Arrange
        DashboardSnapshot snapshot = Snapshot(
            [
                ProjectRow("zeta"),
                ProjectRow("Alpha", ProjectStatus.Maintenance),
                ProjectRow("beta")
            ],
            []);

        // Act
        DashboardResponse response = DashboardCalculator.Build(snapshot);

        // Assert
        response.Portfolio.Select(p => p.Name).Should().Equal("beta", "zeta", "Alpha");
    }

    [Fact]
    public void Should_OrderMilestones_DatedFirst_ThenUndatedByMostStalled()
    {
        // Arrange
        DashboardProjectRow project = ProjectRow("Aurora");
        DashboardStateRow backlog = StateRow(project, "Backlog", 1);
        DashboardStateRow inProgress = StateRow(project, "In Progress", 2);
        DashboardMilestoneRow late = MilestoneRow(project, "Late", new DateOnly(2026, 4, 1));
        DashboardMilestoneRow overdue = MilestoneRow(project, "Overdue", new DateOnly(2026, 1, 15));
        DashboardMilestoneRow quiet = MilestoneRow(project, "Quiet", null);
        DashboardMilestoneRow busy = MilestoneRow(project, "Busy", null);
        DashboardSnapshot snapshot = Snapshot(
            [project],
            [backlog, inProgress],
            [
                GroupRow(inProgress, 1, DashboardCalculator.StalledBand, milestoneId: quiet.Id),
                GroupRow(inProgress, 4, DashboardCalculator.StalledBand, milestoneId: busy.Id)
            ],
            [late, quiet, overdue, busy]);

        // Act
        DashboardResponse response = DashboardCalculator.Build(snapshot);

        // Assert
        response.Milestones.Select(m => m.Name).Should().Equal("Overdue", "Late", "Busy", "Quiet");
    }

    [Fact]
    public void Should_BreakMilestoneTies_ByName()
    {
        // Arrange
        DashboardProjectRow project = ProjectRow("Aurora");
        DateOnly targetEndDate = new(2026, 3, 1);
        DashboardSnapshot snapshot = Snapshot(
            [project],
            [],
            milestones: [MilestoneRow(project, "beta", targetEndDate), MilestoneRow(project, "Alpha", targetEndDate)]);

        // Act
        DashboardResponse response = DashboardCalculator.Build(snapshot);

        // Assert
        response.Milestones.Select(m => m.Name).Should().Equal("Alpha", "beta");
    }

    [Fact]
    public void Should_ComputeTheLinkedCounts_OfAMilestone()
    {
        // Arrange
        DashboardProjectRow project = ProjectRow("Aurora");
        DashboardStateRow backlog = StateRow(project, "Backlog", 1);
        DashboardStateRow inProgress = StateRow(project, "In Progress", 2);
        DashboardMilestoneRow milestone = MilestoneRow(project, "Sprint 1", new DateOnly(2026, 3, 15));
        DashboardSnapshot snapshot = Snapshot(
            [project],
            [backlog, inProgress],
            [
                GroupRow(backlog, 2, milestoneId: milestone.Id),
                GroupRow(inProgress, 3, DashboardCalculator.StalledBand, milestoneId: milestone.Id),
                GroupRow(inProgress, 7)
            ],
            [milestone],
            new Dictionary<Guid, int> { [milestone.Id] = 4 });

        // Act
        DashboardResponse response = DashboardCalculator.Build(snapshot);

        // Assert
        DashboardMilestoneResponse linked = response.Milestones.Single();
        linked.LinkedTotal.Should().Be(9);
        linked.LinkedClosed.Should().Be(4);
        linked.LinkedStalled.Should().Be(3);
        response.Portfolio.Single().ActiveMilestone.Should().Be(new DashboardActiveMilestoneResponse(
            milestone.Id, "Sprint 1", MilestoneColor, new DateOnly(2026, 3, 15), 9, 4));
    }

    [Fact]
    public void Should_PickTheActiveMilestone_WithTheEarliestTargetEndDate_OverdueIncluded()
    {
        // Arrange
        DashboardProjectRow project = ProjectRow("Aurora");
        DashboardMilestoneRow upcoming = MilestoneRow(project, "Upcoming", new DateOnly(2026, 6, 1));
        DashboardMilestoneRow overdue = MilestoneRow(project, "Overdue", new DateOnly(2026, 1, 10));
        DashboardMilestoneRow undated = MilestoneRow(project, "Undated", null, CreatedOnUtc.AddDays(30));
        DashboardSnapshot snapshot = Snapshot([project], [], milestones: [upcoming, undated, overdue]);

        // Act
        DashboardResponse response = DashboardCalculator.Build(snapshot);

        // Assert
        response.Portfolio.Single().ActiveMilestone!.Id.Should().Be(overdue.Id);
    }

    [Fact]
    public void Should_PickTheMostRecentlyCreatedMilestone_When_NoneHasATargetDate()
    {
        // Arrange
        DashboardProjectRow project = ProjectRow("Aurora");
        DashboardMilestoneRow older = MilestoneRow(project, "Older", null, CreatedOnUtc);
        DashboardMilestoneRow newer = MilestoneRow(project, "Newer", null, CreatedOnUtc.AddDays(5));
        DashboardSnapshot snapshot = Snapshot([project], [], milestones: [older, newer]);

        // Act
        DashboardResponse response = DashboardCalculator.Build(snapshot);

        // Assert
        response.Portfolio.Single().ActiveMilestone!.Id.Should().Be(newer.Id);
    }

    [Fact]
    public void Should_OrderMembers_ByFullName()
    {
        // Arrange
        DashboardProjectRow project = ProjectRow("Aurora");
        DashboardSnapshot snapshot = Snapshot(
            [project],
            [],
            members: [MemberRow(project, "Zoe", "Young"), MemberRow(project, "adam", "Smith")]);

        // Act
        DashboardResponse response = DashboardCalculator.Build(snapshot);

        // Assert
        response.Portfolio.Single().Members.Select(m => (m.FullName, m.Initials)).Should().Equal(
            ("adam Smith", "AS"),
            ("Zoe Young", "ZY"));
    }
}
