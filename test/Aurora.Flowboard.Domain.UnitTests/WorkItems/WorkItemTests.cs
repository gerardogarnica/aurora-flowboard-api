namespace Aurora.Flowboard.Domain.UnitTests.WorkItems;

public sealed class WorkItemTests
{
    public sealed class Create : BaseTest
    {
        [Fact]
        public void Should_CreateWorkItem_When_DataIsValid()
        {
            // Arrange
            var (project, admin) = WorkItemData.GetActiveProjectWithFlow();

            // Act
            Result<WorkItem> result = WorkItem.Create(
                WorkItemData.Title,
                WorkItemData.Description,
                WorkItemData.Type,
                WorkItemData.Priority,
                project,
                
                admin,
                WorkItemData.EstimatedPoints,
                WorkItemData.EstimatedCompletionDate,
                WorkItemData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            result.Value.Title.Should().Be(WorkItemData.Title);
            result.Value.Description.Should().Be(WorkItemData.Description);
            result.Value.Type.Should().Be(WorkItemData.Type);
            result.Value.Priority.Should().Be(WorkItemData.Priority);
            result.Value.ProjectId.Should().Be(project.Id);
            result.Value.CreatedById.Should().Be(admin.Id);
            result.Value.AssigneeId.Should().BeNull();
            result.Value.EstimatedPoints.Should().Be(WorkItemData.EstimatedPoints);
            result.Value.EstimatedCompletionDate.Should().Be(WorkItemData.EstimatedCompletionDate);
            result.Value.CreatedOnUtc.Should().Be(WorkItemData.CreatedOnUtc);
        }

        [Fact]
        public void Should_GenerateCode_When_Created()
        {
            // Arrange
            var (project, admin) = WorkItemData.GetActiveProjectWithFlow();
            int expectedSequence = project.WorkItemCounter + 1;

            // Act
            Result<WorkItem> result = WorkItem.Create(
                WorkItemData.Title,
                null,
                WorkItemData.Type,
                WorkItemData.Priority,
                project,
                
                admin,
                null,
                null,
                WorkItemData.CreatedOnUtc);

            // Assert
            result.Value.Code.Should().Be($"{project.Prefix}-{expectedSequence}");
            result.Value.SequenceNumber.Should().Be(expectedSequence);
        }

        [Fact]
        public void Should_IncrementProjectWorkItemCounter_When_Created()
        {
            // Arrange
            var (project, admin) = WorkItemData.GetActiveProjectWithFlow();
            int counterBefore = project.WorkItemCounter;

            // Act
            WorkItem.Create(WorkItemData.Title, null, WorkItemData.Type, WorkItemData.Priority,
                project, admin, null, null, WorkItemData.CreatedOnUtc);

            // Assert
            project.WorkItemCounter.Should().Be(counterBefore + 1);
        }

        [Fact]
        public void Should_SetInitialFlowState_When_Created()
        {
            // Arrange
            var (project, admin) = WorkItemData.GetActiveProjectWithFlow();
            FlowState expectedInitialState = project.FlowStates
                .Where(s => s.Category == FlowStateCategory.Active)
                .MinBy(s => s.SortOrder)!;

            // Act
            Result<WorkItem> result = WorkItem.Create(
                WorkItemData.Title, null, WorkItemData.Type, WorkItemData.Priority,
                project, admin, null, null, WorkItemData.CreatedOnUtc);

            // Assert
            result.Value.FlowStateId.Should().Be(expectedInitialState.Id);
        }

        [Fact]
        public void Should_TrimTitleAndDescription_When_Created()
        {
            // Arrange
            var (project, admin) = WorkItemData.GetActiveProjectWithFlow();

            // Act
            Result<WorkItem> result = WorkItem.Create(
                $"  {WorkItemData.Title}  ",
                $"  {WorkItemData.Description}  ",
                WorkItemData.Type,
                WorkItemData.Priority,
                project,
                
                admin,
                null,
                null,
                WorkItemData.CreatedOnUtc);

            // Assert
            result.Value.Title.Should().Be(WorkItemData.Title);
            result.Value.Description.Should().Be(WorkItemData.Description);
        }

        [Fact]
        public void Should_CreateChangeLog_When_Created()
        {
            // Arrange
            var (project, admin) = WorkItemData.GetActiveProjectWithFlow();

            // Act
            Result<WorkItem> result = WorkItem.Create(
                WorkItemData.Title, null, WorkItemData.Type, WorkItemData.Priority,
                project, admin, null, null, WorkItemData.CreatedOnUtc);

            // Assert
            result.Value.ChangeLogs.Should().ContainSingle(c => c.ChangeType == WorkItemChangeType.Created);
        }

        [Fact]
        public void Should_RaiseWorkItemCreatedDomainEvent_When_Created()
        {
            // Arrange
            var (project, admin) = WorkItemData.GetActiveProjectWithFlow();

            // Act
            Result<WorkItem> result = WorkItem.Create(
                WorkItemData.Title, null, WorkItemData.Type, WorkItemData.Priority,
                project, admin, null, null, WorkItemData.CreatedOnUtc);

            // Assert
            WorkItemCreatedDomainEvent domainEvent = AssertDomainEventWasPublished<WorkItemCreatedDomainEvent>(result.Value);
            domainEvent.WorkItemId.Should().Be(result.Value.Id);
        }

        [Fact]
        public void Should_CreateWithAssignee_When_AssigneeIsProjectMemberAndActive()
        {
            // Arrange
            var (project, admin) = WorkItemData.GetActiveProjectWithFlow();
            User assignee = UserData.GetActiveUser();
            project.AddMember(assignee, ProjectRole.Developer, admin, WorkItemData.CreatedOnUtc);

            // Act
            Result<WorkItem> result = WorkItem.Create(
                WorkItemData.Title, null, WorkItemData.Type, WorkItemData.Priority,
                project, admin, null, null, WorkItemData.CreatedOnUtc, assignee);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            result.Value.AssigneeId.Should().Be(assignee.Id);
        }

        [Fact]
        public void Should_RaiseWorkItemAssignedDomainEvent_When_CreatedWithAssignee()
        {
            // Arrange
            var (project, admin) = WorkItemData.GetActiveProjectWithFlow();
            User assignee = UserData.GetActiveUser();
            project.AddMember(assignee, ProjectRole.Developer, admin, WorkItemData.CreatedOnUtc);

            // Act
            Result<WorkItem> result = WorkItem.Create(
                WorkItemData.Title, null, WorkItemData.Type, WorkItemData.Priority,
                project, admin, null, null, WorkItemData.CreatedOnUtc, assignee);

            // Assert
            WorkItemAssignedDomainEvent domainEvent = AssertDomainEventWasPublished<WorkItemAssignedDomainEvent>(result.Value);
            domainEvent.WorkItemId.Should().Be(result.Value.Id);
            domainEvent.AssigneeId.Should().Be(assignee.Id);
        }

        [Fact]
        public void Should_CreateAssignedChangeLog_When_CreatedWithAssignee()
        {
            // Arrange
            var (project, admin) = WorkItemData.GetActiveProjectWithFlow();
            User assignee = UserData.GetActiveUser();
            project.AddMember(assignee, ProjectRole.Developer, admin, WorkItemData.CreatedOnUtc);

            // Act
            Result<WorkItem> result = WorkItem.Create(
                WorkItemData.Title, null, WorkItemData.Type, WorkItemData.Priority,
                project, admin, null, null, WorkItemData.CreatedOnUtc, assignee);

            // Assert
            result.Value.ChangeLogs.Should().Contain(c => c.ChangeType == WorkItemChangeType.Assigned);
        }

        [Fact]
        public void Should_Fail_When_TitleIsEmpty()
        {
            // Arrange
            var (project, admin) = WorkItemData.GetActiveProjectWithFlow();

            // Act
            Result<WorkItem> result = WorkItem.Create(
                string.Empty, null, WorkItemData.Type, WorkItemData.Priority,
                project, admin, null, null, WorkItemData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.TitleRequired);
        }

        [Fact]
        public void Should_Fail_When_TitleIsWhitespace()
        {
            // Arrange
            var (project, admin) = WorkItemData.GetActiveProjectWithFlow();

            // Act
            Result<WorkItem> result = WorkItem.Create(
                "   ", null, WorkItemData.Type, WorkItemData.Priority,
                project, admin, null, null, WorkItemData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.TitleRequired);
        }

        [Fact]
        public void Should_Fail_When_TitleExceedsMaxLength()
        {
            // Arrange
            var (project, admin) = WorkItemData.GetActiveProjectWithFlow();
            string longTitle = new('A', 201);

            // Act
            Result<WorkItem> result = WorkItem.Create(
                longTitle, null, WorkItemData.Type, WorkItemData.Priority,
                project, admin, null, null, WorkItemData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.TitleTooLong);
        }

        [Fact]
        public void Should_Fail_When_DescriptionExceedsMaxLength()
        {
            // Arrange
            var (project, admin) = WorkItemData.GetActiveProjectWithFlow();
            string longDescription = new('A', 4001);

            // Act
            Result<WorkItem> result = WorkItem.Create(
                WorkItemData.Title, longDescription, WorkItemData.Type, WorkItemData.Priority,
                project, admin, null, null, WorkItemData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.DescriptionTooLong);
        }

        [Fact]
        public void Should_Fail_When_CreatorIsNotProjectMember()
        {
            // Arrange
            var (project, _) = WorkItemData.GetActiveProjectWithFlow();
            User nonMember = UserData.GetActiveUser();

            // Act
            Result<WorkItem> result = WorkItem.Create(
                WorkItemData.Title, null, WorkItemData.Type, WorkItemData.Priority,
                project, nonMember, null, null, WorkItemData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.NotFound);
        }

        [Fact]
        public void Should_Fail_When_ProjectDoesNotAllowWorkItems()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project blockedProject = ProjectData.GetProject(admin);
            blockedProject.AddFlowState("Backlog", FlowStateCategory.Active, ProjectData.FlowStateColor, [ProjectRole.Admin], admin);
            blockedProject.ChangeStatus(ProjectStatus.Archived, admin, ProjectData.UpdatedOnUtc);

            // Act
            Result<WorkItem> result = WorkItem.Create(
                WorkItemData.Title, null, WorkItemData.Type, WorkItemData.Priority,
                blockedProject, admin, null, null, WorkItemData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        [Fact]
        public void Should_Fail_When_CreatorIsInactive()
        {
            // Arrange
            var (project, admin) = WorkItemData.GetActiveProjectWithFlow();
            User creator = UserData.GetActiveUser();
            project.AddMember(creator, ProjectRole.Developer, admin, WorkItemData.CreatedOnUtc);
            creator.Deactivate(WorkItemData.CreatedOnUtc);

            // Act
            Result<WorkItem> result = WorkItem.Create(
                WorkItemData.Title, null, WorkItemData.Type, WorkItemData.Priority,
                project, creator, null, null, WorkItemData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(UserErrors.Inactive);
        }

        [Fact]
        public void Should_Fail_When_FlowHasNoInitialState()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            // No flow states added - the project has no initial state

            // Act
            Result<WorkItem> result = WorkItem.Create(
                WorkItemData.Title, null, WorkItemData.Type, WorkItemData.Priority,
                project, admin, null, null, WorkItemData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.NoInitialFlowState);
        }

        [Fact]
        public void Should_Fail_When_AssigneeIsNotProjectMember()
        {
            // Arrange
            var (project, admin) = WorkItemData.GetActiveProjectWithFlow();
            User nonMemberAssignee = UserData.GetActiveUser();

            // Act
            Result<WorkItem> result = WorkItem.Create(
                WorkItemData.Title, null, WorkItemData.Type, WorkItemData.Priority,
                project, admin, null, null, WorkItemData.CreatedOnUtc, nonMemberAssignee);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.AssigneeNotProjectMember);
        }

        [Fact]
        public void Should_Fail_When_AssigneeIsInactive()
        {
            // Arrange
            var (project, admin) = WorkItemData.GetActiveProjectWithFlow();
            User assignee = UserData.GetActiveUser();
            project.AddMember(assignee, ProjectRole.Developer, admin, WorkItemData.CreatedOnUtc);
            assignee.Deactivate(WorkItemData.CreatedOnUtc);

            // Act
            Result<WorkItem> result = WorkItem.Create(
                WorkItemData.Title, null, WorkItemData.Type, WorkItemData.Priority,
                project, admin, null, null, WorkItemData.CreatedOnUtc, assignee);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.AssigneeInactive);
        }
    }

    public sealed class UpdateTitle : BaseTest
    {
        [Fact]
        public void Should_UpdateTitle_When_DataIsValid()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();
            const string newTitle = "Updated title";

            // Act
            Result result = workItem.UpdateTitle(newTitle, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.Title.Should().Be(newTitle);
            workItem.UpdatedOnUtc.Should().Be(WorkItemData.UpdatedOnUtc);
        }

        [Fact]
        public void Should_TrimTitle_When_Updated()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            workItem.UpdateTitle("  New Title  ", admin, WorkItemData.UpdatedOnUtc);

            // Assert
            workItem.Title.Should().Be("New Title");
        }

        [Fact]
        public void Should_CreateTitleUpdatedChangeLog_When_Updated()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            workItem.UpdateTitle("New Title", admin, WorkItemData.UpdatedOnUtc);

            // Assert
            workItem.ChangeLogs.Should().Contain(c => c.ChangeType == WorkItemChangeType.TitleUpdated);
        }

        [Fact]
        public void Should_RaiseWorkItemTitleUpdatedDomainEvent_When_Updated()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            workItem.UpdateTitle("New Title", admin, WorkItemData.UpdatedOnUtc);

            // Assert
            WorkItemTitleUpdatedDomainEvent domainEvent = AssertDomainEventWasPublished<WorkItemTitleUpdatedDomainEvent>(workItem);
            domainEvent.WorkItemId.Should().Be(workItem.Id);
            domainEvent.NewTitle.Should().Be("New Title");
        }

        [Fact]
        public void Should_BeNoOp_When_TitleIsUnchanged()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.UpdateTitle(WorkItemData.Title, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.UpdatedOnUtc.Should().BeNull();
            workItem.ChangeLogs.Should().NotContain(c => c.ChangeType == WorkItemChangeType.TitleUpdated);
            workItem.DomainEvents.OfType<WorkItemTitleUpdatedDomainEvent>().Should().BeEmpty();
        }

        [Fact]
        public void Should_Fail_When_TitleIsEmpty()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.UpdateTitle(string.Empty, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.TitleRequired);
        }

        [Fact]
        public void Should_Fail_When_TitleIsWhitespace()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.UpdateTitle("   ", admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.TitleRequired);
        }

        [Fact]
        public void Should_Fail_When_TitleExceedsMaxLength()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();
            string longTitle = new('A', WorkItem.MaxTitleLength + 1);

            // Act
            Result result = workItem.UpdateTitle(longTitle, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.TitleTooLong);
        }

        [Fact]
        public void Should_Fail_When_ChangedByIsNotProjectMember()
        {
            // Arrange
            var (workItem, _, _) = WorkItemData.GetWorkItemWithContext();
            User nonMember = UserData.GetActiveUser();

            // Act
            Result result = workItem.UpdateTitle("New Title", nonMember, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.NotFound);
        }

        [Fact]
        public void Should_Fail_When_ChangedByIsInactive()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            User user = UserData.GetActiveUser();
            project.AddMember(user, ProjectRole.Developer, admin, WorkItemData.CreatedOnUtc);
            user.Deactivate(WorkItemData.CreatedOnUtc);

            // Act
            Result result = workItem.UpdateTitle("New Title", user, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(UserErrors.Inactive);
        }

        [Fact]
        public void Should_Fail_When_ProjectDoesNotAllowWorkItems()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            project.ChangeStatus(ProjectStatus.Archived, admin, WorkItemData.UpdatedOnUtc);

            // Act
            Result result = workItem.UpdateTitle("New Title", admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }
    }

    public sealed class UpdateDescription : BaseTest
    {
        [Fact]
        public void Should_UpdateDescription_When_DataIsValid()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();
            const string newDescription = "Updated description";

            // Act
            Result result = workItem.UpdateDescription(newDescription, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.Description.Should().Be(newDescription);
            workItem.UpdatedOnUtc.Should().Be(WorkItemData.UpdatedOnUtc);
        }

        [Fact]
        public void Should_TrimDescription_When_Updated()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            workItem.UpdateDescription("  New description  ", admin, WorkItemData.UpdatedOnUtc);

            // Assert
            workItem.Description.Should().Be("New description");
        }

        [Fact]
        public void Should_ClearDescription_When_NullIsProvided()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.UpdateDescription(null, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.Description.Should().BeNull();
        }

        [Fact]
        public void Should_CreateDescriptionUpdatedChangeLog_When_Updated()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            workItem.UpdateDescription("New description", admin, WorkItemData.UpdatedOnUtc);

            // Assert
            workItem.ChangeLogs.Should().Contain(c => c.ChangeType == WorkItemChangeType.DescriptionUpdated);
        }

        [Fact]
        public void Should_BeNoOp_When_DescriptionIsUnchanged()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.UpdateDescription(WorkItemData.Description, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.UpdatedOnUtc.Should().BeNull();
            workItem.ChangeLogs.Should().NotContain(c => c.ChangeType == WorkItemChangeType.DescriptionUpdated);
        }

        [Fact]
        public void Should_Fail_When_DescriptionExceedsMaxLength()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();
            string longDescription = new('A', WorkItem.MaxDescriptionLength + 1);

            // Act
            Result result = workItem.UpdateDescription(longDescription, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.DescriptionTooLong);
        }

        [Fact]
        public void Should_Fail_When_ChangedByIsNotProjectMember()
        {
            // Arrange
            var (workItem, _, _) = WorkItemData.GetWorkItemWithContext();
            User nonMember = UserData.GetActiveUser();

            // Act
            Result result = workItem.UpdateDescription("New description", nonMember, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.NotFound);
        }

        [Fact]
        public void Should_Fail_When_ChangedByIsInactive()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            User user = UserData.GetActiveUser();
            project.AddMember(user, ProjectRole.Developer, admin, WorkItemData.CreatedOnUtc);
            user.Deactivate(WorkItemData.CreatedOnUtc);

            // Act
            Result result = workItem.UpdateDescription("New description", user, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(UserErrors.Inactive);
        }

        [Fact]
        public void Should_Fail_When_ProjectDoesNotAllowWorkItems()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            project.ChangeStatus(ProjectStatus.Archived, admin, WorkItemData.UpdatedOnUtc);

            // Act
            Result result = workItem.UpdateDescription("New description", admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }
    }

    public sealed class UpdateType : BaseTest
    {
        [Fact]
        public void Should_UpdateType_When_DataIsValid()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.UpdateType(WorkItemType.Bug, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.Type.Should().Be(WorkItemType.Bug);
            workItem.UpdatedOnUtc.Should().Be(WorkItemData.UpdatedOnUtc);
        }

        [Fact]
        public void Should_CreateTypeUpdatedChangeLog_When_Updated()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            workItem.UpdateType(WorkItemType.Bug, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            workItem.ChangeLogs.Should().Contain(c => c.ChangeType == WorkItemChangeType.TypeUpdated);
        }

        [Fact]
        public void Should_BeNoOp_When_TypeIsUnchanged()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.UpdateType(WorkItemData.Type, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.UpdatedOnUtc.Should().BeNull();
            workItem.ChangeLogs.Should().NotContain(c => c.ChangeType == WorkItemChangeType.TypeUpdated);
        }

        [Fact]
        public void Should_Fail_When_ChangedByIsNotProjectMember()
        {
            // Arrange
            var (workItem, _, _) = WorkItemData.GetWorkItemWithContext();
            User nonMember = UserData.GetActiveUser();

            // Act
            Result result = workItem.UpdateType(WorkItemType.Bug, nonMember, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.NotFound);
        }
    }

    public sealed class UpdatePriority : BaseTest
    {
        [Fact]
        public void Should_UpdatePriority_When_DataIsValid()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.UpdatePriority(Priority.High, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.Priority.Should().Be(Priority.High);
            workItem.UpdatedOnUtc.Should().Be(WorkItemData.UpdatedOnUtc);
        }

        [Fact]
        public void Should_CreatePriorityUpdatedChangeLog_When_Updated()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            workItem.UpdatePriority(Priority.High, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            workItem.ChangeLogs.Should().Contain(c => c.ChangeType == WorkItemChangeType.PriorityUpdated);
        }

        [Fact]
        public void Should_BeNoOp_When_PriorityIsUnchanged()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.UpdatePriority(WorkItemData.Priority, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.UpdatedOnUtc.Should().BeNull();
            workItem.ChangeLogs.Should().NotContain(c => c.ChangeType == WorkItemChangeType.PriorityUpdated);
        }

        [Fact]
        public void Should_Fail_When_ChangedByIsNotProjectMember()
        {
            // Arrange
            var (workItem, _, _) = WorkItemData.GetWorkItemWithContext();
            User nonMember = UserData.GetActiveUser();

            // Act
            Result result = workItem.UpdatePriority(Priority.High, nonMember, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.NotFound);
        }
    }

    public sealed class UpdateEstimatedPoints : BaseTest
    {
        private const int NewPoints = 8;

        [Fact]
        public void Should_UpdateEstimatedPoints_When_DataIsValid()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.UpdateEstimatedPoints(NewPoints, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.EstimatedPoints.Should().Be(NewPoints);
            workItem.UpdatedOnUtc.Should().Be(WorkItemData.UpdatedOnUtc);
        }

        [Fact]
        public void Should_ClearEstimatedPoints_When_NullIsProvided()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.UpdateEstimatedPoints(null, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.EstimatedPoints.Should().BeNull();
        }

        [Fact]
        public void Should_CreateEstimatedPointsUpdatedChangeLog_When_Updated()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            workItem.UpdateEstimatedPoints(NewPoints, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            workItem.ChangeLogs.Should().Contain(c => c.ChangeType == WorkItemChangeType.EstimatedPointsUpdated);
        }

        [Fact]
        public void Should_BeNoOp_When_EstimatedPointsIsUnchanged()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.UpdateEstimatedPoints(WorkItemData.EstimatedPoints, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.UpdatedOnUtc.Should().BeNull();
            workItem.ChangeLogs.Should().NotContain(c => c.ChangeType == WorkItemChangeType.EstimatedPointsUpdated);
        }

        [Fact]
        public void Should_Fail_When_EstimatedPointsIsZero()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.UpdateEstimatedPoints(0, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.EstimatedPointsInvalid);
        }

        [Fact]
        public void Should_Fail_When_EstimatedPointsIsNegative()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.UpdateEstimatedPoints(-1, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.EstimatedPointsInvalid);
        }

        [Fact]
        public void Should_Fail_When_ChangedByIsNotProjectMember()
        {
            // Arrange
            var (workItem, _, _) = WorkItemData.GetWorkItemWithContext();
            User nonMember = UserData.GetActiveUser();

            // Act
            Result result = workItem.UpdateEstimatedPoints(NewPoints, nonMember, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.NotFound);
        }
    }

    public sealed class UpdateEstimatedCompletionDate : BaseTest
    {
        private static readonly DateOnly NewDate = new(2026, 9, 30);

        [Fact]
        public void Should_UpdateEstimatedCompletionDate_When_DataIsValid()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.UpdateEstimatedCompletionDate(NewDate, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.EstimatedCompletionDate.Should().Be(NewDate);
            workItem.UpdatedOnUtc.Should().Be(WorkItemData.UpdatedOnUtc);
        }

        [Fact]
        public void Should_ClearEstimatedCompletionDate_When_NullIsProvided()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.UpdateEstimatedCompletionDate(null, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.EstimatedCompletionDate.Should().BeNull();
        }

        [Fact]
        public void Should_CreateEstimatedCompletionDateUpdatedChangeLog_When_Updated()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            workItem.UpdateEstimatedCompletionDate(NewDate, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            workItem.ChangeLogs.Should().Contain(c => c.ChangeType == WorkItemChangeType.EstimatedCompletionDateUpdated);
        }

        [Fact]
        public void Should_BeNoOp_When_EstimatedCompletionDateIsUnchanged()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.UpdateEstimatedCompletionDate(
                WorkItemData.EstimatedCompletionDate, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.UpdatedOnUtc.Should().BeNull();
            workItem.ChangeLogs.Should().NotContain(c => c.ChangeType == WorkItemChangeType.EstimatedCompletionDateUpdated);
        }

        [Fact]
        public void Should_Fail_When_ChangedByIsNotProjectMember()
        {
            // Arrange
            var (workItem, _, _) = WorkItemData.GetWorkItemWithContext();
            User nonMember = UserData.GetActiveUser();

            // Act
            Result result = workItem.UpdateEstimatedCompletionDate(NewDate, nonMember, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.NotFound);
        }
    }

    public sealed class ChangeComponent : BaseTest
    {
        [Fact]
        public void Should_SetComponent_When_DataIsValid()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            Component component = ComponentData.GetComponent(project, admin);

            // Act
            Result result = workItem.ChangeComponent(component, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.ComponentId.Should().Be(component.Id);
            workItem.Component.Should().Be(component);
            workItem.UpdatedOnUtc.Should().Be(WorkItemData.UpdatedOnUtc);
        }

        [Fact]
        public void Should_ClearComponent_When_NullIsProvided()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            Component component = ComponentData.GetComponent(project, admin);
            workItem.ChangeComponent(component, admin, WorkItemData.UpdatedOnUtc);

            // Act
            Result result = workItem.ChangeComponent(null, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.ComponentId.Should().BeNull();
            workItem.Component.Should().BeNull();
        }

        [Fact]
        public void Should_CreateComponentChangedChangeLog_WithComponentIdAsAffectedEntity()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            Component component = ComponentData.GetComponent(project, admin);

            // Act
            workItem.ChangeComponent(component, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            workItem.ChangeLogs.Should().Contain(c =>
                c.ChangeType == WorkItemChangeType.ComponentChanged &&
                c.AffectedEntityId == component.Id);
        }

        [Fact]
        public void Should_BeNoOp_When_ComponentIsUnchanged()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act — the work item was created without a component, so null is unchanged
            Result result = workItem.ChangeComponent(null, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.UpdatedOnUtc.Should().BeNull();
            workItem.ChangeLogs.Should().NotContain(c => c.ChangeType == WorkItemChangeType.ComponentChanged);
        }

        [Fact]
        public void Should_Fail_When_ComponentBelongsToAnotherProject()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();
            var (otherProject, otherAdmin) = WorkItemData.GetActiveProjectWithFlow();
            Component foreignComponent = ComponentData.GetComponent(otherProject, otherAdmin);

            // Act
            Result result = workItem.ChangeComponent(foreignComponent, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.ComponentNotInProject);
        }

        [Fact]
        public void Should_Fail_When_ComponentIsRetired()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            Component component = ComponentData.GetComponent(project, admin);
            component.Retire(admin, 0, WorkItemData.UpdatedOnUtc);

            // Act
            Result result = workItem.ChangeComponent(component, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.ComponentRetired);
        }

        [Fact]
        public void Should_Fail_When_ChangedByIsNotProjectMember()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            Component component = ComponentData.GetComponent(project, admin);
            User nonMember = UserData.GetActiveUser();

            // Act
            Result result = workItem.ChangeComponent(component, nonMember, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.NotFound);
        }
    }

    public sealed class Move : BaseTest
    {
        [Fact]
        public void Should_MoveToState_When_TransitionIsAllowed()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            FlowState toState = project.FlowStates.Single(s => s.Name == "In Progress");

            // Act
            Result result = workItem.Move(toState, admin, null, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.FlowStateId.Should().Be(toState.Id);
            workItem.UpdatedOnUtc.Should().Be(WorkItemData.UpdatedOnUtc);
        }

        [Fact]
        public void Should_RecordStateTransitionHistory_When_Moved()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            FlowState toState = project.FlowStates.Single(s => s.Name == "In Progress");

            // Act
            workItem.Move(toState, admin, "Moving to active work", WorkItemData.UpdatedOnUtc);

            // Assert
            workItem.StateHistory.Should().ContainSingle();
        }

        [Fact]
        public void Should_CreateMoveChangeLog_When_Moved()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            FlowState toState = project.FlowStates.Single(s => s.Name == "In Progress");

            // Act
            workItem.Move(toState, admin, null, WorkItemData.UpdatedOnUtc);

            // Assert
            workItem.ChangeLogs.Should().Contain(c => c.ChangeType == WorkItemChangeType.Moved);
        }

        [Fact]
        public void Should_RaiseWorkItemMovedDomainEvent_When_Moved()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            FlowState toState = project.FlowStates.Single(s => s.Name == "In Progress");
            Guid fromStateId = workItem.FlowStateId;

            // Act
            workItem.Move(toState, admin, null, WorkItemData.UpdatedOnUtc);

            // Assert
            WorkItemMovedDomainEvent domainEvent = AssertDomainEventWasPublished<WorkItemMovedDomainEvent>(workItem);
            domainEvent.WorkItemId.Should().Be(workItem.Id);
            domainEvent.FromStateId.Should().Be(fromStateId);
            domainEvent.ToStateId.Should().Be(toState.Id);
        }

        [Fact]
        public void Should_SetCompletedOnUtc_When_MovedToCompletedState()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            // First move to In Progress so we can reach Done
            FlowState inProgress = project.FlowStates.Single(s => s.Name == "In Progress");
            workItem.Move(inProgress, admin, null, WorkItemData.UpdatedOnUtc);
            FlowState doneState = project.FlowStates.Single(s => s.Name == "Done");

            // Act
            workItem.Move(doneState, admin, null, WorkItemData.UpdatedOnUtc);

            // Assert
            workItem.CompletedOnUtc.Should().Be(WorkItemData.UpdatedOnUtc);
        }

        [Fact]
        public void Should_SetCompletedOnUtc_When_MovedToCancelledState()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            FlowState cancelledState = project.FlowStates.Single(s => s.Name == "Cancelled");

            // Act
            workItem.Move(cancelledState, admin, null, WorkItemData.UpdatedOnUtc);

            // Assert
            workItem.CompletedOnUtc.Should().Be(WorkItemData.UpdatedOnUtc);
        }

        [Fact]
        public void Should_Fail_When_ChangedByIsNotProjectMember()
        {
            // Arrange
            var (workItem, project, _) = WorkItemData.GetWorkItemWithContext();
            User nonMember = UserData.GetActiveUser();
            FlowState toState = project.FlowStates.Single(s => s.Name == "In Progress");

            // Act
            Result result = workItem.Move(toState, nonMember, null, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.NotFound);
        }

        [Fact]
        public void Should_Fail_When_ChangedByIsInactive()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            User user = UserData.GetActiveUser();
            project.AddMember(user, ProjectRole.Developer, admin, WorkItemData.CreatedOnUtc);
            user.Deactivate(WorkItemData.CreatedOnUtc);
            FlowState toState = project.FlowStates.Single(s => s.Name == "In Progress");

            // Act
            Result result = workItem.Move(toState, user, null, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(UserErrors.Inactive);
        }

        [Fact]
        public void Should_Fail_When_TargetStateIsFromDifferentProject()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();
            var (otherProject, _) = WorkItemData.GetActiveProjectWithFlow();
            FlowState otherState = otherProject.FlowStates.Single(s => s.Name == "In Progress");

            // Act
            Result result = workItem.Move(otherState, admin, null, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.TargetStateNotInProject);
        }

        [Fact]
        public void Should_Fail_When_NoTransitionDefined()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            FlowState doneState = project.FlowStates.Single(s => s.Name == "Done");

            // Act
            Result result = workItem.Move(doneState, admin, null, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.TransitionNotAllowed);
        }

        [Fact]
        public void Should_Fail_When_UserRoleNotAllowedForTransition()
        {
            // Arrange
            var (project, admin) = WorkItemData.GetActiveProjectWithFlow();
            User member = UserData.GetActiveUser();
            project.AddMember(member, ProjectRole.QA, admin, WorkItemData.CreatedOnUtc);

            WorkItem workItem = WorkItem.Create(
                WorkItemData.Title, null, WorkItemData.Type, WorkItemData.Priority,
                project, admin, null, null, WorkItemData.CreatedOnUtc).Value;

            // Restrict the first transition to Admin only, leaving the QA member without it.
            FlowState todoState = project.FlowStates.Single(s => s.Name == "Todo");
            FlowState endState = project.FlowStates.Single(s => s.Name == "In Progress");
            FlowTransition transition = project.FlowTransitions
                .Single(t => t.FromStateId == todoState.Id && t.ToStateId == endState.Id);

            foreach (ProjectRole role in transition.AllowedRoles.Where(r => r != ProjectRole.Admin).ToList())
            {
                project.RemoveFlowTransitionRole(transition.Id, role, admin);
            }

            // Act
            Result result = workItem.Move(endState, member, null, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.TransitionRoleNotAllowed);
        }

        [Fact]
        public void Should_Fail_When_ProjectDoesNotAllowWorkItems()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            FlowState toState = project.FlowStates.Single(s => s.Name == "In Progress");
            project.ChangeStatus(ProjectStatus.Archived, admin, WorkItemData.UpdatedOnUtc);

            // Act
            Result result = workItem.Move(toState, admin, null, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }
    }

    public sealed class Assign : BaseTest
    {
        [Fact]
        public void Should_AssignUser_When_UserIsProjectMemberAndActive()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            User assignee = UserData.GetActiveUser();
            project.AddMember(assignee, ProjectRole.Developer, admin, WorkItemData.CreatedOnUtc);

            // Act
            Result result = workItem.Assign(assignee, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.AssigneeId.Should().Be(assignee.Id);
            workItem.UpdatedOnUtc.Should().Be(WorkItemData.UpdatedOnUtc);
        }

        [Fact]
        public void Should_CreateAssignedChangeLog_When_Assigned()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            User assignee = UserData.GetActiveUser();
            project.AddMember(assignee, ProjectRole.Developer, admin, WorkItemData.CreatedOnUtc);

            // Act
            workItem.Assign(assignee, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            workItem.ChangeLogs.Should().Contain(c => c.ChangeType == WorkItemChangeType.Assigned);
        }

        [Fact]
        public void Should_RaiseWorkItemAssignedDomainEvent_When_Assigned()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            User assignee = UserData.GetActiveUser();
            project.AddMember(assignee, ProjectRole.Developer, admin, WorkItemData.CreatedOnUtc);

            // Act
            workItem.Assign(assignee, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            WorkItemAssignedDomainEvent domainEvent = AssertDomainEventWasPublished<WorkItemAssignedDomainEvent>(workItem);
            domainEvent.WorkItemId.Should().Be(workItem.Id);
            domainEvent.AssigneeId.Should().Be(assignee.Id);
        }

        [Fact]
        public void Should_Fail_When_ChangedByIsNotProjectMember()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            User nonMember = UserData.GetActiveUser();
            User assignee = UserData.GetActiveUser();
            project.AddMember(assignee, ProjectRole.Developer, admin, WorkItemData.CreatedOnUtc);

            // Act
            Result result = workItem.Assign(assignee, nonMember, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.NotFound);
        }

        [Fact]
        public void Should_Fail_When_AssigneeIsNotProjectMember()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();
            User nonMemberAssignee = UserData.GetActiveUser();

            // Act
            Result result = workItem.Assign(nonMemberAssignee, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.AssigneeNotProjectMember);
        }

        [Fact]
        public void Should_Fail_When_ChangedByIsInactive()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            User changedBy = UserData.GetActiveUser();
            project.AddMember(changedBy, ProjectRole.Developer, admin, WorkItemData.CreatedOnUtc);
            changedBy.Deactivate(WorkItemData.CreatedOnUtc);
            User assignee = UserData.GetActiveUser();
            project.AddMember(assignee, ProjectRole.Developer, admin, WorkItemData.CreatedOnUtc);

            // Act
            Result result = workItem.Assign(assignee, changedBy, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(UserErrors.Inactive);
        }

        [Fact]
        public void Should_Fail_When_AssigneeIsInactive()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            User assignee = UserData.GetActiveUser();
            project.AddMember(assignee, ProjectRole.Developer, admin, WorkItemData.CreatedOnUtc);
            assignee.Deactivate(WorkItemData.CreatedOnUtc);

            // Act
            Result result = workItem.Assign(assignee, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(UserErrors.Inactive);
        }

        [Fact]
        public void Should_Fail_When_WorkItemStateIsCancelled()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            User assignee = UserData.GetActiveUser();
            project.AddMember(assignee, ProjectRole.Developer, admin, WorkItemData.CreatedOnUtc);
            FlowState cancelledState = project.FlowStates.Single(s => s.Name == "Cancelled");
            workItem.Move(cancelledState, admin, null, WorkItemData.UpdatedOnUtc);

            // Act
            Result result = workItem.Assign(assignee, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.CancelledWorkItemCannotBeModified);
        }

        [Fact]
        public void Should_Fail_When_ProjectDoesNotAllowWorkItems()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            User assignee = UserData.GetActiveUser();
            project.AddMember(assignee, ProjectRole.Developer, admin, WorkItemData.CreatedOnUtc);
            project.ChangeStatus(ProjectStatus.Archived, admin, WorkItemData.UpdatedOnUtc);

            // Act
            Result result = workItem.Assign(assignee, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }
    }

    public sealed class Unassign : BaseTest
    {
        [Fact]
        public void Should_Unassign_When_WorkItemIsAssigned()
        {
            // Arrange
            var (project, admin) = WorkItemData.GetActiveProjectWithFlow();
            User assignee = UserData.GetActiveUser();
            project.AddMember(assignee, ProjectRole.Developer, admin, WorkItemData.CreatedOnUtc);
            WorkItem workItem = WorkItem.Create(
                WorkItemData.Title, null, WorkItemData.Type, WorkItemData.Priority,
                project, admin, null, null, WorkItemData.CreatedOnUtc, assignee).Value;

            // Act
            Result result = workItem.Unassign(admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.AssigneeId.Should().BeNull();
            workItem.UpdatedOnUtc.Should().Be(WorkItemData.UpdatedOnUtc);
        }

        [Fact]
        public void Should_CreateUnassignedChangeLog_When_Unassigned()
        {
            // Arrange
            var (project, admin) = WorkItemData.GetActiveProjectWithFlow();
            User assignee = UserData.GetActiveUser();
            project.AddMember(assignee, ProjectRole.Developer, admin, WorkItemData.CreatedOnUtc);
            WorkItem workItem = WorkItem.Create(
                WorkItemData.Title, null, WorkItemData.Type, WorkItemData.Priority,
                project, admin, null, null, WorkItemData.CreatedOnUtc, assignee).Value;

            // Act
            workItem.Unassign(admin, WorkItemData.UpdatedOnUtc);

            // Assert
            workItem.ChangeLogs.Should().Contain(c => c.ChangeType == WorkItemChangeType.Unassigned);
        }

        [Fact]
        public void Should_RaiseWorkItemUnassignedDomainEvent_When_Unassigned()
        {
            // Arrange
            var (project, admin) = WorkItemData.GetActiveProjectWithFlow();
            User assignee = UserData.GetActiveUser();
            project.AddMember(assignee, ProjectRole.Developer, admin, WorkItemData.CreatedOnUtc);
            WorkItem workItem = WorkItem.Create(
                WorkItemData.Title, null, WorkItemData.Type, WorkItemData.Priority,
                project, admin, null, null, WorkItemData.CreatedOnUtc, assignee).Value;

            // Act
            workItem.Unassign(admin, WorkItemData.UpdatedOnUtc);

            // Assert
            WorkItemUnassignedDomainEvent domainEvent = AssertDomainEventWasPublished<WorkItemUnassignedDomainEvent>(workItem);
            domainEvent.WorkItemId.Should().Be(workItem.Id);
        }

        [Fact]
        public void Should_Fail_When_WorkItemIsNotAssigned()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.Unassign(admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.NotAssigned);
        }

        [Fact]
        public void Should_Fail_When_ChangedByIsNotProjectMember()
        {
            // Arrange
            var (project, admin) = WorkItemData.GetActiveProjectWithFlow();
            User assignee = UserData.GetActiveUser();
            project.AddMember(assignee, ProjectRole.Developer, admin, WorkItemData.CreatedOnUtc);
            WorkItem workItem = WorkItem.Create(
                WorkItemData.Title, null, WorkItemData.Type, WorkItemData.Priority,
                project, admin, null, null, WorkItemData.CreatedOnUtc, assignee).Value;
            User nonMember = UserData.GetActiveUser();

            // Act
            Result result = workItem.Unassign(nonMember, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.NotFound);
        }

        [Fact]
        public void Should_Fail_When_ChangedByIsInactive()
        {
            // Arrange
            var (project, admin) = WorkItemData.GetActiveProjectWithFlow();
            User assignee = UserData.GetActiveUser();
            project.AddMember(assignee, ProjectRole.Developer, admin, WorkItemData.CreatedOnUtc);
            WorkItem workItem = WorkItem.Create(
                WorkItemData.Title, null, WorkItemData.Type, WorkItemData.Priority,
                project, admin, null, null, WorkItemData.CreatedOnUtc, assignee).Value;
            User changedBy = UserData.GetActiveUser();
            project.AddMember(changedBy, ProjectRole.Developer, admin, WorkItemData.CreatedOnUtc);
            changedBy.Deactivate(WorkItemData.CreatedOnUtc);

            // Act
            Result result = workItem.Unassign(changedBy, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(UserErrors.Inactive);
        }

        [Fact]
        public void Should_Fail_When_WorkItemStateIsCancelled()
        {
            // Arrange
            var (project, admin) = WorkItemData.GetActiveProjectWithFlow();
            User assignee = UserData.GetActiveUser();
            project.AddMember(assignee, ProjectRole.Developer, admin, WorkItemData.CreatedOnUtc);
            WorkItem workItem = WorkItem.Create(
                WorkItemData.Title, null, WorkItemData.Type, WorkItemData.Priority,
                project, admin, null, null, WorkItemData.CreatedOnUtc, assignee).Value;
            FlowState cancelledState = project.FlowStates.Single(s => s.Name == "Cancelled");
            workItem.Move(cancelledState, admin, null, WorkItemData.UpdatedOnUtc);

            // Act
            Result result = workItem.Unassign(admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.CancelledWorkItemCannotBeModified);
        }

        [Fact]
        public void Should_Fail_When_ProjectDoesNotAllowWorkItems()
        {
            // Arrange
            var (project, admin) = WorkItemData.GetActiveProjectWithFlow();
            User assignee = UserData.GetActiveUser();
            project.AddMember(assignee, ProjectRole.Developer, admin, WorkItemData.CreatedOnUtc);
            WorkItem workItem = WorkItem.Create(
                WorkItemData.Title, null, WorkItemData.Type, WorkItemData.Priority,
                project, admin, null, null, WorkItemData.CreatedOnUtc, assignee).Value;
            project.ChangeStatus(ProjectStatus.Archived, admin, WorkItemData.UpdatedOnUtc);

            // Act
            Result result = workItem.Unassign(admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }
    }

    public sealed class AddComment : BaseTest
    {
        [Fact]
        public void Should_AddComment_When_DataIsValid()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();
            const string content = "This is a comment";

            // Act
            Result result = workItem.AddComment(admin, content, WorkItemData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.Comments.Should().ContainSingle(c => c.Content == content);
        }

        [Fact]
        public void Should_CreateCommentAddedChangeLog_When_CommentAdded()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            workItem.AddComment(admin, "A comment", WorkItemData.CreatedOnUtc);

            // Assert
            workItem.ChangeLogs.Should().Contain(c => c.ChangeType == WorkItemChangeType.CommentAdded);
        }

        [Fact]
        public void Should_RaiseWorkItemCommentAddedDomainEvent_When_CommentAdded()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            workItem.AddComment(admin, "A comment", WorkItemData.CreatedOnUtc);

            // Assert
            WorkItemCommentAddedDomainEvent domainEvent = AssertDomainEventWasPublished<WorkItemCommentAddedDomainEvent>(workItem);
            domainEvent.WorkItemId.Should().Be(workItem.Id);
        }

        [Fact]
        public void Should_Fail_When_AuthorIsNotProjectMember()
        {
            // Arrange
            var (workItem, _, _) = WorkItemData.GetWorkItemWithContext();
            User nonMember = UserData.GetActiveUser();

            // Act
            Result result = workItem.AddComment(nonMember, "A comment", WorkItemData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.NotFound);
        }

        [Fact]
        public void Should_Fail_When_AuthorIsInactive()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            User author = UserData.GetActiveUser();
            project.AddMember(author, ProjectRole.Developer, admin, WorkItemData.CreatedOnUtc);
            author.Deactivate(WorkItemData.CreatedOnUtc);

            // Act
            Result result = workItem.AddComment(author, "A comment", WorkItemData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(UserErrors.Inactive);
        }

        [Fact]
        public void Should_Fail_When_ContentIsEmpty()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.AddComment(admin, string.Empty, WorkItemData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.CommentContentRequired);
        }

        [Fact]
        public void Should_Fail_When_ContentIsWhitespace()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.AddComment(admin, "   ", WorkItemData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.CommentContentRequired);
        }

        [Fact]
        public void Should_Fail_When_ProjectDoesNotAllowWorkItems()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            project.ChangeStatus(ProjectStatus.Archived, admin, WorkItemData.UpdatedOnUtc);

            // Act
            Result result = workItem.AddComment(admin, "This is a comment", WorkItemData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }
    }

    public sealed class UpdateComment : BaseTest
    {
        [Fact]
        public void Should_UpdateComment_When_AuthorUpdatesOwnComment()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();
            workItem.AddComment(admin, "Original content", WorkItemData.CreatedOnUtc);
            Guid commentId = workItem.Comments.Single().Id;

            // Act
            Result result = workItem.UpdateComment(commentId, admin, "Updated content", WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.Comments.Single().Content.Should().Be("Updated content");
        }

        [Fact]
        public void Should_CreateCommentUpdatedChangeLog_When_CommentUpdated()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();
            workItem.AddComment(admin, "Original content", WorkItemData.CreatedOnUtc);
            Guid commentId = workItem.Comments.Single().Id;

            // Act
            workItem.UpdateComment(commentId, admin, "Updated content", WorkItemData.UpdatedOnUtc);

            // Assert
            workItem.ChangeLogs.Should().Contain(c => c.ChangeType == WorkItemChangeType.CommentUpdated);
        }

        [Fact]
        public void Should_Fail_When_ChangedByIsNotProjectMember()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();
            workItem.AddComment(admin, "Original content", WorkItemData.CreatedOnUtc);
            Guid commentId = workItem.Comments.Single().Id;
            User nonMember = UserData.GetActiveUser();

            // Act
            Result result = workItem.UpdateComment(commentId, nonMember, "Updated content", WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.NotFound);
        }

        [Fact]
        public void Should_Fail_When_CommentNotFound()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.UpdateComment(Guid.NewGuid(), admin, "Updated content", WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.CommentNotFound);
        }

        [Fact]
        public void Should_Fail_When_UserIsNotCommentAuthor()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            workItem.AddComment(admin, "Original content", WorkItemData.CreatedOnUtc);
            Guid commentId = workItem.Comments.Single().Id;
            User otherMember = UserData.GetActiveUser();
            project.AddMember(otherMember, ProjectRole.Developer, admin, WorkItemData.CreatedOnUtc);

            // Act
            Result result = workItem.UpdateComment(commentId, otherMember, "Updated content", WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.CommentNotOwnedByUser);
        }

        [Fact]
        public void Should_Fail_When_UpdatedContentIsEmpty()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();
            workItem.AddComment(admin, "Original content", WorkItemData.CreatedOnUtc);
            Guid commentId = workItem.Comments.Single().Id;

            // Act
            Result result = workItem.UpdateComment(commentId, admin, string.Empty, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.CommentContentRequired);
        }

        [Fact]
        public void Should_Fail_When_ProjectDoesNotAllowWorkItems()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            workItem.AddComment(admin, "Original content", WorkItemData.CreatedOnUtc);
            Guid commentId = workItem.Comments.Single().Id;
            project.ChangeStatus(ProjectStatus.Archived, admin, WorkItemData.UpdatedOnUtc);

            // Act
            Result result = workItem.UpdateComment(commentId, admin, "Updated content", WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }
    }

    public sealed class RemoveComment : BaseTest
    {
        [Fact]
        public void Should_RemoveComment_When_AuthorRemovesOwnComment()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();
            workItem.AddComment(admin, "A comment", WorkItemData.CreatedOnUtc);
            Guid commentId = workItem.Comments.Single().Id;

            // Act
            Result result = workItem.RemoveComment(commentId, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.Comments.Single(c => c.Id == commentId).IsDeleted.Should().BeTrue();
        }

        [Fact]
        public void Should_CreateCommentRemovedChangeLog_When_CommentRemoved()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();
            workItem.AddComment(admin, "A comment", WorkItemData.CreatedOnUtc);
            Guid commentId = workItem.Comments.Single().Id;

            // Act
            workItem.RemoveComment(commentId, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            workItem.ChangeLogs.Should().Contain(c => c.ChangeType == WorkItemChangeType.CommentRemoved);
        }

        [Fact]
        public void Should_Fail_When_ChangedByIsNotProjectMember()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();
            workItem.AddComment(admin, "A comment", WorkItemData.CreatedOnUtc);
            Guid commentId = workItem.Comments.Single().Id;
            User nonMember = UserData.GetActiveUser();

            // Act
            Result result = workItem.RemoveComment(commentId, nonMember, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.NotFound);
        }

        [Fact]
        public void Should_Fail_When_CommentNotFound()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.RemoveComment(Guid.NewGuid(), admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.CommentNotFound);
        }

        [Fact]
        public void Should_Fail_When_UserIsNotCommentAuthor()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            workItem.AddComment(admin, "A comment", WorkItemData.CreatedOnUtc);
            Guid commentId = workItem.Comments.Single().Id;
            User otherMember = UserData.GetActiveUser();
            project.AddMember(otherMember, ProjectRole.Developer, admin, WorkItemData.CreatedOnUtc);

            // Act
            Result result = workItem.RemoveComment(commentId, otherMember, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.CommentNotOwnedByUser);
        }

        [Fact]
        public void Should_Fail_When_ProjectDoesNotAllowWorkItems()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            workItem.AddComment(admin, "A comment", WorkItemData.CreatedOnUtc);
            Guid commentId = workItem.Comments.Single().Id;
            project.ChangeStatus(ProjectStatus.Archived, admin, WorkItemData.UpdatedOnUtc);

            // Act
            Result result = workItem.RemoveComment(commentId, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }
    }

    public sealed class LogTime : BaseTest
    {
        [Fact]
        public void Should_LogTime_When_DataIsValid()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();
            const decimal hours = 2.5m;

            // Act
            Result result = workItem.LogTime(admin, hours, "Worked on implementation", WorkItemData.UpdatedOnUtc, WorkItemData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.TimeEntries.Should().ContainSingle(e => e.Hours == hours);
        }

        [Fact]
        public void Should_CreateTimeLoggedChangeLog_When_TimeLogged()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            workItem.LogTime(admin, 1m, null, WorkItemData.UpdatedOnUtc, WorkItemData.CreatedOnUtc);

            // Assert
            workItem.ChangeLogs.Should().Contain(c => c.ChangeType == WorkItemChangeType.TimeLogged);
        }

        [Fact]
        public void Should_RaiseWorkItemTimeLoggedDomainEvent_When_TimeLogged()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();
            const decimal hours = 3m;

            // Act
            workItem.LogTime(admin, hours, null, WorkItemData.UpdatedOnUtc, WorkItemData.CreatedOnUtc);

            // Assert
            WorkItemTimeLoggedDomainEvent domainEvent = AssertDomainEventWasPublished<WorkItemTimeLoggedDomainEvent>(workItem);
            domainEvent.WorkItemId.Should().Be(workItem.Id);
            domainEvent.Hours.Should().Be(hours);
        }

        [Fact]
        public void Should_Fail_When_UserIsNotProjectMember()
        {
            // Arrange
            var (workItem, _, _) = WorkItemData.GetWorkItemWithContext();
            User nonMember = UserData.GetActiveUser();

            // Act
            Result result = workItem.LogTime(nonMember, 1m, null, WorkItemData.UpdatedOnUtc, WorkItemData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.NotFound);
        }

        [Fact]
        public void Should_Fail_When_UserIsInactive()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            User user = UserData.GetActiveUser();
            project.AddMember(user, ProjectRole.Developer, admin, WorkItemData.CreatedOnUtc);
            user.Deactivate(WorkItemData.CreatedOnUtc);

            // Act
            Result result = workItem.LogTime(user, 1m, null, WorkItemData.UpdatedOnUtc, WorkItemData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(UserErrors.Inactive);
        }

        [Fact]
        public void Should_Fail_When_HoursIsZero()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.LogTime(admin, 0m, null, WorkItemData.UpdatedOnUtc, WorkItemData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.TimeEntryHoursInvalid);
        }

        [Fact]
        public void Should_Fail_When_HoursIsNegative()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.LogTime(admin, -1m, null, WorkItemData.UpdatedOnUtc, WorkItemData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.TimeEntryHoursInvalid);
        }

        [Fact]
        public void Should_Fail_When_ProjectDoesNotAllowWorkItems()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            project.ChangeStatus(ProjectStatus.Archived, admin, WorkItemData.UpdatedOnUtc);

            // Act
            Result result = workItem.LogTime(admin, 1m, null, WorkItemData.UpdatedOnUtc, WorkItemData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }
    }

    public sealed class AddTag : BaseTest
    {
        [Fact]
        public void Should_AddTag_When_DataIsValid()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.AddTag("backend", admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.Tags.Should().ContainSingle(t => t.Name == "backend");
            workItem.DomainEvents.Should().ContainSingle(e => e is WorkItemTagAddedDomainEvent);
        }

        [Fact]
        public void Should_NormalizeName_When_TagAdded()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            workItem.AddTag("  Backend  ", admin, WorkItemData.UpdatedOnUtc);

            // Assert
            workItem.Tags.Should().ContainSingle(t => t.Name == "backend");
        }

        [Fact]
        public void Should_Fail_When_TagNameIsEmpty()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.AddTag(string.Empty, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.TagNameRequired);
        }

        [Fact]
        public void Should_Fail_When_TagNameTooLong()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();
            string longName = new('x', WorkItemTag.MaxNameLength + 1);

            // Act
            Result result = workItem.AddTag(longName, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.TagNameTooLong);
        }

        [Fact]
        public void Should_Fail_When_DuplicateTagName()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();
            workItem.AddTag("backend", admin, WorkItemData.UpdatedOnUtc);

            // Act
            Result result = workItem.AddTag("BACKEND", admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.DuplicateTagName);
        }

        [Fact]
        public void Should_Fail_When_UserNotProjectMember()
        {
            // Arrange
            var (workItem, _, _) = WorkItemData.GetWorkItemWithContext();
            User nonMember = UserData.GetActiveUser();

            // Act
            Result result = workItem.AddTag("backend", nonMember, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.NotFound);
        }

        [Fact]
        public void Should_Fail_When_UserIsInactive()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();
            admin.Deactivate(WorkItemData.UpdatedOnUtc);

            // Act
            Result result = workItem.AddTag("backend", admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(UserErrors.Inactive);
        }

        [Fact]
        public void Should_Fail_When_ProjectDoesNotAllowWorkItems()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            project.ChangeStatus(ProjectStatus.Archived, admin, WorkItemData.UpdatedOnUtc);

            // Act
            Result result = workItem.AddTag("backend", admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }
    }

    public sealed class RemoveTag : BaseTest
    {
        [Fact]
        public void Should_RemoveTag_When_TagExists()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();
            workItem.AddTag("backend", admin, WorkItemData.UpdatedOnUtc);
            Guid tagId = workItem.Tags.First().Id;
            workItem.ClearDomainEvents();

            // Act
            Result result = workItem.RemoveTag(tagId, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            workItem.Tags.Should().BeEmpty();
            workItem.DomainEvents.Should().ContainSingle(e => e is WorkItemTagRemovedDomainEvent);
        }

        [Fact]
        public void Should_Fail_When_TagNotFound()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();

            // Act
            Result result = workItem.RemoveTag(Guid.NewGuid(), admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.TagNotFound);
        }

        [Fact]
        public void Should_Fail_When_UserNotProjectMember()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();
            workItem.AddTag("backend", admin, WorkItemData.UpdatedOnUtc);
            Guid tagId = workItem.Tags.First().Id;
            User nonMember = UserData.GetActiveUser();

            // Act
            Result result = workItem.RemoveTag(tagId, nonMember, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(WorkItemErrors.NotFound);
        }

        [Fact]
        public void Should_Fail_When_UserIsInactive()
        {
            // Arrange
            var (workItem, _, admin) = WorkItemData.GetWorkItemWithContext();
            workItem.AddTag("backend", admin, WorkItemData.UpdatedOnUtc);
            Guid tagId = workItem.Tags.First().Id;
            admin.Deactivate(WorkItemData.UpdatedOnUtc);

            // Act
            Result result = workItem.RemoveTag(tagId, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(UserErrors.Inactive);
        }

        [Fact]
        public void Should_Fail_When_ProjectDoesNotAllowWorkItems()
        {
            // Arrange
            var (workItem, project, admin) = WorkItemData.GetWorkItemWithContext();
            workItem.AddTag("backend", admin, WorkItemData.UpdatedOnUtc);
            Guid tagId = workItem.Tags.First().Id;
            project.ChangeStatus(ProjectStatus.Archived, admin, WorkItemData.UpdatedOnUtc);

            // Act
            Result result = workItem.RemoveTag(tagId, admin, WorkItemData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }
    }
}
