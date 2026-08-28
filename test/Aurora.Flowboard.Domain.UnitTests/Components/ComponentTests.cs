namespace Aurora.Flowboard.Domain.UnitTests.Components;

public sealed class ComponentTests
{
    public sealed class Create : BaseTest
    {
        [Fact]
        public void Should_CreateComponent_When_AdminCreates()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);

            // Act
            Result<Component> result = Component.Create(ComponentData.Name, project, admin, ComponentData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            result.Value.Name.Should().Be(ComponentData.Name);
            result.Value.Status.Should().Be(ComponentStatus.Active);
            result.Value.ProjectId.Should().Be(project.Id);
            result.Value.CreatedBy.Should().Be(admin.Id);
            result.Value.CreatedOnUtc.Should().Be(ComponentData.CreatedOnUtc);
        }

        [Fact]
        public void Should_AddComponentToProject_When_Created()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);

            // Act
            Result<Component> result = Component.Create(ComponentData.Name, project, admin, ComponentData.CreatedOnUtc);

            // Assert
            project.Components.Should().ContainSingle(c => c.Id == result.Value.Id);
            result.Value.Project.Should().BeSameAs(project);
        }

        [Fact]
        public void Should_TrimName_When_Created()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);

            // Act
            Result<Component> result = Component.Create("  Billing  ", project, admin, ComponentData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            result.Value.Name.Should().Be(ComponentData.Name);
        }

        [Fact]
        public void Should_RaiseComponentCreatedDomainEvent_When_Created()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);

            // Act
            Component component = ComponentData.GetComponent(project, admin);

            // Assert
            ComponentCreatedDomainEvent domainEvent = AssertDomainEventWasPublished<ComponentCreatedDomainEvent>(component);
            domainEvent.ComponentId.Should().Be(component.Id);
            domainEvent.ProjectId.Should().Be(project.Id);
            domainEvent.Name.Should().Be(ComponentData.Name);
        }

        [Fact]
        public void Should_Fail_When_NonAdminCreates()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            User nonAdmin = UserData.GetActiveUser();

            // Act
            Result<Component> result = Component.Create(ComponentData.Name, project, nonAdmin, ComponentData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ComponentErrors.OnlyAdminCanManageComponent);
        }

        [Fact]
        public void Should_Fail_When_NameIsEmpty()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);

            // Act
            Result<Component> result = Component.Create(string.Empty, project, admin, ComponentData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ComponentErrors.NameRequired);
        }

        [Fact]
        public void Should_Fail_When_NameExceedsMaxLength()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            string longName = new('A', Component.MaxNameLength + 1);

            // Act
            Result<Component> result = Component.Create(longName, project, admin, ComponentData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ComponentErrors.NameTooLong);
        }

        [Fact]
        public void Should_Fail_When_DuplicateNameCaseInsensitive()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            ComponentData.GetComponent(project, admin);

            // Act
            Result<Component> result = Component.Create("billing", project, admin, ComponentData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ComponentErrors.DuplicateName);
        }
    }

    public sealed class Rename : BaseTest
    {
        [Fact]
        public void Should_RenameComponent_When_AdminRenames()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            Component component = ComponentData.GetComponent(project, admin);

            // Act
            Result result = component.Rename(ComponentData.RenamedTo, admin, ComponentData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            component.Name.Should().Be(ComponentData.RenamedTo);
            component.UpdatedOnUtc.Should().Be(ComponentData.UpdatedOnUtc);
        }

        [Fact]
        public void Should_RaiseComponentRenamedDomainEvent_When_Renamed()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            Component component = ComponentData.GetComponent(project, admin);

            // Act
            component.Rename(ComponentData.RenamedTo, admin, ComponentData.UpdatedOnUtc);

            // Assert
            ComponentRenamedDomainEvent domainEvent = AssertDomainEventWasPublished<ComponentRenamedDomainEvent>(component);
            domainEvent.ComponentId.Should().Be(component.Id);
            domainEvent.ProjectId.Should().Be(project.Id);
            domainEvent.OldName.Should().Be(ComponentData.Name);
            domainEvent.NewName.Should().Be(ComponentData.RenamedTo);
        }

        [Fact]
        public void Should_Fail_When_NonAdminRenames()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            Component component = ComponentData.GetComponent(project, admin);
            User nonAdmin = UserData.GetActiveUser();

            // Act
            Result result = component.Rename(ComponentData.RenamedTo, nonAdmin, ComponentData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ComponentErrors.OnlyAdminCanManageComponent);
        }

        [Fact]
        public void Should_Fail_When_NameIsEmpty()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            Component component = ComponentData.GetComponent(project, admin);

            // Act
            Result result = component.Rename(string.Empty, admin, ComponentData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ComponentErrors.NameRequired);
        }

        [Fact]
        public void Should_Fail_When_NameExceedsMaxLength()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            Component component = ComponentData.GetComponent(project, admin);
            string longName = new('A', Component.MaxNameLength + 1);

            // Act
            Result result = component.Rename(longName, admin, ComponentData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ComponentErrors.NameTooLong);
        }

        [Fact]
        public void Should_Fail_When_RenamingToDuplicateNameCaseInsensitive()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            ComponentData.GetComponent(project, admin);
            Component portal = ComponentData.GetComponent(project, admin, "Portal");

            // Act
            Result result = portal.Rename("billing", admin, ComponentData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ComponentErrors.DuplicateName);
        }

        [Fact]
        public void Should_AllowRenamingToItsOwnName_When_CaseChangesOnly()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            Component component = ComponentData.GetComponent(project, admin);

            // Act
            Result result = component.Rename("billing", admin, ComponentData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            component.Name.Should().Be("billing");
        }
    }

    public sealed class Retire : BaseTest
    {
        [Fact]
        public void Should_RetireComponent_When_AdminRetires()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            Component component = ComponentData.GetComponent(project, admin);

            // Act
            Result result = component.Retire(admin, 0, ComponentData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            component.Status.Should().Be(ComponentStatus.Retired);
            component.UpdatedOnUtc.Should().Be(ComponentData.UpdatedOnUtc);
        }

        [Fact]
        public void Should_RaiseComponentRetiredDomainEvent_When_Retired()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            Component component = ComponentData.GetComponent(project, admin);

            // Act
            component.Retire(admin, 0, ComponentData.UpdatedOnUtc);

            // Assert
            ComponentRetiredDomainEvent domainEvent = AssertDomainEventWasPublished<ComponentRetiredDomainEvent>(component);
            domainEvent.ComponentId.Should().Be(component.Id);
            domainEvent.ProjectId.Should().Be(project.Id);
        }

        [Fact]
        public void Should_Fail_When_NonAdminRetires()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            Component component = ComponentData.GetComponent(project, admin);
            User nonAdmin = UserData.GetActiveUser();

            // Act
            Result result = component.Retire(nonAdmin, 0, ComponentData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ComponentErrors.OnlyAdminCanManageComponent);
        }

        [Fact]
        public void Should_Fail_When_ComponentAlreadyRetired()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            Component component = ComponentData.GetComponent(project, admin);
            component.Retire(admin, 0, ComponentData.UpdatedOnUtc);

            // Act
            Result result = component.Retire(admin, 0, ComponentData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ComponentErrors.AlreadyRetired);
        }

        [Fact]
        public void Should_Fail_When_ComponentHasOpenWorkItems()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProject(admin);
            Component component = ComponentData.GetComponent(project, admin);

            // Act
            Result result = component.Retire(admin, 1, ComponentData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ComponentErrors.CannotRetireWithOpenWorkItems);
        }
    }
}
