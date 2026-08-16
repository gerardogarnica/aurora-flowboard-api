namespace Aurora.Flowboard.Domain.UnitTests.FlowStateTemplates;

public sealed class FlowStateTemplateTests
{
    public sealed class Create : BaseTest
    {
        [Fact]
        public void Should_CreateTemplate_When_Created()
        {
            // Arrange
            Guid createdBy = Guid.NewGuid();

            // Act
            Result<FlowStateTemplate> result = FlowStateTemplate.Create(ProjectKind.Product, createdBy, FlowStateTemplateData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            result.Value.Kind.Should().Be(ProjectKind.Product);
            result.Value.CreatedBy.Should().Be(createdBy);
            result.Value.CreatedOnUtc.Should().Be(FlowStateTemplateData.CreatedOnUtc);
            result.Value.States.Should().BeEmpty();
        }

        [Fact]
        public void Should_RaiseFlowStateTemplateCreatedDomainEvent_When_Created()
        {
            // Arrange & Act
            FlowStateTemplate template = FlowStateTemplateData.GetTemplate(ProjectKind.Client);

            // Assert
            FlowStateTemplateCreatedDomainEvent domainEvent = AssertDomainEventWasPublished<FlowStateTemplateCreatedDomainEvent>(template);
            domainEvent.FlowStateTemplateId.Should().Be(template.Id);
            domainEvent.Kind.Should().Be(ProjectKind.Client);
        }
    }

    public sealed class AddState : BaseTest
    {
        [Fact]
        public void Should_AddActiveState_When_Valid()
        {
            // Arrange
            FlowStateTemplate template = FlowStateTemplateData.GetTemplate();

            // Act
            Result result = template.AddState(FlowStateTemplateData.StateName, FlowStateCategory.Active, FlowStateTemplateData.Color);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            TemplateFlowState state = template.States.Should().ContainSingle().Subject;
            state.Name.Should().Be(FlowStateTemplateData.StateName);
            state.Category.Should().Be(FlowStateCategory.Active);
            state.Color.Should().Be(FlowStateTemplateData.Color);
            state.SortOrder.Should().Be(1);
        }

        [Fact]
        public void Should_AssignSequentialSortOrder_When_MultipleActiveStatesAdded()
        {
            // Arrange
            FlowStateTemplate template = FlowStateTemplateData.GetTemplate();
            template.AddState("To Do", FlowStateCategory.Active, FlowStateTemplateData.Color);

            // Act
            template.AddState("In Progress", FlowStateCategory.Active, FlowStateTemplateData.Color);

            // Assert
            template.States.Single(s => s.Name == "To Do").SortOrder.Should().Be(1);
            template.States.Single(s => s.Name == "In Progress").SortOrder.Should().Be(2);
        }

        [Fact]
        public void Should_AssignZeroSortOrder_When_NonActiveStateAdded()
        {
            // Arrange
            FlowStateTemplate template = FlowStateTemplateData.GetTemplate();

            // Act
            Result result = template.AddState("Done", FlowStateCategory.Completed, FlowStateTemplateData.Color);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            template.States.Single(s => s.Name == "Done").SortOrder.Should().Be(0);
        }

        [Fact]
        public void Should_RaiseTemplateFlowStateAddedDomainEvent_When_StateAdded()
        {
            // Arrange
            FlowStateTemplate template = FlowStateTemplateData.GetTemplate();

            // Act
            template.AddState(FlowStateTemplateData.StateName, FlowStateCategory.Active, FlowStateTemplateData.Color);

            // Assert
            TemplateFlowStateAddedDomainEvent domainEvent = AssertDomainEventWasPublished<TemplateFlowStateAddedDomainEvent>(template);
            domainEvent.FlowStateTemplateId.Should().Be(template.Id);
            domainEvent.TemplateFlowStateId.Should().Be(template.States.Single().Id);
        }

        [Fact]
        public void Should_Fail_When_NameIsEmpty()
        {
            // Arrange
            FlowStateTemplate template = FlowStateTemplateData.GetTemplate();

            // Act
            Result result = template.AddState(string.Empty, FlowStateCategory.Active, FlowStateTemplateData.Color);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowStateTemplateErrors.StateNameRequired);
        }

        [Fact]
        public void Should_Fail_When_NameExceedsMaxLength()
        {
            // Arrange
            FlowStateTemplate template = FlowStateTemplateData.GetTemplate();
            string longName = new('A', TemplateFlowState.MaxNameLength + 1);

            // Act
            Result result = template.AddState(longName, FlowStateCategory.Active, FlowStateTemplateData.Color);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowStateTemplateErrors.StateNameTooLong);
        }

        [Fact]
        public void Should_Fail_When_DuplicateNameCaseInsensitive()
        {
            // Arrange
            FlowStateTemplate template = FlowStateTemplateData.GetTemplate();
            template.AddState(FlowStateTemplateData.StateName, FlowStateCategory.Active, FlowStateTemplateData.Color);

            // Act
            Result result = template.AddState("in progress", FlowStateCategory.Completed, FlowStateTemplateData.Color);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowStateTemplateErrors.DuplicateStateName);
        }

        [Fact]
        public void Should_Fail_When_MaxActiveStatesReached()
        {
            // Arrange
            FlowStateTemplate template = FlowStateTemplateData.GetTemplate();
            for (int i = 0; i < FlowStateTemplate.MaxActiveStates; i++)
            {
                template.AddState($"Active {i}", FlowStateCategory.Active, FlowStateTemplateData.Color);
            }

            // Act
            Result result = template.AddState("One Too Many", FlowStateCategory.Active, FlowStateTemplateData.Color);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowStateTemplateErrors.MaxActiveStatesReached);
        }
    }

    public sealed class UpdateState : BaseTest
    {
        [Fact]
        public void Should_RenameAndChangeColor_When_Valid()
        {
            // Arrange
            FlowStateTemplate template = FlowStateTemplateData.GetTemplate();
            template.AddState(FlowStateTemplateData.StateName, FlowStateCategory.Active, FlowStateTemplateData.Color);
            TemplateFlowState state = template.States.Single();

            // Act
            Result result = template.UpdateState(state.Id, FlowStateTemplateData.UpdatedStateName, FlowStateTemplateData.UpdatedColor);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            state.Name.Should().Be(FlowStateTemplateData.UpdatedStateName);
            state.Color.Should().Be(FlowStateTemplateData.UpdatedColor);
        }

        [Fact]
        public void Should_NotChangeCategoryOrSortOrder_When_Updated()
        {
            // Arrange
            FlowStateTemplate template = FlowStateTemplateData.GetTemplate();
            template.AddState("To Do", FlowStateCategory.Active, FlowStateTemplateData.Color);
            template.AddState("Done", FlowStateCategory.Completed, FlowStateTemplateData.Color);
            TemplateFlowState done = template.States.Single(s => s.Name == "Done");

            // Act
            Result result = template.UpdateState(done.Id, FlowStateTemplateData.UpdatedStateName, FlowStateTemplateData.UpdatedColor);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            done.Category.Should().Be(FlowStateCategory.Completed);
            done.SortOrder.Should().Be(0);
        }

        [Fact]
        public void Should_RaiseTemplateFlowStateUpdatedDomainEvent_When_Updated()
        {
            // Arrange
            FlowStateTemplate template = FlowStateTemplateData.GetTemplate();
            template.AddState(FlowStateTemplateData.StateName, FlowStateCategory.Active, FlowStateTemplateData.Color);
            TemplateFlowState state = template.States.Single();

            // Act
            template.UpdateState(state.Id, FlowStateTemplateData.UpdatedStateName, FlowStateTemplateData.Color);

            // Assert
            TemplateFlowStateUpdatedDomainEvent domainEvent = AssertDomainEventWasPublished<TemplateFlowStateUpdatedDomainEvent>(template);
            domainEvent.FlowStateTemplateId.Should().Be(template.Id);
            domainEvent.TemplateFlowStateId.Should().Be(state.Id);
        }

        [Fact]
        public void Should_Fail_When_StateNotFound()
        {
            // Arrange
            FlowStateTemplate template = FlowStateTemplateData.GetTemplate();

            // Act
            Result result = template.UpdateState(Guid.NewGuid(), FlowStateTemplateData.StateName, FlowStateTemplateData.Color);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowStateTemplateErrors.StateNotFound);
        }

        [Fact]
        public void Should_Fail_When_DuplicateNameCaseInsensitive()
        {
            // Arrange
            FlowStateTemplate template = FlowStateTemplateData.GetTemplate();
            template.AddState("To Do", FlowStateCategory.Active, FlowStateTemplateData.Color);
            template.AddState("In Progress", FlowStateCategory.Active, FlowStateTemplateData.Color);
            TemplateFlowState state = template.States.Single(s => s.Name == "In Progress");

            // Act
            Result result = template.UpdateState(state.Id, "to do", FlowStateTemplateData.Color);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowStateTemplateErrors.DuplicateStateName);
        }
    }

    public sealed class RemoveState : BaseTest
    {
        [Fact]
        public void Should_RemoveState_When_Valid()
        {
            // Arrange
            FlowStateTemplate template = FlowStateTemplateData.GetTemplate();
            template.AddState(FlowStateTemplateData.StateName, FlowStateCategory.Active, FlowStateTemplateData.Color);
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
            FlowStateTemplate template = FlowStateTemplateData.GetTemplate();
            template.AddState("To Do", FlowStateCategory.Active, FlowStateTemplateData.Color);
            template.AddState("In Progress", FlowStateCategory.Active, FlowStateTemplateData.Color);
            template.AddState("Done", FlowStateCategory.Active, FlowStateTemplateData.Color);
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
            FlowStateTemplate template = FlowStateTemplateData.GetTemplate();
            template.AddState(FlowStateTemplateData.StateName, FlowStateCategory.Active, FlowStateTemplateData.Color);
            TemplateFlowState state = template.States.Single();

            // Act
            template.RemoveState(state.Id);

            // Assert
            TemplateFlowStateRemovedDomainEvent domainEvent = AssertDomainEventWasPublished<TemplateFlowStateRemovedDomainEvent>(template);
            domainEvent.FlowStateTemplateId.Should().Be(template.Id);
            domainEvent.TemplateFlowStateId.Should().Be(state.Id);
        }

        [Fact]
        public void Should_Fail_When_StateNotFound()
        {
            // Arrange
            FlowStateTemplate template = FlowStateTemplateData.GetTemplate();

            // Act
            Result result = template.RemoveState(Guid.NewGuid());

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowStateTemplateErrors.StateNotFound);
        }
    }

    public sealed class ReorderStates : BaseTest
    {
        [Fact]
        public void Should_ReorderActiveStates_When_ValidSetProvided()
        {
            // Arrange
            FlowStateTemplate template = FlowStateTemplateData.GetTemplate();
            template.AddState("To Do", FlowStateCategory.Active, FlowStateTemplateData.Color);
            template.AddState("In Progress", FlowStateCategory.Active, FlowStateTemplateData.Color);
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
            FlowStateTemplate template = FlowStateTemplateData.GetTemplate();
            template.AddState("To Do", FlowStateCategory.Active, FlowStateTemplateData.Color);
            template.AddState("In Progress", FlowStateCategory.Active, FlowStateTemplateData.Color);
            TemplateFlowState toDo = template.States.Single(s => s.Name == "To Do");
            TemplateFlowState inProgress = template.States.Single(s => s.Name == "In Progress");

            // Act
            template.ReorderStates([inProgress.Id, toDo.Id]);

            // Assert
            TemplateFlowStatesReorderedDomainEvent domainEvent = AssertDomainEventWasPublished<TemplateFlowStatesReorderedDomainEvent>(template);
            domainEvent.FlowStateTemplateId.Should().Be(template.Id);
        }

        [Fact]
        public void Should_Fail_When_SetIsMissingAnActiveState()
        {
            // Arrange
            FlowStateTemplate template = FlowStateTemplateData.GetTemplate();
            template.AddState("To Do", FlowStateCategory.Active, FlowStateTemplateData.Color);
            template.AddState("In Progress", FlowStateCategory.Active, FlowStateTemplateData.Color);
            TemplateFlowState toDo = template.States.Single(s => s.Name == "To Do");

            // Act
            Result result = template.ReorderStates([toDo.Id]);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowStateTemplateErrors.InvalidReorderSet);
        }

        [Fact]
        public void Should_Fail_When_SetContainsDuplicateIds()
        {
            // Arrange
            FlowStateTemplate template = FlowStateTemplateData.GetTemplate();
            template.AddState("To Do", FlowStateCategory.Active, FlowStateTemplateData.Color);
            TemplateFlowState toDo = template.States.Single(s => s.Name == "To Do");

            // Act
            Result result = template.ReorderStates([toDo.Id, toDo.Id]);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowStateTemplateErrors.InvalidReorderSet);
        }
    }
}
