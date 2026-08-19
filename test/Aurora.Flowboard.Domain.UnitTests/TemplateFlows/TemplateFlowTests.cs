namespace Aurora.Flowboard.Domain.UnitTests.TemplateFlows;

public sealed class TemplateFlowTests
{
    public sealed class Create : BaseTest
    {
        [Fact]
        public void Should_CreateTemplate_When_Created()
        {
            // Arrange
            Guid createdBy = Guid.NewGuid();

            // Act
            Result<TemplateFlow> result = TemplateFlow.Create(ProjectKind.Product, createdBy, TemplateFlowData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            result.Value.Kind.Should().Be(ProjectKind.Product);
            result.Value.CreatedBy.Should().Be(createdBy);
            result.Value.CreatedOnUtc.Should().Be(TemplateFlowData.CreatedOnUtc);
            result.Value.States.Should().BeEmpty();
        }

        [Fact]
        public void Should_RaiseTemplateFlowCreatedDomainEvent_When_Created()
        {
            // Arrange & Act
            TemplateFlow template = TemplateFlowData.GetTemplate(ProjectKind.Client);

            // Assert
            TemplateFlowCreatedDomainEvent domainEvent = AssertDomainEventWasPublished<TemplateFlowCreatedDomainEvent>(template);
            domainEvent.TemplateFlowId.Should().Be(template.Id);
            domainEvent.Kind.Should().Be(ProjectKind.Client);
        }
    }

    public sealed class AddState : BaseTest
    {
        [Fact]
        public void Should_AddActiveState_When_Valid()
        {
            // Arrange
            TemplateFlow template = TemplateFlowData.GetTemplate();

            // Act
            Result result = template.AddState(TemplateFlowData.StateName, FlowStateCategory.Active, TemplateFlowData.Color);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            TemplateFlowState state = template.States.Should().ContainSingle().Subject;
            state.Name.Should().Be(TemplateFlowData.StateName);
            state.Category.Should().Be(FlowStateCategory.Active);
            state.Color.Should().Be(TemplateFlowData.Color);
            state.SortOrder.Should().Be(1);
        }

        [Fact]
        public void Should_AssignSequentialSortOrder_When_MultipleActiveStatesAdded()
        {
            // Arrange
            TemplateFlow template = TemplateFlowData.GetTemplate();
            template.AddState("To Do", FlowStateCategory.Active, TemplateFlowData.Color);

            // Act
            template.AddState("In Progress", FlowStateCategory.Active, TemplateFlowData.Color);

            // Assert
            template.States.Single(s => s.Name == "To Do").SortOrder.Should().Be(1);
            template.States.Single(s => s.Name == "In Progress").SortOrder.Should().Be(2);
        }

        [Fact]
        public void Should_AssignZeroSortOrder_When_NonActiveStateAdded()
        {
            // Arrange
            TemplateFlow template = TemplateFlowData.GetTemplate();

            // Act
            Result result = template.AddState("Done", FlowStateCategory.Completed, TemplateFlowData.Color);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            template.States.Single(s => s.Name == "Done").SortOrder.Should().Be(0);
        }

        [Fact]
        public void Should_RaiseTemplateFlowStateAddedDomainEvent_When_StateAdded()
        {
            // Arrange
            TemplateFlow template = TemplateFlowData.GetTemplate();

            // Act
            template.AddState(TemplateFlowData.StateName, FlowStateCategory.Active, TemplateFlowData.Color);

            // Assert
            TemplateFlowStateAddedDomainEvent domainEvent = AssertDomainEventWasPublished<TemplateFlowStateAddedDomainEvent>(template);
            domainEvent.TemplateFlowId.Should().Be(template.Id);
            domainEvent.TemplateFlowStateId.Should().Be(template.States.Single().Id);
        }

        [Fact]
        public void Should_Fail_When_NameIsEmpty()
        {
            // Arrange
            TemplateFlow template = TemplateFlowData.GetTemplate();

            // Act
            Result result = template.AddState(string.Empty, FlowStateCategory.Active, TemplateFlowData.Color);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(TemplateFlowErrors.StateNameRequired);
        }

        [Fact]
        public void Should_Fail_When_NameExceedsMaxLength()
        {
            // Arrange
            TemplateFlow template = TemplateFlowData.GetTemplate();
            string longName = new('A', TemplateFlowState.MaxNameLength + 1);

            // Act
            Result result = template.AddState(longName, FlowStateCategory.Active, TemplateFlowData.Color);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(TemplateFlowErrors.StateNameTooLong);
        }

        [Fact]
        public void Should_Fail_When_DuplicateNameCaseInsensitive()
        {
            // Arrange
            TemplateFlow template = TemplateFlowData.GetTemplate();
            template.AddState(TemplateFlowData.StateName, FlowStateCategory.Active, TemplateFlowData.Color);

            // Act
            Result result = template.AddState("in progress", FlowStateCategory.Completed, TemplateFlowData.Color);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(TemplateFlowErrors.DuplicateStateName);
        }

        [Fact]
        public void Should_Fail_When_MaxActiveStatesReached()
        {
            // Arrange
            TemplateFlow template = TemplateFlowData.GetTemplate();
            for (int i = 0; i < TemplateFlow.MaxActiveStates; i++)
            {
                template.AddState($"Active {i}", FlowStateCategory.Active, TemplateFlowData.Color);
            }

            // Act
            Result result = template.AddState("One Too Many", FlowStateCategory.Active, TemplateFlowData.Color);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(TemplateFlowErrors.MaxActiveStatesReached);
        }
    }

    public sealed class UpdateState : BaseTest
    {
        [Fact]
        public void Should_RenameAndChangeColor_When_Valid()
        {
            // Arrange
            TemplateFlow template = TemplateFlowData.GetTemplate();
            template.AddState(TemplateFlowData.StateName, FlowStateCategory.Active, TemplateFlowData.Color);
            TemplateFlowState state = template.States.Single();

            // Act
            Result result = template.UpdateState(state.Id, TemplateFlowData.UpdatedStateName, TemplateFlowData.UpdatedColor);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            state.Name.Should().Be(TemplateFlowData.UpdatedStateName);
            state.Color.Should().Be(TemplateFlowData.UpdatedColor);
        }

        [Fact]
        public void Should_NotChangeCategoryOrSortOrder_When_Updated()
        {
            // Arrange
            TemplateFlow template = TemplateFlowData.GetTemplate();
            template.AddState("To Do", FlowStateCategory.Active, TemplateFlowData.Color);
            template.AddState("Done", FlowStateCategory.Completed, TemplateFlowData.Color);
            TemplateFlowState done = template.States.Single(s => s.Name == "Done");

            // Act
            Result result = template.UpdateState(done.Id, TemplateFlowData.UpdatedStateName, TemplateFlowData.UpdatedColor);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            done.Category.Should().Be(FlowStateCategory.Completed);
            done.SortOrder.Should().Be(0);
        }

        [Fact]
        public void Should_RaiseTemplateFlowStateUpdatedDomainEvent_When_Updated()
        {
            // Arrange
            TemplateFlow template = TemplateFlowData.GetTemplate();
            template.AddState(TemplateFlowData.StateName, FlowStateCategory.Active, TemplateFlowData.Color);
            TemplateFlowState state = template.States.Single();

            // Act
            template.UpdateState(state.Id, TemplateFlowData.UpdatedStateName, TemplateFlowData.Color);

            // Assert
            TemplateFlowStateUpdatedDomainEvent domainEvent = AssertDomainEventWasPublished<TemplateFlowStateUpdatedDomainEvent>(template);
            domainEvent.TemplateFlowId.Should().Be(template.Id);
            domainEvent.TemplateFlowStateId.Should().Be(state.Id);
        }

        [Fact]
        public void Should_Fail_When_StateNotFound()
        {
            // Arrange
            TemplateFlow template = TemplateFlowData.GetTemplate();

            // Act
            Result result = template.UpdateState(Guid.NewGuid(), TemplateFlowData.StateName, TemplateFlowData.Color);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(TemplateFlowErrors.StateNotFound);
        }

        [Fact]
        public void Should_Fail_When_DuplicateNameCaseInsensitive()
        {
            // Arrange
            TemplateFlow template = TemplateFlowData.GetTemplate();
            template.AddState("To Do", FlowStateCategory.Active, TemplateFlowData.Color);
            template.AddState("In Progress", FlowStateCategory.Active, TemplateFlowData.Color);
            TemplateFlowState state = template.States.Single(s => s.Name == "In Progress");

            // Act
            Result result = template.UpdateState(state.Id, "to do", TemplateFlowData.Color);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(TemplateFlowErrors.DuplicateStateName);
        }
    }

    public sealed class RemoveState : BaseTest
    {
        [Fact]
        public void Should_RemoveState_When_Valid()
        {
            // Arrange
            TemplateFlow template = TemplateFlowData.GetTemplate();
            template.AddState(TemplateFlowData.StateName, FlowStateCategory.Active, TemplateFlowData.Color);
            TemplateFlowState state = template.States.Single();

            // Act
            Result result = template.RemoveState(state.Id);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            template.States.Should().BeEmpty();
        }

        [Fact]
        public void Should_ReshuffleSortOrder_When_ActiveStateRemoved()
        {
            // Arrange
            TemplateFlow template = TemplateFlowData.GetTemplate();
            template.AddState("To Do", FlowStateCategory.Active, TemplateFlowData.Color);
            template.AddState("In Progress", FlowStateCategory.Active, TemplateFlowData.Color);
            template.AddState("Done", FlowStateCategory.Active, TemplateFlowData.Color);
            TemplateFlowState inProgress = template.States.Single(s => s.Name == "In Progress");

            // Act
            Result result = template.RemoveState(inProgress.Id);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            template.States.Single(s => s.Name == "To Do").SortOrder.Should().Be(1);
            template.States.Single(s => s.Name == "Done").SortOrder.Should().Be(2);
        }

        [Fact]
        public void Should_RaiseTemplateFlowStateRemovedDomainEvent_When_Removed()
        {
            // Arrange
            TemplateFlow template = TemplateFlowData.GetTemplate();
            template.AddState(TemplateFlowData.StateName, FlowStateCategory.Active, TemplateFlowData.Color);
            TemplateFlowState state = template.States.Single();

            // Act
            template.RemoveState(state.Id);

            // Assert
            TemplateFlowStateRemovedDomainEvent domainEvent = AssertDomainEventWasPublished<TemplateFlowStateRemovedDomainEvent>(template);
            domainEvent.TemplateFlowId.Should().Be(template.Id);
            domainEvent.TemplateFlowStateId.Should().Be(state.Id);
        }

        [Fact]
        public void Should_Fail_When_StateNotFound()
        {
            // Arrange
            TemplateFlow template = TemplateFlowData.GetTemplate();

            // Act
            Result result = template.RemoveState(Guid.NewGuid());

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(TemplateFlowErrors.StateNotFound);
        }
    }

    public sealed class ReorderStates : BaseTest
    {
        [Fact]
        public void Should_ReorderActiveStates_When_ValidSetProvided()
        {
            // Arrange
            TemplateFlow template = TemplateFlowData.GetTemplate();
            template.AddState("To Do", FlowStateCategory.Active, TemplateFlowData.Color);
            template.AddState("In Progress", FlowStateCategory.Active, TemplateFlowData.Color);
            TemplateFlowState toDo = template.States.Single(s => s.Name == "To Do");
            TemplateFlowState inProgress = template.States.Single(s => s.Name == "In Progress");

            // Act
            Result result = template.ReorderStates([inProgress.Id, toDo.Id]);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            inProgress.SortOrder.Should().Be(1);
            toDo.SortOrder.Should().Be(2);
        }

        [Fact]
        public void Should_RaiseTemplateFlowStatesReorderedDomainEvent_When_Reordered()
        {
            // Arrange
            TemplateFlow template = TemplateFlowData.GetTemplate();
            template.AddState("To Do", FlowStateCategory.Active, TemplateFlowData.Color);
            template.AddState("In Progress", FlowStateCategory.Active, TemplateFlowData.Color);
            TemplateFlowState toDo = template.States.Single(s => s.Name == "To Do");
            TemplateFlowState inProgress = template.States.Single(s => s.Name == "In Progress");

            // Act
            template.ReorderStates([inProgress.Id, toDo.Id]);

            // Assert
            TemplateFlowStatesReorderedDomainEvent domainEvent = AssertDomainEventWasPublished<TemplateFlowStatesReorderedDomainEvent>(template);
            domainEvent.TemplateFlowId.Should().Be(template.Id);
        }

        [Fact]
        public void Should_Fail_When_SetIsMissingAnActiveState()
        {
            // Arrange
            TemplateFlow template = TemplateFlowData.GetTemplate();
            template.AddState("To Do", FlowStateCategory.Active, TemplateFlowData.Color);
            template.AddState("In Progress", FlowStateCategory.Active, TemplateFlowData.Color);
            TemplateFlowState toDo = template.States.Single(s => s.Name == "To Do");

            // Act
            Result result = template.ReorderStates([toDo.Id]);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(TemplateFlowErrors.InvalidReorderSet);
        }

        [Fact]
        public void Should_Fail_When_SetContainsDuplicateIds()
        {
            // Arrange
            TemplateFlow template = TemplateFlowData.GetTemplate();
            template.AddState("To Do", FlowStateCategory.Active, TemplateFlowData.Color);
            TemplateFlowState toDo = template.States.Single(s => s.Name == "To Do");

            // Act
            Result result = template.ReorderStates([toDo.Id, toDo.Id]);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(TemplateFlowErrors.InvalidReorderSet);
        }
    }
}
