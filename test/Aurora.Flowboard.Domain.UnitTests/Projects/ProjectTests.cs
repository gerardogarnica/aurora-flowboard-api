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
                ProjectData.Prefix,
                ProjectData.Kind,
                ProjectData.Color,
                creator,
                ProjectData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            result.Value.Name.Should().Be(ProjectData.Name);
            result.Value.Prefix.Should().Be(ProjectData.Prefix);
            result.Value.CreatedBy.Should().Be(creator.Id);
        }

        [Fact]
        public void Should_SetStatusToActive_When_Created()
        {
            // Arrange
            User creator = UserData.GetActiveUser();

            // Act
            Result<Project> result = Project.Create(
                ProjectData.Name,
                ProjectData.Description,
                ProjectData.Prefix,
                ProjectData.Kind,
                ProjectData.Color,
                creator,
                ProjectData.CreatedOnUtc);

            // Assert
            result.Value.Status.Should().Be(ProjectStatus.Active);
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
                ProjectData.Prefix,
                ProjectData.Kind,
                ProjectData.Color,
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
                ProjectData.Prefix,
                ProjectData.Kind,
                ProjectData.Color,
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
                ProjectData.Prefix,
                ProjectData.Kind,
                ProjectData.Color,
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
                ProjectData.Prefix,
                ProjectData.Kind,
                ProjectData.Color,
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
                ProjectData.Prefix,
                ProjectData.Kind,
                ProjectData.Color,
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
                ProjectData.Prefix,
                ProjectData.Kind,
                ProjectData.Color,
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
                ProjectData.Prefix,
                ProjectData.Kind,
                ProjectData.Color,
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
                ProjectData.Prefix,
                ProjectData.Kind,
                ProjectData.Color,
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
                ProjectData.Prefix,
                ProjectData.Kind,
                ProjectData.Color,
                creator,
                ProjectData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.DescriptionTooLong);
        }

        [Fact]
        public void Should_StoreColorValueObject_When_Created()
        {
            // Arrange
            User creator = UserData.GetActiveUser();

            // Act
            Result<Project> result = Project.Create(
                ProjectData.Name,
                ProjectData.Description,
                ProjectData.Prefix,
                ProjectData.Kind,
                ProjectData.Color,
                creator,
                ProjectData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            result.Value.Color.Value.Should().Be("white");
        }

        [Fact]
        public void Should_SetKind_When_ProjectIsCreated()
        {
            // Arrange
            User creator = UserData.GetActiveUser();

            // Act
            Result<Project> result = Project.Create(
                ProjectData.Name,
                ProjectData.Description,
                ProjectData.Prefix,
                ProjectKind.Client,
                ProjectData.Color,
                creator,
                ProjectData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            result.Value.Kind.Should().Be(ProjectKind.Client);
        }
    }

    public sealed class Update : BaseTest
    {
        [Fact]
        public void Should_UpdateProject_When_AdminUpdates()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);

            // Act
            Result result = project.Update("New Name", "New Description", ProjectData.Color, admin, ProjectData.UpdatedOnUtc);

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
            Project project = ProjectData.GetProject(admin);

            // Act
            project.Update("New Name", null, ProjectData.Color, admin, ProjectData.UpdatedOnUtc);

            // Assert
            ProjectUpdatedDomainEvent domainEvent = AssertDomainEventWasPublished<ProjectUpdatedDomainEvent>(project);
            domainEvent.ProjectId.Should().Be(project.Id);
        }

        [Fact]
        public void Should_AddUpdatedChangeLog_When_Updated()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            int initialCount = project.ChangeLogs.Count;

            // Act
            project.Update("New Name", null, ProjectData.Color, admin, ProjectData.UpdatedOnUtc);

            // Assert
            project.ChangeLogs.Count.Should().Be(initialCount + 1);
            project.ChangeLogs.Should().Contain(cl => cl.ChangeType == ProjectChangeType.Updated);
        }

        [Fact]
        public void Should_Fail_When_NonAdminUpdates()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            User nonAdmin = UserData.GetActiveUser();

            // Act
            Result result = project.Update("New Name", null, ProjectData.Color, nonAdmin, ProjectData.UpdatedOnUtc);

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
            Result result = project.Update("New Name", null, ProjectData.Color, admin, ProjectData.UpdatedOnUtc);

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
            Result result = project.Update("New Name", null, ProjectData.Color, admin, ProjectData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        [Fact]
        public void Should_Fail_When_NameIsEmptyOnUpdate()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);

            // Act
            Result result = project.Update(string.Empty, null, ProjectData.Color, admin, ProjectData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.NameRequired);
        }

        [Fact]
        public void Should_Fail_When_NameExceedsMaxLengthOnUpdate()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            string longName = new('A', 101);

            // Act
            Result result = project.Update(longName, null, ProjectData.Color, admin, ProjectData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.NameTooLong);
        }

        [Fact]
        public void Should_Fail_When_DescriptionExceedsMaxLengthOnUpdate()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            string longDescription = new('A', 501);

            // Act
            Result result = project.Update(ProjectData.Name, longDescription, ProjectData.Color, admin, ProjectData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.DescriptionTooLong);
        }
    }

    public sealed class ChangeKind : BaseTest
    {
        [Fact]
        public void Should_ChangeKind_When_AdminChanges()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);

            // Act
            Result result = project.ChangeKind(ProjectKind.Client, admin, ProjectData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            project.Kind.Should().Be(ProjectKind.Client);
            project.UpdatedOnUtc.Should().Be(ProjectData.UpdatedOnUtc);
        }

        [Fact]
        public void Should_RaiseProjectKindChangedDomainEvent_When_KindChanges()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);

            // Act
            project.ChangeKind(ProjectKind.Client, admin, ProjectData.UpdatedOnUtc);

            // Assert
            ProjectKindChangedDomainEvent domainEvent = AssertDomainEventWasPublished<ProjectKindChangedDomainEvent>(project);
            domainEvent.ProjectId.Should().Be(project.Id);
            domainEvent.OldKind.Should().Be(ProjectData.Kind);
            domainEvent.NewKind.Should().Be(ProjectKind.Client);
        }

        [Fact]
        public void Should_AddKindChangedChangeLog_When_KindChanges()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            int initialCount = project.ChangeLogs.Count;

            // Act
            project.ChangeKind(ProjectKind.Client, admin, ProjectData.UpdatedOnUtc);

            // Assert
            project.ChangeLogs.Count.Should().Be(initialCount + 1);
            project.ChangeLogs.Should().ContainSingle(cl =>
                cl.ChangeType == ProjectChangeType.KindChanged && cl.NewKind == ProjectKind.Client);
        }

        [Fact]
        public void Should_Fail_When_NonAdminChangesKind()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            User nonAdmin = UserData.GetActiveUser();

            // Act
            Result result = project.ChangeKind(ProjectKind.Client, nonAdmin, ProjectData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OnlyAdminCanChangeKind);
        }

        [Fact]
        public void Should_Fail_When_KindIsTheSame()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);

            // Act
            Result result = project.ChangeKind(ProjectData.Kind, admin, ProjectData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.KindUnchanged);
        }

        [Fact]
        public void Should_Fail_When_ChangeKindOnCompletedProject()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProjectWithStatus(ProjectStatus.Completed, admin);

            // Act
            Result result = project.ChangeKind(ProjectKind.Client, admin, ProjectData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        [Fact]
        public void Should_Fail_When_ChangeKindOnArchivedProject()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProjectWithStatus(ProjectStatus.Archived, admin);

            // Act
            Result result = project.ChangeKind(ProjectKind.Client, admin, ProjectData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }
    }

    public sealed class ChangeStatus : BaseTest
    {
        public static TheoryData<ProjectKind, ProjectStatus, ProjectStatus> ValidTransitions => new()
        {
            { ProjectKind.Product, ProjectStatus.Active, ProjectStatus.Maintenance },
            { ProjectKind.Product, ProjectStatus.Active, ProjectStatus.Archived },
            { ProjectKind.Product, ProjectStatus.Maintenance, ProjectStatus.Active },
            { ProjectKind.Product, ProjectStatus.Maintenance, ProjectStatus.Archived },
            { ProjectKind.Client, ProjectStatus.Active, ProjectStatus.Completed },
            { ProjectKind.Client, ProjectStatus.Active, ProjectStatus.Archived },
            { ProjectKind.Client, ProjectStatus.Completed, ProjectStatus.Archived },
        };

        public static TheoryData<ProjectKind, ProjectStatus, ProjectStatus> InvalidTransitions => new()
        {
            { ProjectKind.Product, ProjectStatus.Active, ProjectStatus.Completed },
            { ProjectKind.Product, ProjectStatus.Maintenance, ProjectStatus.Completed },
            { ProjectKind.Product, ProjectStatus.Archived, ProjectStatus.Active },
            { ProjectKind.Product, ProjectStatus.Archived, ProjectStatus.Maintenance },
            { ProjectKind.Client, ProjectStatus.Active, ProjectStatus.Maintenance },
            { ProjectKind.Client, ProjectStatus.Completed, ProjectStatus.Active },
            { ProjectKind.Client, ProjectStatus.Completed, ProjectStatus.Maintenance },
            { ProjectKind.Client, ProjectStatus.Archived, ProjectStatus.Completed },
        };

        [Theory]
        [MemberData(nameof(ValidTransitions))]
        public void Should_ChangeStatus_When_TransitionIsValid(ProjectKind kind, ProjectStatus from, ProjectStatus to)
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProjectWithStatus(from, admin, kind);

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
            Project project = ProjectData.GetProject(admin);

            // Act
            project.ChangeStatus(ProjectStatus.Maintenance, admin, ProjectData.UpdatedOnUtc);

            // Assert
            ProjectStatusChangedDomainEvent domainEvent = AssertDomainEventWasPublished<ProjectStatusChangedDomainEvent>(project);
            domainEvent.ProjectId.Should().Be(project.Id);
            domainEvent.OldStatus.Should().Be(ProjectStatus.Active);
            domainEvent.NewStatus.Should().Be(ProjectStatus.Maintenance);
        }

        [Fact]
        public void Should_AddStatusChangedChangeLog_When_StatusChanges()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            int initialCount = project.ChangeLogs.Count;

            // Act
            project.ChangeStatus(ProjectStatus.Maintenance, admin, ProjectData.UpdatedOnUtc);

            // Assert
            project.ChangeLogs.Count.Should().Be(initialCount + 1);
            project.ChangeLogs.Should().Contain(cl => cl.ChangeType == ProjectChangeType.StatusChanged);
        }

        [Fact]
        public void Should_RecordNewStatusOnChangeLog_When_StatusChanges()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);

            // Act
            project.ChangeStatus(ProjectStatus.Maintenance, admin, ProjectData.UpdatedOnUtc);

            // Assert
            project.ChangeLogs.Should().ContainSingle(cl =>
                cl.ChangeType == ProjectChangeType.StatusChanged && cl.NewStatus == ProjectStatus.Maintenance);
        }

        [Fact]
        public void Should_NotSetNewStatusOnChangeLog_When_ChangeIsNotStatusChange()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);

            // Assert
            project.ChangeLogs.Should().OnlyContain(cl => cl.NewStatus == null);
        }

        [Fact]
        public void Should_Fail_When_NonAdminChangesStatus()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            User nonAdmin = UserData.GetActiveUser();

            // Act
            Result result = project.ChangeStatus(ProjectStatus.Maintenance, nonAdmin, ProjectData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OnlyAdminCanChangeStatus);
        }

        [Fact]
        public void Should_Fail_When_StatusIsTheSame()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);

            // Act
            Result result = project.ChangeStatus(ProjectStatus.Active, admin, ProjectData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.InvalidStatusTransition);
        }

        [Theory]
        [MemberData(nameof(InvalidTransitions))]
        public void Should_Fail_When_TransitionIsInvalid(ProjectKind kind, ProjectStatus from, ProjectStatus to)
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProjectWithStatus(from, admin, kind);

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

        [Fact]
        public void Should_ChangeStatusToArchived_When_KindChangedWhileInMaintenanceMakesItUnreachableOtherwise()
        {
            // Arrange: a Product project in Maintenance, whose kind is switched to a timeboxed kind
            // (Client) — Maintenance is not part of the Client path, so Archived must stay reachable
            // as an escape hatch instead of stranding the project.
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProjectWithStatus(ProjectStatus.Maintenance, admin, ProjectKind.Product);
            project.ChangeKind(ProjectKind.Client, admin, ProjectData.UpdatedOnUtc);

            // Act
            Result result = project.ChangeStatus(ProjectStatus.Archived, admin, ProjectData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            project.Status.Should().Be(ProjectStatus.Archived);
        }
    }

    public sealed class AddMember : BaseTest
    {
        [Fact]
        public void Should_AddMember_When_DataIsValid()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
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
            Project project = ProjectData.GetProject(admin);
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
            Project project = ProjectData.GetProject(admin);
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
            Project project = ProjectData.GetProject(admin);
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
            Project project = ProjectData.GetProject(admin);
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
            Project project = ProjectData.GetProject(admin);

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
            Project project = ProjectData.GetProject(admin);
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
            Project project = ProjectData.GetProject(admin);
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
            Project project = ProjectData.GetProject(admin);
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
            Project project = ProjectData.GetProject(admin);
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
            Project project = ProjectData.GetProject(admin);

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
            Project project = ProjectData.GetProject(admin);

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
            Project project = ProjectData.GetProject(firstAdmin);
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
            Project project = ProjectData.GetProject();

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
            Project project = ProjectData.GetProject();

            // Act
            project.IncrementWorkItemCounter();
            project.IncrementWorkItemCounter();
            int newValue = project.IncrementWorkItemCounter();

            // Assert
            newValue.Should().Be(3);
            project.WorkItemCounter.Should().Be(3);
        }
    }

    public sealed class CanModifyFlowStates
    {
        [Fact]
        public void Should_ReturnTrue_When_StatusIsActive()
        {
            Project project = ProjectData.GetProjectWithStatus(ProjectStatus.Active);
            project.CanModifyFlowStates().Should().BeTrue();
        }

        [Fact]
        public void Should_ReturnTrue_When_StatusIsMaintenance()
        {
            Project project = ProjectData.GetProjectWithStatus(ProjectStatus.Maintenance);
            project.CanModifyFlowStates().Should().BeTrue();
        }

        [Fact]
        public void Should_ReturnFalse_When_StatusIsCompleted()
        {
            Project project = ProjectData.GetProjectWithStatus(ProjectStatus.Completed);
            project.CanModifyFlowStates().Should().BeFalse();
        }

        [Fact]
        public void Should_ReturnFalse_When_StatusIsArchived()
        {
            Project project = ProjectData.GetProjectWithStatus(ProjectStatus.Archived);
            project.CanModifyFlowStates().Should().BeFalse();
        }
    }

    public sealed class FlowStates
    {
        [Fact]
        public void Should_ReturnEmptyCollections_When_ProjectIsCreated()
        {
            Project project = ProjectData.GetProject();

            project.FlowStates.Should().BeEmpty();
            project.FlowTransitions.Should().BeEmpty();
        }
    }

    public sealed class AddFlowState : BaseTest
    {
        [Fact]
        public void Should_AddActiveState_When_DataIsValid()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);

            // Act
            Result result = project.AddFlowState("In Progress", FlowStateCategory.Active, ProjectData.FlowStateColor, [], admin);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            project.FlowStates.Should().ContainSingle(s => s.Name == "In Progress" && s.Category == FlowStateCategory.Active);
        }

        [Fact]
        public void Should_AddCompletedState_When_DataIsValid()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);

            // Act
            Result result = project.AddFlowState("Done", FlowStateCategory.Completed, ProjectData.FlowStateColor, [], admin);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            project.FlowStates.Should().ContainSingle(s => s.Name == "Done" && s.Category == FlowStateCategory.Completed);
        }

        [Fact]
        public void Should_AddCancelledState_When_DataIsValid()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);

            // Act
            Result result = project.AddFlowState("Cancelled", FlowStateCategory.Cancelled, ProjectData.FlowStateColor, [], admin);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            project.FlowStates.Should().ContainSingle(s => s.Name == "Cancelled" && s.Category == FlowStateCategory.Cancelled);
        }

        [Fact]
        public void Should_RaiseFlowStateAddedDomainEvent_When_StateAdded()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);

            // Act
            project.AddFlowState("In Progress", FlowStateCategory.Active, ProjectData.FlowStateColor, [], admin);

            // Assert
            FlowStateAddedDomainEvent domainEvent = AssertDomainEventWasPublished<FlowStateAddedDomainEvent>(project);
            domainEvent.ProjectId.Should().Be(project.Id);
            domainEvent.FlowStateId.Should().Be(project.FlowStates.Single().Id);
        }

        [Fact]
        public void Should_SetSortOrderToOne_When_FirstActiveStateAdded()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);

            // Act
            project.AddFlowState("State A", FlowStateCategory.Active, ProjectData.FlowStateColor, [], admin);

            // Assert
            project.FlowStates.Single(s => s.Name == "State A").SortOrder.Should().Be(1);
        }

        [Fact]
        public void Should_IncrementSortOrder_When_SubsequentActiveStateAdded()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            project.AddFlowState("State A", FlowStateCategory.Active, ProjectData.FlowStateColor, [], admin);

            // Act
            project.AddFlowState("State B", FlowStateCategory.Active, ProjectData.FlowStateColor, [], admin);

            // Assert
            project.FlowStates.Single(s => s.Name == "State B").SortOrder.Should().Be(2);
        }

        [Fact]
        public void Should_SetSortOrderToZero_When_CompletedStateAdded()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);

            // Act
            project.AddFlowState("Done", FlowStateCategory.Completed, ProjectData.FlowStateColor, [], admin);

            // Assert
            project.FlowStates.Single(s => s.Name == "Done").SortOrder.Should().Be(0);
        }

        [Fact]
        public void Should_SetSortOrderToZero_When_CancelledStateAdded()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);

            // Act
            project.AddFlowState("Cancelled", FlowStateCategory.Cancelled, ProjectData.FlowStateColor, [], admin);

            // Assert
            project.FlowStates.Single(s => s.Name == "Cancelled").SortOrder.Should().Be(0);
        }

        [Fact]
        public void Should_AddBidirectionalTransitions_When_SecondActiveStateAdded()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            project.AddFlowState("State A", FlowStateCategory.Active, ProjectData.FlowStateColor, [], admin);

            // Act
            project.AddFlowState("State B", FlowStateCategory.Active, ProjectData.FlowStateColor, [], admin);

            // Assert
            FlowState stateA = project.FlowStates.Single(s => s.Name == "State A");
            FlowState stateB = project.FlowStates.Single(s => s.Name == "State B");

            project.FlowTransitions.Should().Contain(t => t.FromStateId == stateA.Id && t.ToStateId == stateB.Id);
            project.FlowTransitions.Should().Contain(t => t.FromStateId == stateB.Id && t.ToStateId == stateA.Id);
        }

        [Fact]
        public void Should_AddTransitionFromLastActiveToCompleted_When_CompletedStateAdded()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            project.AddFlowState("State A", FlowStateCategory.Active, ProjectData.FlowStateColor, [], admin);

            // Act
            project.AddFlowState("Done", FlowStateCategory.Completed, ProjectData.FlowStateColor, [], admin);

            // Assert
            FlowState stateA = project.FlowStates.Single(s => s.Name == "State A");
            FlowState stateDone = project.FlowStates.Single(s => s.Name == "Done");

            project.FlowTransitions.Should().ContainSingle(t =>
                t.FromStateId == stateA.Id && t.ToStateId == stateDone.Id);
        }

        [Fact]
        public void Should_AddTransitionsFromAllActiveStatesToCancelled_When_CancelledStateAdded()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            project.AddFlowState("State A", FlowStateCategory.Active, ProjectData.FlowStateColor, [], admin);
            project.AddFlowState("State B", FlowStateCategory.Active, ProjectData.FlowStateColor, [], admin);

            // Act
            project.AddFlowState("Cancelled", FlowStateCategory.Cancelled, ProjectData.FlowStateColor, [], admin);

            // Assert
            FlowState stateA = project.FlowStates.Single(s => s.Name == "State A");
            FlowState stateB = project.FlowStates.Single(s => s.Name == "State B");
            FlowState stateCancelled = project.FlowStates.Single(s => s.Name == "Cancelled");

            project.FlowTransitions.Should().Contain(t => t.FromStateId == stateA.Id && t.ToStateId == stateCancelled.Id);
            project.FlowTransitions.Should().Contain(t => t.FromStateId == stateB.Id && t.ToStateId == stateCancelled.Id);
        }

        [Fact]
        public void Should_RerouteCompletedTransitions_When_NewActiveStateAdded()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            project.AddFlowState("State A", FlowStateCategory.Active, ProjectData.FlowStateColor, [], admin);
            project.AddFlowState("Done", FlowStateCategory.Completed, ProjectData.FlowStateColor, [], admin);

            // Act
            project.AddFlowState("State B", FlowStateCategory.Active, ProjectData.FlowStateColor, [], admin);

            // Assert
            FlowState stateA = project.FlowStates.Single(s => s.Name == "State A");
            FlowState stateB = project.FlowStates.Single(s => s.Name == "State B");
            FlowState stateDone = project.FlowStates.Single(s => s.Name == "Done");

            project.FlowTransitions.Should().Contain(t => t.FromStateId == stateA.Id && t.ToStateId == stateB.Id);
            project.FlowTransitions.Should().Contain(t => t.FromStateId == stateB.Id && t.ToStateId == stateA.Id);
            project.FlowTransitions.Should().Contain(t => t.FromStateId == stateB.Id && t.ToStateId == stateDone.Id);
            project.FlowTransitions.Should().NotContain(t => t.FromStateId == stateA.Id && t.ToStateId == stateDone.Id);
        }

        [Fact]
        public void Should_Fail_When_NonAdminAddsFlowState()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            User nonAdmin = UserData.GetActiveUser();

            // Act
            Result result = project.AddFlowState("State A", FlowStateCategory.Active, ProjectData.FlowStateColor, [], nonAdmin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OnlyAdminCanModifyFlow);
        }

        [Fact]
        public void Should_Fail_When_ProjectStatusDoesNotAllowAddFlowState()
        {
            // Arrange
            (Project project, User admin) = ProjectData.GetArchivedProjectWithFlowStates();

            // Act
            Result result = project.AddFlowState("State A", FlowStateCategory.Active, ProjectData.FlowStateColor, [], admin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        [Fact]
        public void Should_Fail_When_StateNameIsDuplicate()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            project.AddFlowState("State A", FlowStateCategory.Active, ProjectData.FlowStateColor, [], admin);

            // Act
            Result result = project.AddFlowState("State A", FlowStateCategory.Active, ProjectData.FlowStateColor, [], admin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.DuplicateFlowStateName);
        }

        [Fact]
        public void Should_Fail_When_StateNameIsDuplicate_CaseInsensitive()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            project.AddFlowState("state a", FlowStateCategory.Active, ProjectData.FlowStateColor, [], admin);

            // Act
            Result result = project.AddFlowState("STATE A", FlowStateCategory.Active, ProjectData.FlowStateColor, [], admin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.DuplicateFlowStateName);
        }

        [Fact]
        public void Should_Fail_When_MaxActiveStatesReached()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            for (int i = 1; i <= Project.MaxActiveFlowStates; i++)
            {
                project.AddFlowState($"State {i}", FlowStateCategory.Active, ProjectData.FlowStateColor, [], admin);
            }

            // Act
            Result result = project.AddFlowState("State 11", FlowStateCategory.Active, ProjectData.FlowStateColor, [], admin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.MaxActiveFlowStatesReached);
        }

        [Fact]
        public void Should_Fail_When_StateNameIsEmpty()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);

            // Act
            Result result = project.AddFlowState(string.Empty, FlowStateCategory.Active, ProjectData.FlowStateColor, [], admin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.FlowStateNameRequired);
        }

        [Fact]
        public void Should_Fail_When_StateNameExceedsMaxLength()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            string longName = new('A', FlowState.MaxNameLength + 1);

            // Act
            Result result = project.AddFlowState(longName, FlowStateCategory.Active, ProjectData.FlowStateColor, [], admin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.FlowStateNameTooLong);
        }
    }

    public sealed class RemoveFlowState : BaseTest
    {
        [Fact]
        public void Should_RemoveState_When_StateExists()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            project.AddFlowState("State A", FlowStateCategory.Active, ProjectData.FlowStateColor, [], admin);
            project.AddFlowState("Done", FlowStateCategory.Completed, ProjectData.FlowStateColor, [], admin);
            FlowState stateToRemove = project.FlowStates.Single(s => s.Name == "State A");

            // Act
            Result result = project.RemoveFlowState(stateToRemove.Id, admin);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            project.FlowStates.Should().NotContain(s => s.Id == stateToRemove.Id);
        }

        [Fact]
        public void Should_RemoveTransitionsConnectedToState_When_StateRemoved()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            project.AddFlowState("State A", FlowStateCategory.Active, ProjectData.FlowStateColor, [], admin);
            project.AddFlowState("State B", FlowStateCategory.Active, ProjectData.FlowStateColor, [], admin);
            FlowState stateB = project.FlowStates.Single(s => s.Name == "State B");

            // Act
            project.RemoveFlowState(stateB.Id, admin);

            // Assert
            project.FlowTransitions.Should().NotContain(t => t.FromStateId == stateB.Id || t.ToStateId == stateB.Id);
        }

        [Fact]
        public void Should_DecrementSortOrderOfSuccessors_When_MiddleActiveStateRemoved()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            project.AddFlowState("State A", FlowStateCategory.Active, ProjectData.FlowStateColor, [], admin);
            project.AddFlowState("State B", FlowStateCategory.Active, ProjectData.FlowStateColor, [], admin);
            project.AddFlowState("State C", FlowStateCategory.Active, ProjectData.FlowStateColor, [], admin);
            FlowState stateB = project.FlowStates.Single(s => s.Name == "State B");
            FlowState stateC = project.FlowStates.Single(s => s.Name == "State C");

            // Act
            project.RemoveFlowState(stateB.Id, admin);

            // Assert
            stateC.SortOrder.Should().Be(2);
        }

        [Fact]
        public void Should_BridgeAdjacentActiveStates_When_MiddleActiveStateRemoved()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            project.AddFlowState("State A", FlowStateCategory.Active, ProjectData.FlowStateColor, [], admin);
            project.AddFlowState("State B", FlowStateCategory.Active, ProjectData.FlowStateColor, [], admin);
            project.AddFlowState("State C", FlowStateCategory.Active, ProjectData.FlowStateColor, [], admin);
            FlowState stateA = project.FlowStates.Single(s => s.Name == "State A");
            FlowState stateB = project.FlowStates.Single(s => s.Name == "State B");
            FlowState stateC = project.FlowStates.Single(s => s.Name == "State C");

            // Act
            project.RemoveFlowState(stateB.Id, admin);

            // Assert
            project.FlowTransitions.Should().Contain(t => t.FromStateId == stateA.Id && t.ToStateId == stateC.Id);
        }

        [Fact]
        public void Should_Fail_When_NonAdminRemovesState()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            project.AddFlowState("State A", FlowStateCategory.Active, ProjectData.FlowStateColor, [], admin);
            FlowState state = project.FlowStates.Single();
            User nonAdmin = UserData.GetActiveUser();

            // Act
            Result result = project.RemoveFlowState(state.Id, nonAdmin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OnlyAdminCanModifyFlow);
        }

        [Fact]
        public void Should_Fail_When_ProjectStatusDoesNotAllowRemoveFlowState()
        {
            // Arrange
            (Project project, User admin) = ProjectData.GetArchivedProjectWithFlowStates();

            // Act
            Result result = project.RemoveFlowState(Guid.NewGuid(), admin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        [Fact]
        public void Should_Fail_When_StateNotFound()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);

            // Act
            Result result = project.RemoveFlowState(Guid.NewGuid(), admin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.FlowStateNotFound);
        }

        [Fact]
        public void Should_Fail_When_RemovingLastCompletedState()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            project.AddFlowState("Done", FlowStateCategory.Completed, ProjectData.FlowStateColor, [], admin);
            FlowState completedState = project.FlowStates.Single(s => s.Category == FlowStateCategory.Completed);

            // Act
            Result result = project.RemoveFlowState(completedState.Id, admin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.LastCompletedFlowState);
        }

        [Fact]
        public void Should_Fail_When_RemovingLastCancelledState()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            project.AddFlowState("Cancelled", FlowStateCategory.Cancelled, ProjectData.FlowStateColor, [], admin);
            FlowState cancelledState = project.FlowStates.Single(s => s.Category == FlowStateCategory.Cancelled);

            // Act
            Result result = project.RemoveFlowState(cancelledState.Id, admin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.LastCancelledFlowState);
        }
    }

    public sealed class AddFlowTransitionRole : BaseTest
    {
        [Fact]
        public void Should_AddTransitionRole_When_TransitionExistsAndRoleNotAllowed()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            project.AddFlowState("State A", FlowStateCategory.Active, ProjectData.FlowStateColor, [], admin);
            project.AddFlowState("State B", FlowStateCategory.Active, ProjectData.FlowStateColor, [], admin);
            FlowState stateA = project.FlowStates.Single(s => s.Name == "State A");
            FlowState stateB = project.FlowStates.Single(s => s.Name == "State B");
            FlowTransition transition = project.FlowTransitions.Single(t => t.FromStateId == stateA.Id && t.ToStateId == stateB.Id);

            // Act
            Result result = project.AddFlowTransitionRole(transition.Id, ProjectRole.Developer, admin);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            transition.AllowedRoles.Should().Contain(ProjectRole.Developer);
        }

        [Fact]
        public void Should_Fail_When_NonAdminAddsTransitionRole()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            project.AddFlowState("State A", FlowStateCategory.Active, ProjectData.FlowStateColor, [], admin);
            project.AddFlowState("State B", FlowStateCategory.Active, ProjectData.FlowStateColor, [], admin);
            FlowTransition transition = project.FlowTransitions.First();
            User nonAdmin = UserData.GetActiveUser();

            // Act
            Result result = project.AddFlowTransitionRole(transition.Id, ProjectRole.Developer, nonAdmin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OnlyAdminCanModifyFlow);
        }

        [Fact]
        public void Should_Fail_When_ProjectStatusDoesNotAllowAddTransitionRole()
        {
            // Arrange
            (Project project, User admin) = ProjectData.GetArchivedProjectWithFlowStates();

            // Act
            Result result = project.AddFlowTransitionRole(Guid.NewGuid(), ProjectRole.Developer, admin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        [Fact]
        public void Should_Fail_When_TransitionNotFound()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);

            // Act
            Result result = project.AddFlowTransitionRole(Guid.NewGuid(), ProjectRole.Developer, admin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.FlowTransitionNotFound);
        }

        [Fact]
        public void Should_Fail_When_RoleAlreadyAllowed()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            project.AddFlowState("State A", FlowStateCategory.Active, ProjectData.FlowStateColor, [ProjectRole.Developer], admin);
            project.AddFlowState("State B", FlowStateCategory.Active, ProjectData.FlowStateColor, [ProjectRole.Developer], admin);
            FlowState stateA = project.FlowStates.Single(s => s.Name == "State A");
            FlowState stateB = project.FlowStates.Single(s => s.Name == "State B");
            FlowTransition transition = project.FlowTransitions.Single(t => t.FromStateId == stateA.Id && t.ToStateId == stateB.Id);

            // Act
            Result result = project.AddFlowTransitionRole(transition.Id, ProjectRole.Developer, admin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.FlowTransitionRoleAlreadyAllowed);
        }
    }

    public sealed class RemoveFlowTransitionRole : BaseTest
    {
        [Fact]
        public void Should_RemoveTransitionRole_When_TransitionExistsAndRoleIsAllowed()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            project.AddFlowState("State A", FlowStateCategory.Active, ProjectData.FlowStateColor, [ProjectRole.Developer], admin);
            project.AddFlowState("State B", FlowStateCategory.Active, ProjectData.FlowStateColor, [ProjectRole.Developer], admin);
            FlowState stateA = project.FlowStates.Single(s => s.Name == "State A");
            FlowState stateB = project.FlowStates.Single(s => s.Name == "State B");
            FlowTransition transition = project.FlowTransitions.Single(t => t.FromStateId == stateA.Id && t.ToStateId == stateB.Id);

            // Act
            Result result = project.RemoveFlowTransitionRole(transition.Id, ProjectRole.Developer, admin);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            transition.AllowedRoles.Should().NotContain(ProjectRole.Developer);
        }

        [Fact]
        public void Should_Fail_When_NonAdminRemovesTransitionRole()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            project.AddFlowState("State A", FlowStateCategory.Active, ProjectData.FlowStateColor, [], admin);
            project.AddFlowState("State B", FlowStateCategory.Active, ProjectData.FlowStateColor, [], admin);
            FlowTransition transition = project.FlowTransitions.First();
            User nonAdmin = UserData.GetActiveUser();

            // Act
            Result result = project.RemoveFlowTransitionRole(transition.Id, ProjectRole.Developer, nonAdmin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OnlyAdminCanModifyFlow);
        }

        [Fact]
        public void Should_Fail_When_ProjectStatusDoesNotAllowRemoveTransitionRole()
        {
            // Arrange
            (Project project, User admin) = ProjectData.GetArchivedProjectWithFlowStates();

            // Act
            Result result = project.RemoveFlowTransitionRole(Guid.NewGuid(), ProjectRole.Developer, admin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        [Fact]
        public void Should_Fail_When_TransitionNotFound()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);

            // Act
            Result result = project.RemoveFlowTransitionRole(Guid.NewGuid(), ProjectRole.Developer, admin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.FlowTransitionNotFound);
        }

        [Fact]
        public void Should_Fail_When_RoleNotAllowed()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            project.AddFlowState("State A", FlowStateCategory.Active, ProjectData.FlowStateColor, [], admin);
            project.AddFlowState("State B", FlowStateCategory.Active, ProjectData.FlowStateColor, [], admin);
            FlowState stateA = project.FlowStates.Single(s => s.Name == "State A");
            FlowState stateB = project.FlowStates.Single(s => s.Name == "State B");
            FlowTransition transition = project.FlowTransitions.Single(t => t.FromStateId == stateA.Id && t.ToStateId == stateB.Id);

            // Act
            Result result = project.RemoveFlowTransitionRole(transition.Id, ProjectRole.Developer, admin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.FlowTransitionRoleNotAllowed);
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
        public void Should_ReturnTrue_When_StatusIsMaintenance()
        {
            Project project = ProjectData.GetProjectWithStatus(ProjectStatus.Maintenance);
            project.CanAddOrUpdateWorkItem().Should().BeTrue();
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
