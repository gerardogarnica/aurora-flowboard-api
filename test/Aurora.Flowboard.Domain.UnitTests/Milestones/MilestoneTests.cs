namespace Aurora.Flowboard.Domain.UnitTests.Milestones;

public sealed class MilestoneTests
{
    public sealed class Create : BaseTest
    {
        [Fact]
        public void Should_CreateMilestone_When_AdminCreates()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);

            // Act
            Result<Milestone> result = Milestone.Create(
                MilestoneData.Name,
                MilestoneData.Description,
                MilestoneData.TargetStartDate,
                MilestoneData.TargetEndDate,
                project,
                admin,
                MilestoneData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            result.Value.Name.Should().Be(MilestoneData.Name);
            result.Value.Description.Should().Be(MilestoneData.Description);
            result.Value.Status.Should().Be(MilestoneStatus.Draft);
            result.Value.TargetStartDate.Should().Be(MilestoneData.TargetStartDate);
            result.Value.TargetEndDate.Should().Be(MilestoneData.TargetEndDate);
            result.Value.ProjectId.Should().Be(project.Id);
            result.Value.CreatedBy.Should().Be(admin.Id);
            result.Value.CreatedOnUtc.Should().Be(MilestoneData.CreatedOnUtc);
        }

        [Fact]
        public void Should_CreateMilestone_When_OptionalFieldsAreNull()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);

            // Act
            Result<Milestone> result = Milestone.Create(
                MilestoneData.Name,
                null,
                null,
                null,
                project,
                admin,
                MilestoneData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            result.Value.Description.Should().BeNull();
            result.Value.TargetStartDate.Should().BeNull();
            result.Value.TargetEndDate.Should().BeNull();
        }

        [Fact]
        public void Should_AddMilestoneToProject_When_Created()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);

            // Act
            Result<Milestone> result = Milestone.Create(
                MilestoneData.Name,
                MilestoneData.Description,
                MilestoneData.TargetStartDate,
                MilestoneData.TargetEndDate,
                project,
                admin,
                MilestoneData.CreatedOnUtc);

            // Assert
            project.Milestones.Should().ContainSingle(m => m.Id == result.Value.Id);
            result.Value.Project.Should().BeSameAs(project);
        }

        [Fact]
        public void Should_TrimName_When_Created()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);

            // Act
            Result<Milestone> result = Milestone.Create(
                "  Phase 1 delivery  ",
                MilestoneData.Description,
                MilestoneData.TargetStartDate,
                MilestoneData.TargetEndDate,
                project,
                admin,
                MilestoneData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            result.Value.Name.Should().Be(MilestoneData.Name);
        }

        [Fact]
        public void Should_RaiseMilestoneCreatedDomainEvent_When_Created()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);

            // Act
            Milestone milestone = MilestoneData.GetMilestone(project, admin);

            // Assert
            MilestoneCreatedDomainEvent domainEvent = AssertDomainEventWasPublished<MilestoneCreatedDomainEvent>(milestone);
            domainEvent.MilestoneId.Should().Be(milestone.Id);
            domainEvent.ProjectId.Should().Be(project.Id);
        }

        [Fact]
        public void Should_Fail_When_NonAdminCreates()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            User nonAdmin = UserData.GetActiveUser();

            // Act
            Result<Milestone> result = Milestone.Create(
                MilestoneData.Name,
                MilestoneData.Description,
                MilestoneData.TargetStartDate,
                MilestoneData.TargetEndDate,
                project,
                nonAdmin,
                MilestoneData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(MilestoneErrors.OnlyAdminCanManageMilestone);
        }

        [Fact]
        public void Should_Fail_When_NameIsEmpty()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);

            // Act
            Result<Milestone> result = Milestone.Create(
                string.Empty,
                MilestoneData.Description,
                MilestoneData.TargetStartDate,
                MilestoneData.TargetEndDate,
                project,
                admin,
                MilestoneData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(MilestoneErrors.NameRequired);
        }

        [Fact]
        public void Should_Fail_When_NameExceedsMaxLength()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            string longName = new('A', Milestone.MaxNameLength + 1);

            // Act
            Result<Milestone> result = Milestone.Create(
                longName,
                MilestoneData.Description,
                MilestoneData.TargetStartDate,
                MilestoneData.TargetEndDate,
                project,
                admin,
                MilestoneData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(MilestoneErrors.NameTooLong);
        }

        [Fact]
        public void Should_Fail_When_DescriptionExceedsMaxLength()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            string longDescription = new('A', Milestone.MaxDescriptionLength + 1);

            // Act
            Result<Milestone> result = Milestone.Create(
                MilestoneData.Name,
                longDescription,
                MilestoneData.TargetStartDate,
                MilestoneData.TargetEndDate,
                project,
                admin,
                MilestoneData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(MilestoneErrors.DescriptionTooLong);
        }

        [Fact]
        public void Should_Fail_When_DuplicateNameCaseInsensitive()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            MilestoneData.GetMilestone(project, admin);

            // Act
            Result<Milestone> result = Milestone.Create(
                "phase 1 delivery",
                MilestoneData.Description,
                MilestoneData.TargetStartDate,
                MilestoneData.TargetEndDate,
                project,
                admin,
                MilestoneData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(MilestoneErrors.DuplicateName);
        }

        [Fact]
        public void Should_Fail_When_TargetEndDateBeforeTargetStartDate()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);

            // Act
            Result<Milestone> result = Milestone.Create(
                MilestoneData.Name,
                MilestoneData.Description,
                MilestoneData.TargetEndDate,
                MilestoneData.TargetStartDate,
                project,
                admin,
                MilestoneData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(MilestoneErrors.InvalidDateRange);
        }
    }

    public sealed class Update : BaseTest
    {
        [Fact]
        public void Should_UpdateMilestone_When_AdminUpdates()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            Milestone milestone = MilestoneData.GetMilestone(project, admin);

            // Act
            Result result = milestone.Update(
                MilestoneData.UpdatedName,
                MilestoneData.UpdatedDescription,
                MilestoneData.TargetStartDate,
                MilestoneData.TargetEndDate,
                admin,
                MilestoneData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            milestone.Name.Should().Be(MilestoneData.UpdatedName);
            milestone.Description.Should().Be(MilestoneData.UpdatedDescription);
            milestone.UpdatedOnUtc.Should().Be(MilestoneData.UpdatedOnUtc);
        }

        [Fact]
        public void Should_RaiseMilestoneUpdatedDomainEvent_When_Updated()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            Milestone milestone = MilestoneData.GetMilestone(project, admin);

            // Act
            milestone.Update(
                MilestoneData.UpdatedName,
                MilestoneData.UpdatedDescription,
                MilestoneData.TargetStartDate,
                MilestoneData.TargetEndDate,
                admin,
                MilestoneData.UpdatedOnUtc);

            // Assert
            MilestoneUpdatedDomainEvent domainEvent = AssertDomainEventWasPublished<MilestoneUpdatedDomainEvent>(milestone);
            domainEvent.MilestoneId.Should().Be(milestone.Id);
        }

        [Fact]
        public void Should_Fail_When_NonAdminUpdates()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            Milestone milestone = MilestoneData.GetMilestone(project, admin);
            User nonAdmin = UserData.GetActiveUser();

            // Act
            Result result = milestone.Update(
                MilestoneData.UpdatedName,
                MilestoneData.UpdatedDescription,
                MilestoneData.TargetStartDate,
                MilestoneData.TargetEndDate,
                nonAdmin,
                MilestoneData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(MilestoneErrors.OnlyAdminCanManageMilestone);
        }

        [Fact]
        public void Should_Fail_When_NameIsEmpty()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            Milestone milestone = MilestoneData.GetMilestone(project, admin);

            // Act
            Result result = milestone.Update(
                string.Empty,
                MilestoneData.UpdatedDescription,
                MilestoneData.TargetStartDate,
                MilestoneData.TargetEndDate,
                admin,
                MilestoneData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(MilestoneErrors.NameRequired);
        }

        [Fact]
        public void Should_Fail_When_NameExceedsMaxLength()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            Milestone milestone = MilestoneData.GetMilestone(project, admin);
            string longName = new('A', Milestone.MaxNameLength + 1);

            // Act
            Result result = milestone.Update(
                longName,
                MilestoneData.UpdatedDescription,
                MilestoneData.TargetStartDate,
                MilestoneData.TargetEndDate,
                admin,
                MilestoneData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(MilestoneErrors.NameTooLong);
        }

        [Fact]
        public void Should_Fail_When_DescriptionExceedsMaxLength()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            Milestone milestone = MilestoneData.GetMilestone(project, admin);
            string longDescription = new('A', Milestone.MaxDescriptionLength + 1);

            // Act
            Result result = milestone.Update(
                MilestoneData.UpdatedName,
                longDescription,
                MilestoneData.TargetStartDate,
                MilestoneData.TargetEndDate,
                admin,
                MilestoneData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(MilestoneErrors.DescriptionTooLong);
        }

        [Fact]
        public void Should_Fail_When_TargetEndDateBeforeTargetStartDate()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            Milestone milestone = MilestoneData.GetMilestone(project, admin);

            // Act
            Result result = milestone.Update(
                MilestoneData.UpdatedName,
                MilestoneData.UpdatedDescription,
                MilestoneData.TargetEndDate,
                MilestoneData.TargetStartDate,
                admin,
                MilestoneData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(MilestoneErrors.InvalidDateRange);
        }

        [Fact]
        public void Should_Fail_When_MilestoneIsCompleted()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            Milestone milestone = MilestoneData.GetMilestoneWithStatus(MilestoneStatus.Completed, project, admin);

            // Act
            Result result = milestone.Update(
                MilestoneData.UpdatedName,
                MilestoneData.UpdatedDescription,
                MilestoneData.TargetStartDate,
                MilestoneData.TargetEndDate,
                admin,
                MilestoneData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(MilestoneErrors.OperationNotAllowedInCurrentStatus);
        }

        [Fact]
        public void Should_Fail_When_MilestoneIsArchived()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            Milestone milestone = MilestoneData.GetMilestoneWithStatus(MilestoneStatus.Archived, project, admin);

            // Act
            Result result = milestone.Update(
                MilestoneData.UpdatedName,
                MilestoneData.UpdatedDescription,
                MilestoneData.TargetStartDate,
                MilestoneData.TargetEndDate,
                admin,
                MilestoneData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(MilestoneErrors.OperationNotAllowedInCurrentStatus);
        }
    }

    public sealed class ChangeStatus : BaseTest
    {
        [Fact]
        public void Should_TransitionToActive_When_CurrentStatusIsDraft()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            Milestone milestone = MilestoneData.GetMilestone(project, admin);

            // Act
            Result result = milestone.ChangeStatus(MilestoneStatus.Active, admin, 0, MilestoneData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            milestone.Status.Should().Be(MilestoneStatus.Active);
            milestone.UpdatedOnUtc.Should().Be(MilestoneData.UpdatedOnUtc);
        }

        [Fact]
        public void Should_TransitionToArchived_When_CurrentStatusIsDraft()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            Milestone milestone = MilestoneData.GetMilestone(project, admin);

            // Act
            Result result = milestone.ChangeStatus(MilestoneStatus.Archived, admin, 0, MilestoneData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            milestone.Status.Should().Be(MilestoneStatus.Archived);
        }

        [Fact]
        public void Should_TransitionToOnHold_When_CurrentStatusIsActive()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            Milestone milestone = MilestoneData.GetMilestoneWithStatus(MilestoneStatus.Active, project, admin);

            // Act
            Result result = milestone.ChangeStatus(MilestoneStatus.OnHold, admin, 0, MilestoneData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            milestone.Status.Should().Be(MilestoneStatus.OnHold);
        }

        [Fact]
        public void Should_TransitionToCompleted_When_CurrentStatusIsActive()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            Milestone milestone = MilestoneData.GetMilestoneWithStatus(MilestoneStatus.Active, project, admin);

            // Act
            Result result = milestone.ChangeStatus(MilestoneStatus.Completed, admin, 0, MilestoneData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            milestone.Status.Should().Be(MilestoneStatus.Completed);
        }

        [Fact]
        public void Should_TransitionToActive_When_CurrentStatusIsOnHold()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            Milestone milestone = MilestoneData.GetMilestoneWithStatus(MilestoneStatus.OnHold, project, admin);

            // Act
            Result result = milestone.ChangeStatus(MilestoneStatus.Active, admin, 0, MilestoneData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            milestone.Status.Should().Be(MilestoneStatus.Active);
        }

        [Fact]
        public void Should_Fail_When_TransitionFromOnHoldToCompleted()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            Milestone milestone = MilestoneData.GetMilestoneWithStatus(MilestoneStatus.OnHold, project, admin);

            // Act
            Result result = milestone.ChangeStatus(MilestoneStatus.Completed, admin, 0, MilestoneData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(MilestoneErrors.InvalidStatusTransition);
        }

        [Fact]
        public void Should_TransitionToArchived_When_CurrentStatusIsCompleted()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            Milestone milestone = MilestoneData.GetMilestoneWithStatus(MilestoneStatus.Completed, project, admin);

            // Act
            Result result = milestone.ChangeStatus(MilestoneStatus.Archived, admin, 0, MilestoneData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            milestone.Status.Should().Be(MilestoneStatus.Archived);
        }

        [Fact]
        public void Should_RaiseMilestoneStatusChangedDomainEvent_When_StatusChanges()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            Milestone milestone = MilestoneData.GetMilestone(project, admin);

            // Act
            milestone.ChangeStatus(MilestoneStatus.Active, admin, 0, MilestoneData.UpdatedOnUtc);

            // Assert
            MilestoneStatusChangedDomainEvent domainEvent =
                AssertDomainEventWasPublished<MilestoneStatusChangedDomainEvent>(milestone);
            domainEvent.MilestoneId.Should().Be(milestone.Id);
            domainEvent.OldStatus.Should().Be(MilestoneStatus.Draft);
            domainEvent.NewStatus.Should().Be(MilestoneStatus.Active);
        }

        [Fact]
        public void Should_Fail_When_TransitionFromDraftToCompleted()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            Milestone milestone = MilestoneData.GetMilestone(project, admin);

            // Act
            Result result = milestone.ChangeStatus(MilestoneStatus.Completed, admin, 0, MilestoneData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(MilestoneErrors.InvalidStatusTransition);
        }

        [Fact]
        public void Should_Fail_When_TransitionFromCompletedToActive()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            Milestone milestone = MilestoneData.GetMilestoneWithStatus(MilestoneStatus.Completed, project, admin);

            // Act
            Result result = milestone.ChangeStatus(MilestoneStatus.Active, admin, 0, MilestoneData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(MilestoneErrors.InvalidStatusTransition);
        }

        [Fact]
        public void Should_Fail_When_TransitionFromArchivedToAnyStatus()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            Milestone milestone = MilestoneData.GetMilestoneWithStatus(MilestoneStatus.Archived, project, admin);

            // Act
            Result result = milestone.ChangeStatus(MilestoneStatus.Active, admin, 0, MilestoneData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(MilestoneErrors.InvalidStatusTransition);
        }

        [Fact]
        public void Should_Fail_When_NewStatusEqualsCurrentStatus()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            Milestone milestone = MilestoneData.GetMilestone(project, admin);

            // Act
            Result result = milestone.ChangeStatus(MilestoneStatus.Draft, admin, 0, MilestoneData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(MilestoneErrors.InvalidStatusTransition);
        }

        [Fact]
        public void Should_Fail_When_NonAdminChangesStatus()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            Milestone milestone = MilestoneData.GetMilestone(project, admin);
            User nonAdmin = UserData.GetActiveUser();

            // Act
            Result result = milestone.ChangeStatus(MilestoneStatus.Active, nonAdmin, 0, MilestoneData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(MilestoneErrors.OnlyAdminCanManageMilestone);
        }

        [Fact]
        public void Should_Fail_When_CompletingWithOpenWorkItems()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            Milestone milestone = MilestoneData.GetMilestoneWithStatus(MilestoneStatus.Active, project, admin);

            // Act
            Result result = milestone.ChangeStatus(MilestoneStatus.Completed, admin, 1, MilestoneData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(MilestoneErrors.CannotCloseWithOpenWorkItems);
        }

        [Fact]
        public void Should_Fail_When_ArchivingWithOpenWorkItems()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            Milestone milestone = MilestoneData.GetMilestone(project, admin);

            // Act
            Result result = milestone.ChangeStatus(MilestoneStatus.Archived, admin, 1, MilestoneData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(MilestoneErrors.CannotCloseWithOpenWorkItems);
        }
    }
}
