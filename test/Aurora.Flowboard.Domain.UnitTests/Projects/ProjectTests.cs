namespace Aurora.Flowboard.Domain.UnitTests.Projects;

public sealed class ProjectTests
{
    public sealed class Create : BaseTest
    {
        [Fact]
        public void Should_CreateProject_When_DataIsValid()
        {
            // Arrange
            User creator = UserData.GetActiveUser();

            // Act
            Result<Project> result = Project.Create(
                ProjectData.Name,
                ProjectData.Description,
                ProjectData.Code,
                ProjectData.EstimatedCompletionDate,
                creator,
                ProjectData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            result.Value.Name.Should().Be(ProjectData.Name);
            result.Value.Code.Should().Be(ProjectData.Code);
            result.Value.CreatedBy.Should().Be(creator.Id);
        }

        [Fact]
        public void Should_SetStatusToDraft_When_Created()
        {
            // Arrange
            User creator = UserData.GetActiveUser();

            // Act
            Result<Project> result = Project.Create(
                ProjectData.Name,
                ProjectData.Description,
                ProjectData.Code,
                ProjectData.EstimatedCompletionDate,
                creator,
                ProjectData.CreatedOnUtc);

            // Assert
            result.Value.Status.Should().Be(ProjectStatus.Draft);
        }

        [Fact]
        public void Should_SetWorkItemCounterToZero_When_Created()
        {
            // Arrange
            User creator = UserData.GetActiveUser();

            // Act
            Result<Project> result = Project.Create(
                ProjectData.Name,
                ProjectData.Description,
                ProjectData.Code,
                ProjectData.EstimatedCompletionDate,
                creator,
                ProjectData.CreatedOnUtc);

            // Assert
            result.Value.WorkItemCounter.Should().Be(0);
        }

        [Fact]
        public void Should_AddCreatorAsAdminMember_When_Created()
        {
            // Arrange
            User creator = UserData.GetActiveUser();

            // Act
            Result<Project> result = Project.Create(
                ProjectData.Name,
                ProjectData.Description,
                ProjectData.Code,
                ProjectData.EstimatedCompletionDate,
                creator,
                ProjectData.CreatedOnUtc);

            // Assert
            result.Value.Members.Should().ContainSingle(m =>
                m.UserId == creator.Id && m.Role == ProjectRole.Admin);
        }

        [Fact]
        public void Should_AddCreatedChangeLog_When_Created()
        {
            // Arrange
            User creator = UserData.GetActiveUser();

            // Act
            Result<Project> result = Project.Create(
                ProjectData.Name,
                ProjectData.Description,
                ProjectData.Code,
                ProjectData.EstimatedCompletionDate,
                creator,
                ProjectData.CreatedOnUtc);

            // Assert
            result.Value.ChangeLogs.Should().ContainSingle(cl =>
                cl.ChangeType == ProjectChangeType.Created);
        }

        [Fact]
        public void Should_RaiseProjectCreatedDomainEvent_When_Created()
        {
            // Arrange
            User creator = UserData.GetActiveUser();

            // Act
            Result<Project> result = Project.Create(
                ProjectData.Name,
                ProjectData.Description,
                ProjectData.Code,
                ProjectData.EstimatedCompletionDate,
                creator,
                ProjectData.CreatedOnUtc);

            // Assert
            ProjectCreatedDomainEvent domainEvent = AssertDomainEventWasPublished<ProjectCreatedDomainEvent>(result.Value);
            domainEvent.ProjectId.Should().Be(result.Value.Id);
        }

        [Fact]
        public void Should_TrimNameAndDescription_When_Creating()
        {
            // Arrange
            User creator = UserData.GetActiveUser();

            // Act
            Result<Project> result = Project.Create(
                $"  {ProjectData.Name}  ",
                $"  {ProjectData.Description}  ",
                ProjectData.Code,
                ProjectData.EstimatedCompletionDate,
                creator,
                ProjectData.CreatedOnUtc);

            // Assert
            result.Value.Name.Should().Be(ProjectData.Name);
            result.Value.Description.Should().Be(ProjectData.Description);
        }

        [Fact]
        public void Should_Fail_When_NameIsEmpty()
        {
            // Arrange
            User creator = UserData.GetActiveUser();

            // Act
            Result<Project> result = Project.Create(
                string.Empty,
                ProjectData.Description,
                ProjectData.Code,
                ProjectData.EstimatedCompletionDate,
                creator,
                ProjectData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.NameRequired);
        }

        [Fact]
        public void Should_Fail_When_NameIsWhitespace()
        {
            // Arrange
            User creator = UserData.GetActiveUser();

            // Act
            Result<Project> result = Project.Create(
                "   ",
                ProjectData.Description,
                ProjectData.Code,
                ProjectData.EstimatedCompletionDate,
                creator,
                ProjectData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.NameRequired);
        }

        [Fact]
        public void Should_Fail_When_NameExceedsMaxLength()
        {
            // Arrange
            User creator = UserData.GetActiveUser();
            string longName = new('A', 101);

            // Act
            Result<Project> result = Project.Create(
                longName,
                ProjectData.Description,
                ProjectData.Code,
                ProjectData.EstimatedCompletionDate,
                creator,
                ProjectData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.NameTooLong);
        }

        [Fact]
        public void Should_Fail_When_DescriptionExceedsMaxLength()
        {
            // Arrange
            User creator = UserData.GetActiveUser();
            string longDescription = new('A', 501);

            // Act
            Result<Project> result = Project.Create(
                ProjectData.Name,
                longDescription,
                ProjectData.Code,
                ProjectData.EstimatedCompletionDate,
                creator,
                ProjectData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.DescriptionTooLong);
        }

        [Fact]
        public void Should_Fail_When_CodeIsEmpty()
        {
            // Arrange
            User creator = UserData.GetActiveUser();

            // Act
            Result<Project> result = Project.Create(
                ProjectData.Name,
                ProjectData.Description,
                string.Empty,
                ProjectData.EstimatedCompletionDate,
                creator,
                ProjectData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.CodeRequired);
        }

        [Fact]
        public void Should_Fail_When_CodeIsTooLong()
        {
            // Arrange
            User creator = UserData.GetActiveUser();

            // Act
            Result<Project> result = Project.Create(
                ProjectData.Name,
                ProjectData.Description,
                "ABCD",
                ProjectData.EstimatedCompletionDate,
                creator,
                ProjectData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.CodeTooLong);
        }

        [Fact]
        public void Should_Fail_When_CodeHasInvalidCharacters()
        {
            // Arrange
            User creator = UserData.GetActiveUser();

            // Act
            Result<Project> result = Project.Create(
                ProjectData.Name,
                ProjectData.Description,
                "A1B",
                ProjectData.EstimatedCompletionDate,
                creator,
                ProjectData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.CodeInvalidCharacters);
        }
    }

    public sealed class Update : BaseTest
    {
        [Fact]
        public void Should_UpdateProject_When_AdminUpdates()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetDraftProject(admin);

            // Act
            Result result = project.Update("New Name", "New Description", null, admin, ProjectData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            project.Name.Should().Be("New Name");
            project.Description.Should().Be("New Description");
        }

        [Fact]
        public void Should_RaiseProjectUpdatedDomainEvent_When_Updated()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetDraftProject(admin);

            // Act
            project.Update("New Name", null, null, admin, ProjectData.UpdatedOnUtc);

            // Assert
            ProjectUpdatedDomainEvent domainEvent = AssertDomainEventWasPublished<ProjectUpdatedDomainEvent>(project);
            domainEvent.ProjectId.Should().Be(project.Id);
        }

        [Fact]
        public void Should_AddUpdatedChangeLog_When_Updated()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetDraftProject(admin);
            int initialCount = project.ChangeLogs.Count;

            // Act
            project.Update("New Name", null, null, admin, ProjectData.UpdatedOnUtc);

            // Assert
            project.ChangeLogs.Count.Should().Be(initialCount + 1);
            project.ChangeLogs.Should().Contain(cl => cl.ChangeType == ProjectChangeType.Updated);
        }

        [Fact]
        public void Should_Fail_When_NonAdminUpdates()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetDraftProject(admin);
            User nonAdmin = UserData.GetActiveUser();

            // Act
            Result result = project.Update("New Name", null, null, nonAdmin, ProjectData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OnlyAdminCanUpdateProject);
        }

        [Fact]
        public void Should_Fail_When_UpdateOnCompletedProject()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProjectWithStatus(ProjectStatus.Completed, admin);

            // Act
            Result result = project.Update("New Name", null, null, admin, ProjectData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        [Fact]
        public void Should_Fail_When_UpdateOnArchivedProject()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProjectWithStatus(ProjectStatus.Archived, admin);

            // Act
            Result result = project.Update("New Name", null, null, admin, ProjectData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        [Fact]
        public void Should_Fail_When_NameIsEmptyOnUpdate()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetDraftProject(admin);

            // Act
            Result result = project.Update(string.Empty, null, null, admin, ProjectData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.NameRequired);
        }

        [Fact]
        public void Should_Fail_When_NameExceedsMaxLengthOnUpdate()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetDraftProject(admin);
            string longName = new('A', 101);

            // Act
            Result result = project.Update(longName, null, null, admin, ProjectData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.NameTooLong);
        }

        [Fact]
        public void Should_Fail_When_DescriptionExceedsMaxLengthOnUpdate()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetDraftProject(admin);
            string longDescription = new('A', 501);

            // Act
            Result result = project.Update(ProjectData.Name, longDescription, null, admin, ProjectData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.DescriptionTooLong);
        }
    }

    public sealed class ChangeStatus : BaseTest
    {
        public static TheoryData<ProjectStatus, ProjectStatus> ValidTransitions => new()
        {
            { ProjectStatus.Draft, ProjectStatus.Active },
            { ProjectStatus.Draft, ProjectStatus.Archived },
            { ProjectStatus.Active, ProjectStatus.OnHold },
            { ProjectStatus.Active, ProjectStatus.Completed },
            { ProjectStatus.Active, ProjectStatus.Archived },
            { ProjectStatus.OnHold, ProjectStatus.Active },
            { ProjectStatus.OnHold, ProjectStatus.Archived },
            { ProjectStatus.Completed, ProjectStatus.Archived },
        };

        public static TheoryData<ProjectStatus, ProjectStatus> InvalidTransitions => new()
        {
            { ProjectStatus.Draft, ProjectStatus.OnHold },
            { ProjectStatus.Draft, ProjectStatus.Completed },
            { ProjectStatus.Active, ProjectStatus.Draft },
            { ProjectStatus.OnHold, ProjectStatus.Draft },
            { ProjectStatus.OnHold, ProjectStatus.Completed },
            { ProjectStatus.Completed, ProjectStatus.Draft },
            { ProjectStatus.Completed, ProjectStatus.Active },
            { ProjectStatus.Completed, ProjectStatus.OnHold },
        };

        [Theory]
        [MemberData(nameof(ValidTransitions))]
        public void Should_ChangeStatus_When_TransitionIsValid(ProjectStatus from, ProjectStatus to)
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProjectWithStatus(from, admin);

            // Act
            Result result = project.ChangeStatus(to, admin, ProjectData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            project.Status.Should().Be(to);
        }

        [Fact]
        public void Should_RaiseProjectStatusChangedDomainEvent_When_StatusChanges()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetDraftProject(admin);

            // Act
            project.ChangeStatus(ProjectStatus.Active, admin, ProjectData.UpdatedOnUtc);

            // Assert
            ProjectStatusChangedDomainEvent domainEvent = AssertDomainEventWasPublished<ProjectStatusChangedDomainEvent>(project);
            domainEvent.ProjectId.Should().Be(project.Id);
            domainEvent.OldStatus.Should().Be(ProjectStatus.Draft);
            domainEvent.NewStatus.Should().Be(ProjectStatus.Active);
        }

        [Fact]
        public void Should_AddStatusChangedChangeLog_When_StatusChanges()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetDraftProject(admin);
            int initialCount = project.ChangeLogs.Count;

            // Act
            project.ChangeStatus(ProjectStatus.Active, admin, ProjectData.UpdatedOnUtc);

            // Assert
            project.ChangeLogs.Count.Should().Be(initialCount + 1);
            project.ChangeLogs.Should().Contain(cl => cl.ChangeType == ProjectChangeType.StatusChanged);
        }

        [Fact]
        public void Should_Fail_When_NonAdminChangesStatus()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetDraftProject(admin);
            User nonAdmin = UserData.GetActiveUser();

            // Act
            Result result = project.ChangeStatus(ProjectStatus.Active, nonAdmin, ProjectData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OnlyAdminCanChangeStatus);
        }

        [Fact]
        public void Should_Fail_When_StatusIsTheSame()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetDraftProject(admin);

            // Act
            Result result = project.ChangeStatus(ProjectStatus.Draft, admin, ProjectData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.InvalidStatusTransition);
        }

        [Theory]
        [MemberData(nameof(InvalidTransitions))]
        public void Should_Fail_When_TransitionIsInvalid(ProjectStatus from, ProjectStatus to)
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProjectWithStatus(from, admin);

            // Act
            Result result = project.ChangeStatus(to, admin, ProjectData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.InvalidStatusTransition);
        }

        [Fact]
        public void Should_Fail_When_ArchivedProjectChangesStatus()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProjectWithStatus(ProjectStatus.Archived, admin);

            // Act
            Result result = project.ChangeStatus(ProjectStatus.Active, admin, ProjectData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.InvalidStatusTransition);
        }
    }

    public sealed class AddMember : BaseTest
    {
        [Fact]
        public void Should_AddMember_When_DataIsValid()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetDraftProject(admin);
            User newMember = UserData.GetActiveUser();
            int initialCount = project.Members.Count;

            // Act
            Result result = project.AddMember(newMember, ProjectRole.Developer, admin, ProjectData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            project.Members.Count.Should().Be(initialCount + 1);
            project.Members.Should().Contain(m => m.UserId == newMember.Id && m.Role == ProjectRole.Developer);
        }

        [Fact]
        public void Should_RaiseProjectMemberAddedDomainEvent_When_MemberAdded()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetDraftProject(admin);
            User newMember = UserData.GetActiveUser();

            // Act
            project.AddMember(newMember, ProjectRole.Developer, admin, ProjectData.UpdatedOnUtc);

            // Assert
            ProjectMemberAddedDomainEvent domainEvent = AssertDomainEventWasPublished<ProjectMemberAddedDomainEvent>(project);
            domainEvent.ProjectId.Should().Be(project.Id);
            domainEvent.UserId.Should().Be(newMember.Id);
            domainEvent.Role.Should().Be(ProjectRole.Developer);
        }

        [Fact]
        public void Should_AddMemberAddedChangeLog_When_MemberAdded()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetDraftProject(admin);
            User newMember = UserData.GetActiveUser();
            int initialCount = project.ChangeLogs.Count;

            // Act
            project.AddMember(newMember, ProjectRole.Developer, admin, ProjectData.UpdatedOnUtc);

            // Assert
            project.ChangeLogs.Count.Should().Be(initialCount + 1);
            project.ChangeLogs.Should().Contain(cl => cl.ChangeType == ProjectChangeType.MemberAdded);
        }

        [Fact]
        public void Should_Fail_When_NonAdminAddsMember()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetDraftProject(admin);
            User nonAdmin = UserData.GetActiveUser();
            User newMember = UserData.GetActiveUser();

            // Act
            Result result = project.AddMember(newMember, ProjectRole.Developer, nonAdmin, ProjectData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OnlyAdminCanAddMembers);
        }

        [Fact]
        public void Should_Fail_When_AddMemberOnCompletedProject()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProjectWithStatus(ProjectStatus.Completed, admin);
            User newMember = UserData.GetActiveUser();

            // Act
            Result result = project.AddMember(newMember, ProjectRole.Developer, admin, ProjectData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        [Fact]
        public void Should_Fail_When_AddMemberOnArchivedProject()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProjectWithStatus(ProjectStatus.Archived, admin);
            User newMember = UserData.GetActiveUser();

            // Act
            Result result = project.AddMember(newMember, ProjectRole.Developer, admin, ProjectData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        [Fact]
        public void Should_Fail_When_UserIsInactive()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetDraftProject(admin);
            User inactiveUser = UserData.GetInactiveUser();

            // Act
            Result result = project.AddMember(inactiveUser, ProjectRole.Developer, admin, ProjectData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(UserErrors.Inactive);
        }

        [Fact]
        public void Should_Fail_When_UserIsAlreadyMember()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetDraftProject(admin);

            // Act
            Result result = project.AddMember(admin, ProjectRole.Developer, admin, ProjectData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.MemberAlreadyExists);
        }
    }

    public sealed class RemoveMember : BaseTest
    {
        [Fact]
        public void Should_RemoveMember_When_DataIsValid()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetDraftProject(admin);
            User member = UserData.GetActiveUser();
            project.AddMember(member, ProjectRole.Developer, admin, ProjectData.UpdatedOnUtc);
            int countAfterAdd = project.Members.Count;

            // Act
            Result result = project.RemoveMember(member.Id, admin, ProjectData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            project.Members.Count.Should().Be(countAfterAdd - 1);
        }

        [Fact]
        public void Should_RaiseProjectMemberRemovedDomainEvent_When_MemberRemoved()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetDraftProject(admin);
            User member = UserData.GetActiveUser();
            project.AddMember(member, ProjectRole.Developer, admin, ProjectData.UpdatedOnUtc);

            // Act
            project.RemoveMember(member.Id, admin, ProjectData.UpdatedOnUtc);

            // Assert
            ProjectMemberRemovedDomainEvent domainEvent = AssertDomainEventWasPublished<ProjectMemberRemovedDomainEvent>(project);
            domainEvent.ProjectId.Should().Be(project.Id);
            domainEvent.UserId.Should().Be(member.Id);
        }

        [Fact]
        public void Should_AddMemberRemovedChangeLog_When_MemberRemoved()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetDraftProject(admin);
            User member = UserData.GetActiveUser();
            project.AddMember(member, ProjectRole.Developer, admin, ProjectData.UpdatedOnUtc);
            int countAfterAdd = project.ChangeLogs.Count;

            // Act
            project.RemoveMember(member.Id, admin, ProjectData.UpdatedOnUtc);

            // Assert
            project.ChangeLogs.Count.Should().Be(countAfterAdd + 1);
            project.ChangeLogs.Should().Contain(cl => cl.ChangeType == ProjectChangeType.MemberRemoved);
        }

        [Fact]
        public void Should_Fail_When_NonAdminRemovesMember()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetDraftProject(admin);
            User member = UserData.GetActiveUser();
            project.AddMember(member, ProjectRole.Developer, admin, ProjectData.UpdatedOnUtc);

            // Act
            Result result = project.RemoveMember(member.Id, member, ProjectData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OnlyAdminCanRemoveMembers);
        }

        [Fact]
        public void Should_Fail_When_RemoveMemberOnCompletedProject()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProjectWithStatus(ProjectStatus.Completed, admin);

            // Act
            Result result = project.RemoveMember(Guid.NewGuid(), admin, ProjectData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        [Fact]
        public void Should_Fail_When_RemoveMemberOnArchivedProject()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProjectWithStatus(ProjectStatus.Archived, admin);

            // Act
            Result result = project.RemoveMember(Guid.NewGuid(), admin, ProjectData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        [Fact]
        public void Should_Fail_When_MemberNotFound()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetDraftProject(admin);

            // Act
            Result result = project.RemoveMember(Guid.NewGuid(), admin, ProjectData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.MemberNotFound);
        }

        [Fact]
        public void Should_Fail_When_RemovingLastAdmin()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetDraftProject(admin);

            // Act
            Result result = project.RemoveMember(admin.Id, admin, ProjectData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.CannotRemoveLastAdmin);
        }

        [Fact]
        public void Should_AllowRemovingAdmin_When_AnotherAdminExists()
        {
            // Arrange
            User firstAdmin = UserData.GetActiveUser();
            Project project = ProjectData.GetDraftProject(firstAdmin);
            User secondAdmin = UserData.GetActiveUser();
            project.AddMember(secondAdmin, ProjectRole.Admin, firstAdmin, ProjectData.UpdatedOnUtc);

            // Act
            Result result = project.RemoveMember(firstAdmin.Id, secondAdmin, ProjectData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
        }
    }

    public sealed class IncrementWorkItemCounter
    {
        [Fact]
        public void Should_IncrementCounterAndReturnNewValue_When_Called()
        {
            // Arrange
            Project project = ProjectData.GetDraftProject();

            // Act
            int newValue = project.IncrementWorkItemCounter();

            // Assert
            newValue.Should().Be(1);
            project.WorkItemCounter.Should().Be(1);
        }

        [Fact]
        public void Should_IncrementCounterCumulatively_When_CalledMultipleTimes()
        {
            // Arrange
            Project project = ProjectData.GetDraftProject();

            // Act
            project.IncrementWorkItemCounter();
            project.IncrementWorkItemCounter();
            int newValue = project.IncrementWorkItemCounter();

            // Assert
            newValue.Should().Be(3);
            project.WorkItemCounter.Should().Be(3);
        }
    }

    public sealed class CanAddOrUpdateFlow
    {
        [Fact]
        public void Should_ReturnTrue_When_StatusIsDraft()
        {
            Project project = ProjectData.GetProjectWithStatus(ProjectStatus.Draft);
            project.CanAddOrUpdateFlow().Should().BeTrue();
        }

        [Fact]
        public void Should_ReturnTrue_When_StatusIsActive()
        {
            Project project = ProjectData.GetProjectWithStatus(ProjectStatus.Active);
            project.CanAddOrUpdateFlow().Should().BeTrue();
        }

        [Fact]
        public void Should_ReturnFalse_When_StatusIsOnHold()
        {
            Project project = ProjectData.GetProjectWithStatus(ProjectStatus.OnHold);
            project.CanAddOrUpdateFlow().Should().BeFalse();
        }

        [Fact]
        public void Should_ReturnFalse_When_StatusIsCompleted()
        {
            Project project = ProjectData.GetProjectWithStatus(ProjectStatus.Completed);
            project.CanAddOrUpdateFlow().Should().BeFalse();
        }

        [Fact]
        public void Should_ReturnFalse_When_StatusIsArchived()
        {
            Project project = ProjectData.GetProjectWithStatus(ProjectStatus.Archived);
            project.CanAddOrUpdateFlow().Should().BeFalse();
        }
    }

    public sealed class Flows
    {
        [Fact]
        public void Should_ReturnEmptyCollection_When_ProjectIsCreated()
        {
            Project project = ProjectData.GetDraftProject();

            project.Flows.Should().BeEmpty();
        }
    }

    public sealed class CanAddOrUpdateWorkItem
    {
        [Fact]
        public void Should_ReturnTrue_When_StatusIsActive()
        {
            Project project = ProjectData.GetProjectWithStatus(ProjectStatus.Active);
            project.CanAddOrUpdateWorkItem().Should().BeTrue();
        }

        [Fact]
        public void Should_ReturnFalse_When_StatusIsDraft()
        {
            Project project = ProjectData.GetProjectWithStatus(ProjectStatus.Draft);
            project.CanAddOrUpdateWorkItem().Should().BeFalse();
        }

        [Fact]
        public void Should_ReturnFalse_When_StatusIsOnHold()
        {
            Project project = ProjectData.GetProjectWithStatus(ProjectStatus.OnHold);
            project.CanAddOrUpdateWorkItem().Should().BeFalse();
        }

        [Fact]
        public void Should_ReturnFalse_When_StatusIsCompleted()
        {
            Project project = ProjectData.GetProjectWithStatus(ProjectStatus.Completed);
            project.CanAddOrUpdateWorkItem().Should().BeFalse();
        }

        [Fact]
        public void Should_ReturnFalse_When_StatusIsArchived()
        {
            Project project = ProjectData.GetProjectWithStatus(ProjectStatus.Archived);
            project.CanAddOrUpdateWorkItem().Should().BeFalse();
        }
    }
}
