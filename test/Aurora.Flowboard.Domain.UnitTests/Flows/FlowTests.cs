namespace Aurora.Flowboard.Domain.UnitTests.Flows;

public sealed class FlowTests
{
    public sealed class Create : BaseTest
    {
        [Fact]
        public void Should_CreateFlow_When_DataIsValid()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetDraftProject(admin);

            // Act
            Result<Flow> result = Flow.Create(FlowData.Name, FlowData.Description, project, true, FlowData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            result.Value.Name.Should().Be(FlowData.Name);
            result.Value.Description.Should().Be(FlowData.Description);
            result.Value.IsDefault.Should().BeTrue();
            result.Value.ProjectId.Should().Be(project.Id);
        }

        [Fact]
        public void Should_SetIsActiveToTrue_When_Created()
        {
            // Arrange
            Project project = ProjectData.GetDraftProject();

            // Act
            Result<Flow> result = Flow.Create(FlowData.Name, null, project, false, FlowData.CreatedOnUtc);

            // Assert
            result.Value.IsActive.Should().BeTrue();
        }

        [Fact]
        public void Should_TrimNameAndDescription_When_Creating()
        {
            // Arrange
            Project project = ProjectData.GetDraftProject();

            // Act
            Result<Flow> result = Flow.Create($"  {FlowData.Name}  ", $"  {FlowData.Description}  ", project, false, FlowData.CreatedOnUtc);

            // Assert
            result.Value.Name.Should().Be(FlowData.Name);
            result.Value.Description.Should().Be(FlowData.Description);
        }

        [Fact]
        public void Should_RaiseFlowCreatedDomainEvent_When_Created()
        {
            // Arrange
            Project project = ProjectData.GetDraftProject();

            // Act
            Result<Flow> result = Flow.Create(FlowData.Name, null, project, false, FlowData.CreatedOnUtc);

            // Assert
            FlowCreatedDomainEvent domainEvent = AssertDomainEventWasPublished<FlowCreatedDomainEvent>(result.Value);
            domainEvent.FlowId.Should().Be(result.Value.Id);
        }

        [Fact]
        public void Should_Fail_When_NameIsEmpty()
        {
            // Arrange
            Project project = ProjectData.GetDraftProject();

            // Act
            Result<Flow> result = Flow.Create(string.Empty, null, project, false, FlowData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowErrors.NameRequired);
        }

        [Fact]
        public void Should_Fail_When_NameIsWhitespace()
        {
            // Arrange
            Project project = ProjectData.GetDraftProject();

            // Act
            Result<Flow> result = Flow.Create("   ", null, project, false, FlowData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowErrors.NameRequired);
        }

        [Fact]
        public void Should_Fail_When_NameExceedsMaxLength()
        {
            // Arrange
            Project project = ProjectData.GetDraftProject();
            string longName = new('A', 101);

            // Act
            Result<Flow> result = Flow.Create(longName, null, project, false, FlowData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowErrors.NameTooLong);
        }

        [Fact]
        public void Should_Fail_When_DescriptionExceedsMaxLength()
        {
            // Arrange
            Project project = ProjectData.GetDraftProject();
            string longDescription = new('A', 501);

            // Act
            Result<Flow> result = Flow.Create(FlowData.Name, longDescription, project, false, FlowData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowErrors.DescriptionTooLong);
        }

        [Fact]
        public void Should_Fail_When_ProjectDoesNotAllowFlowCreation()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Project project = ProjectData.GetProjectWithStatus(ProjectStatus.OnHold, admin);

            // Act
            Result<Flow> result = Flow.Create(FlowData.Name, null, project, false, FlowData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }
    }

    public sealed class Update : BaseTest
    {
        [Fact]
        public void Should_UpdateFlow_When_AdminUpdates()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);

            // Act
            Result result = flow.Update("New Name", "New Description", admin, FlowData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            flow.Name.Should().Be("New Name");
            flow.Description.Should().Be("New Description");
        }

        [Fact]
        public void Should_RaiseFlowUpdatedDomainEvent_When_Updated()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);

            // Act
            flow.Update("New Name", null, admin, FlowData.UpdatedOnUtc);

            // Assert
            FlowUpdatedDomainEvent domainEvent = AssertDomainEventWasPublished<FlowUpdatedDomainEvent>(flow);
            domainEvent.FlowId.Should().Be(flow.Id);
        }

        [Fact]
        public void Should_TrimNameAndDescription_When_Updating()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);

            // Act
            flow.Update($"  New Name  ", $"  New Description  ", admin, FlowData.UpdatedOnUtc);

            // Assert
            flow.Name.Should().Be("New Name");
            flow.Description.Should().Be("New Description");
        }

        [Fact]
        public void Should_Fail_When_NonAdminUpdates()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);
            User nonAdmin = UserData.GetActiveUser();

            // Act
            Result result = flow.Update("New Name", null, nonAdmin, FlowData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowErrors.OnlyAdminCanModifyFlow);
        }

        [Fact]
        public void Should_Fail_When_FlowIsDeactivated()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetDeactivatedFlow(admin);

            // Act
            Result result = flow.Update("New Name", null, admin, FlowData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowErrors.Deactivated);
        }

        [Fact]
        public void Should_Fail_When_ProjectDoesNotAllowUpdate()
        {
            // Arrange
            (Flow flow, User admin) = FlowData.GetFlowOnBlockedProject();

            // Act
            Result result = flow.Update("New Name", null, admin, FlowData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        [Fact]
        public void Should_Fail_When_NameIsEmpty()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);

            // Act
            Result result = flow.Update(string.Empty, null, admin, FlowData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowErrors.NameRequired);
        }

        [Fact]
        public void Should_Fail_When_NameExceedsMaxLength()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);
            string longName = new('A', 101);

            // Act
            Result result = flow.Update(longName, null, admin, FlowData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowErrors.NameTooLong);
        }

        [Fact]
        public void Should_Fail_When_DescriptionExceedsMaxLengthOnUpdate()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);
            string longDescription = new('A', 501);

            // Act
            Result result = flow.Update(FlowData.Name, longDescription, admin, FlowData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowErrors.DescriptionTooLong);
        }
    }

    public sealed class Deactivate : BaseTest
    {
        [Fact]
        public void Should_DeactivateFlow_When_AdminDeactivatesActiveNonDefaultFlow()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin, isDefault: false);

            // Act
            Result result = flow.Deactivate(admin, FlowData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
        }

        [Fact]
        public void Should_SetIsActiveToFalse_When_Deactivated()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin, isDefault: false);

            // Act
            flow.Deactivate(admin, FlowData.UpdatedOnUtc);

            // Assert
            flow.IsActive.Should().BeFalse();
        }

        [Fact]
        public void Should_Fail_When_NonAdminDeactivates()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);
            User nonAdmin = UserData.GetActiveUser();

            // Act
            Result result = flow.Deactivate(nonAdmin, FlowData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowErrors.OnlyAdminCanModifyFlow);
        }

        [Fact]
        public void Should_Fail_When_FlowIsAlreadyDeactivated()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetDeactivatedFlow(admin);

            // Act
            Result result = flow.Deactivate(admin, FlowData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowErrors.AlreadyDeactivated);
        }

        [Fact]
        public void Should_Fail_When_FlowIsDefault()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin, isDefault: true);

            // Act
            Result result = flow.Deactivate(admin, FlowData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowErrors.IsDefault);
        }

        [Fact]
        public void Should_Fail_When_ProjectDoesNotAllowDeactivation()
        {
            // Arrange
            (Flow flow, User admin) = FlowData.GetFlowOnBlockedProject();

            // Act
            Result result = flow.Deactivate(admin, FlowData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }
    }

    public sealed class AddState : BaseTest
    {
        [Fact]
        public void Should_AddActiveState_When_DataIsValid()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);

            // Act
            Result result = flow.AddState("In Progress", FlowStateCategory.Active, [], admin);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            flow.States.Should().ContainSingle(s => s.Name == "In Progress" && s.Category == FlowStateCategory.Active);
        }

        [Fact]
        public void Should_AddCompletedState_When_DataIsValid()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);

            // Act
            Result result = flow.AddState("Done", FlowStateCategory.Completed, [], admin);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            flow.States.Should().ContainSingle(s => s.Name == "Done" && s.Category == FlowStateCategory.Completed);
        }

        [Fact]
        public void Should_AddCancelledState_When_DataIsValid()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);

            // Act
            Result result = flow.AddState("Cancelled", FlowStateCategory.Cancelled, [], admin);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            flow.States.Should().ContainSingle(s => s.Name == "Cancelled" && s.Category == FlowStateCategory.Cancelled);
        }

        [Fact]
        public void Should_RaiseFlowStateAddedDomainEvent_When_StateAdded()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);

            // Act
            flow.AddState("In Progress", FlowStateCategory.Active, [], admin);

            // Assert
            FlowStateAddedDomainEvent domainEvent = AssertDomainEventWasPublished<FlowStateAddedDomainEvent>(flow);
            domainEvent.FlowId.Should().Be(flow.Id);
            domainEvent.StateId.Should().Be(flow.States.Single().Id);
        }

        [Fact]
        public void Should_SetSortOrderToOne_When_FirstActiveStateAdded()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);

            // Act
            flow.AddState("State A", FlowStateCategory.Active, [], admin);

            // Assert
            flow.States.Single(s => s.Name == "State A").SortOrder.Should().Be(1);
        }

        [Fact]
        public void Should_IncrementSortOrder_When_SubsequentActiveStateAdded()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);
            flow.AddState("State A", FlowStateCategory.Active, [], admin);

            // Act
            flow.AddState("State B", FlowStateCategory.Active, [], admin);

            // Assert
            flow.States.Single(s => s.Name == "State B").SortOrder.Should().Be(2);
        }

        [Fact]
        public void Should_SetSortOrderToZero_When_CompletedStateAdded()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);

            // Act
            flow.AddState("Done", FlowStateCategory.Completed, [], admin);

            // Assert
            flow.States.Single(s => s.Name == "Done").SortOrder.Should().Be(0);
        }

        [Fact]
        public void Should_SetSortOrderToZero_When_CancelledStateAdded()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);

            // Act
            flow.AddState("Cancelled", FlowStateCategory.Cancelled, [], admin);

            // Assert
            flow.States.Single(s => s.Name == "Cancelled").SortOrder.Should().Be(0);
        }

        [Fact]
        public void Should_AddBidirectionalTransitions_When_SecondActiveStateAdded()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);
            flow.AddState("State A", FlowStateCategory.Active, [], admin);

            // Act
            flow.AddState("State B", FlowStateCategory.Active, [], admin);

            // Assert
            FlowState stateA = flow.States.Single(s => s.Name == "State A");
            FlowState stateB = flow.States.Single(s => s.Name == "State B");

            flow.Transitions.Should().Contain(t => t.FromStateId == stateA.Id && t.ToStateId == stateB.Id);
            flow.Transitions.Should().Contain(t => t.FromStateId == stateB.Id && t.ToStateId == stateA.Id);
        }

        [Fact]
        public void Should_AddTransitionFromLastActiveToCompleted_When_CompletedStateAdded()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);
            flow.AddState("State A", FlowStateCategory.Active, [], admin);

            // Act
            flow.AddState("Done", FlowStateCategory.Completed, [], admin);

            // Assert
            FlowState stateA = flow.States.Single(s => s.Name == "State A");
            FlowState stateDone = flow.States.Single(s => s.Name == "Done");

            flow.Transitions.Should().ContainSingle(t =>
                t.FromStateId == stateA.Id && t.ToStateId == stateDone.Id);
        }

        [Fact]
        public void Should_AddTransitionsFromAllActiveStatesToCancelled_When_CancelledStateAdded()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);
            flow.AddState("State A", FlowStateCategory.Active, [], admin);
            flow.AddState("State B", FlowStateCategory.Active, [], admin);

            // Act
            flow.AddState("Cancelled", FlowStateCategory.Cancelled, [], admin);

            // Assert
            FlowState stateA = flow.States.Single(s => s.Name == "State A");
            FlowState stateB = flow.States.Single(s => s.Name == "State B");
            FlowState stateCancelled = flow.States.Single(s => s.Name == "Cancelled");

            flow.Transitions.Should().Contain(t => t.FromStateId == stateA.Id && t.ToStateId == stateCancelled.Id);
            flow.Transitions.Should().Contain(t => t.FromStateId == stateB.Id && t.ToStateId == stateCancelled.Id);
        }

        [Fact]
        public void Should_RerouteCompletedTransitions_When_NewActiveStateAdded()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);
            flow.AddState("State A", FlowStateCategory.Active, [], admin);
            flow.AddState("Done", FlowStateCategory.Completed, [], admin);

            // Act
            flow.AddState("State B", FlowStateCategory.Active, [], admin);

            // Assert
            FlowState stateA = flow.States.Single(s => s.Name == "State A");
            FlowState stateB = flow.States.Single(s => s.Name == "State B");
            FlowState stateDone = flow.States.Single(s => s.Name == "Done");

            flow.Transitions.Should().Contain(t => t.FromStateId == stateA.Id && t.ToStateId == stateB.Id);
            flow.Transitions.Should().Contain(t => t.FromStateId == stateB.Id && t.ToStateId == stateA.Id);
            flow.Transitions.Should().Contain(t => t.FromStateId == stateB.Id && t.ToStateId == stateDone.Id);
            flow.Transitions.Should().NotContain(t => t.FromStateId == stateA.Id && t.ToStateId == stateDone.Id);
        }

        [Fact]
        public void Should_Fail_When_NonAdminAddsState()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);
            User nonAdmin = UserData.GetActiveUser();

            // Act
            Result result = flow.AddState("State A", FlowStateCategory.Active, [], nonAdmin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowErrors.OnlyAdminCanModifyFlow);
        }

        [Fact]
        public void Should_Fail_When_FlowIsDeactivated()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetDeactivatedFlow(admin);

            // Act
            Result result = flow.AddState("State A", FlowStateCategory.Active, [], admin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowErrors.Deactivated);
        }

        [Fact]
        public void Should_Fail_When_ProjectDoesNotAllowAddState()
        {
            // Arrange
            (Flow flow, User admin) = FlowData.GetFlowOnBlockedProject();

            // Act
            Result result = flow.AddState("State A", FlowStateCategory.Active, [], admin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        [Fact]
        public void Should_Fail_When_StateNameIsDuplicate()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);
            flow.AddState("State A", FlowStateCategory.Active, [], admin);

            // Act
            Result result = flow.AddState("State A", FlowStateCategory.Active, [], admin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowErrors.DuplicateStateName);
        }

        [Fact]
        public void Should_Fail_When_StateNameIsDuplicate_CaseInsensitive()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);
            flow.AddState("state a", FlowStateCategory.Active, [], admin);

            // Act
            Result result = flow.AddState("STATE A", FlowStateCategory.Active, [], admin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowErrors.DuplicateStateName);
        }

        [Fact]
        public void Should_Fail_When_MaxActiveStatesReached()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);
            for (int i = 1; i <= 10; i++)
            {
                flow.AddState($"State {i}", FlowStateCategory.Active, [], admin);
            }

            // Act
            Result result = flow.AddState("State 11", FlowStateCategory.Active, [], admin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowErrors.MaxActiveStatesReached);
        }

        [Fact]
        public void Should_Fail_When_StateNameIsEmpty()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);

            // Act
            Result result = flow.AddState(string.Empty, FlowStateCategory.Active, [], admin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowErrors.StateNameRequired);
        }

        [Fact]
        public void Should_Fail_When_StateNameExceedsMaxLength()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);
            string longName = new('A', 51);

            // Act
            Result result = flow.AddState(longName, FlowStateCategory.Active, [], admin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowErrors.StateNameTooLong);
        }
    }

    public sealed class RemoveState : BaseTest
    {
        [Fact]
        public void Should_RemoveState_When_StateExists()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);
            flow.AddState("State A", FlowStateCategory.Active, [], admin);
            flow.AddState("Done", FlowStateCategory.Completed, [], admin);
            FlowState stateToRemove = flow.States.Single(s => s.Name == "State A");

            // Act
            Result result = flow.RemoveState(stateToRemove.Id, admin);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            flow.States.Should().NotContain(s => s.Id == stateToRemove.Id);
        }

        [Fact]
        public void Should_RemoveTransitionsConnectedToState_When_StateRemoved()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);
            flow.AddState("State A", FlowStateCategory.Active, [], admin);
            flow.AddState("State B", FlowStateCategory.Active, [], admin);
            FlowState stateB = flow.States.Single(s => s.Name == "State B");

            // Act
            flow.RemoveState(stateB.Id, admin);

            // Assert
            flow.Transitions.Should().NotContain(t => t.FromStateId == stateB.Id || t.ToStateId == stateB.Id);
        }

        [Fact]
        public void Should_DecrementSortOrderOfSuccessors_When_MiddleActiveStateRemoved()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);
            flow.AddState("State A", FlowStateCategory.Active, [], admin);
            flow.AddState("State B", FlowStateCategory.Active, [], admin);
            flow.AddState("State C", FlowStateCategory.Active, [], admin);
            FlowState stateB = flow.States.Single(s => s.Name == "State B");
            FlowState stateC = flow.States.Single(s => s.Name == "State C");

            // Act
            flow.RemoveState(stateB.Id, admin);

            // Assert
            stateC.SortOrder.Should().Be(2);
        }

        [Fact]
        public void Should_BridgeAdjacentActiveStates_When_MiddleActiveStateRemoved()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);
            flow.AddState("State A", FlowStateCategory.Active, [], admin);
            flow.AddState("State B", FlowStateCategory.Active, [], admin);
            flow.AddState("State C", FlowStateCategory.Active, [], admin);
            FlowState stateA = flow.States.Single(s => s.Name == "State A");
            FlowState stateB = flow.States.Single(s => s.Name == "State B");
            FlowState stateC = flow.States.Single(s => s.Name == "State C");

            // Act
            flow.RemoveState(stateB.Id, admin);

            // Assert
            flow.Transitions.Should().Contain(t => t.FromStateId == stateA.Id && t.ToStateId == stateC.Id);
        }

        [Fact]
        public void Should_Fail_When_NonAdminRemovesState()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);
            flow.AddState("State A", FlowStateCategory.Active, [], admin);
            FlowState state = flow.States.Single();
            User nonAdmin = UserData.GetActiveUser();

            // Act
            Result result = flow.RemoveState(state.Id, nonAdmin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowErrors.OnlyAdminCanModifyFlow);
        }

        [Fact]
        public void Should_Fail_When_FlowIsDeactivated()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetDeactivatedFlow(admin);

            // Act
            Result result = flow.RemoveState(Guid.NewGuid(), admin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowErrors.Deactivated);
        }

        [Fact]
        public void Should_Fail_When_ProjectDoesNotAllowRemoveState()
        {
            // Arrange
            (Flow flow, User admin) = FlowData.GetFlowOnBlockedProject();

            // Act
            Result result = flow.RemoveState(Guid.NewGuid(), admin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        [Fact]
        public void Should_Fail_When_StateNotFound()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);

            // Act
            Result result = flow.RemoveState(Guid.NewGuid(), admin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowErrors.StateNotFound);
        }

        [Fact]
        public void Should_Fail_When_RemovingLastCompletedState()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);
            flow.AddState("Done", FlowStateCategory.Completed, [], admin);
            FlowState completedState = flow.States.Single(s => s.Category == FlowStateCategory.Completed);

            // Act
            Result result = flow.RemoveState(completedState.Id, admin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowErrors.LastCompletedState);
        }

        [Fact]
        public void Should_Fail_When_RemovingLastCancelledState()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);
            flow.AddState("Cancelled", FlowStateCategory.Cancelled, [], admin);
            FlowState cancelledState = flow.States.Single(s => s.Category == FlowStateCategory.Cancelled);

            // Act
            Result result = flow.RemoveState(cancelledState.Id, admin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowErrors.LastCancelledState);
        }
    }

    public sealed class AddTransitionRole : BaseTest
    {
        [Fact]
        public void Should_AddTransitionRole_When_TransitionExistsAndRoleNotAllowed()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);
            flow.AddState("State A", FlowStateCategory.Active, [], admin);
            flow.AddState("State B", FlowStateCategory.Active, [], admin);
            FlowState stateA = flow.States.Single(s => s.Name == "State A");
            FlowState stateB = flow.States.Single(s => s.Name == "State B");
            FlowTransition transition = flow.Transitions.Single(t => t.FromStateId == stateA.Id && t.ToStateId == stateB.Id);

            // Act
            Result result = flow.AddTransitionRole(transition.Id, ProjectRole.Developer, admin);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            transition.AllowedRoles.Should().Contain(ProjectRole.Developer);
        }

        [Fact]
        public void Should_Fail_When_NonAdminAddsTransitionRole()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);
            flow.AddState("State A", FlowStateCategory.Active, [], admin);
            flow.AddState("State B", FlowStateCategory.Active, [], admin);
            FlowTransition transition = flow.Transitions.First();
            User nonAdmin = UserData.GetActiveUser();

            // Act
            Result result = flow.AddTransitionRole(transition.Id, ProjectRole.Developer, nonAdmin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowErrors.OnlyAdminCanModifyFlow);
        }

        [Fact]
        public void Should_Fail_When_FlowIsDeactivated()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetDeactivatedFlow(admin);

            // Act
            Result result = flow.AddTransitionRole(Guid.NewGuid(), ProjectRole.Developer, admin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowErrors.Deactivated);
        }

        [Fact]
        public void Should_Fail_When_ProjectDoesNotAllowAddTransitionRole()
        {
            // Arrange
            (Flow flow, User admin) = FlowData.GetFlowOnBlockedProject();

            // Act
            Result result = flow.AddTransitionRole(Guid.NewGuid(), ProjectRole.Developer, admin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        [Fact]
        public void Should_Fail_When_TransitionNotFound()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);

            // Act
            Result result = flow.AddTransitionRole(Guid.NewGuid(), ProjectRole.Developer, admin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowErrors.TransitionNotFound);
        }

        [Fact]
        public void Should_Fail_When_RoleAlreadyAllowed()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);
            flow.AddState("State A", FlowStateCategory.Active, [ProjectRole.Developer], admin);
            flow.AddState("State B", FlowStateCategory.Active, [ProjectRole.Developer], admin);
            FlowState stateA = flow.States.Single(s => s.Name == "State A");
            FlowState stateB = flow.States.Single(s => s.Name == "State B");
            FlowTransition transition = flow.Transitions.Single(t => t.FromStateId == stateA.Id && t.ToStateId == stateB.Id);

            // Act
            Result result = flow.AddTransitionRole(transition.Id, ProjectRole.Developer, admin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowErrors.TransitionRoleAlreadyAllowed);
        }
    }

    public sealed class RemoveTransitionRole : BaseTest
    {
        [Fact]
        public void Should_RemoveTransitionRole_When_TransitionExistsAndRoleIsAllowed()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);
            flow.AddState("State A", FlowStateCategory.Active, [ProjectRole.Developer], admin);
            flow.AddState("State B", FlowStateCategory.Active, [ProjectRole.Developer], admin);
            FlowState stateA = flow.States.Single(s => s.Name == "State A");
            FlowState stateB = flow.States.Single(s => s.Name == "State B");
            FlowTransition transition = flow.Transitions.Single(t => t.FromStateId == stateA.Id && t.ToStateId == stateB.Id);

            // Act
            Result result = flow.RemoveTransitionRole(transition.Id, ProjectRole.Developer, admin);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            transition.AllowedRoles.Should().NotContain(ProjectRole.Developer);
        }

        [Fact]
        public void Should_Fail_When_NonAdminRemovesTransitionRole()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);
            flow.AddState("State A", FlowStateCategory.Active, [], admin);
            flow.AddState("State B", FlowStateCategory.Active, [], admin);
            FlowTransition transition = flow.Transitions.First();
            User nonAdmin = UserData.GetActiveUser();

            // Act
            Result result = flow.RemoveTransitionRole(transition.Id, ProjectRole.Developer, nonAdmin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowErrors.OnlyAdminCanModifyFlow);
        }

        [Fact]
        public void Should_Fail_When_FlowIsDeactivated()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetDeactivatedFlow(admin);

            // Act
            Result result = flow.RemoveTransitionRole(Guid.NewGuid(), ProjectRole.Developer, admin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowErrors.Deactivated);
        }

        [Fact]
        public void Should_Fail_When_ProjectDoesNotAllowRemoveTransitionRole()
        {
            // Arrange
            (Flow flow, User admin) = FlowData.GetFlowOnBlockedProject();

            // Act
            Result result = flow.RemoveTransitionRole(Guid.NewGuid(), ProjectRole.Developer, admin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        [Fact]
        public void Should_Fail_When_TransitionNotFound()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);

            // Act
            Result result = flow.RemoveTransitionRole(Guid.NewGuid(), ProjectRole.Developer, admin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowErrors.TransitionNotFound);
        }

        [Fact]
        public void Should_Fail_When_RoleNotAllowed()
        {
            // Arrange
            User admin = UserData.GetActiveUser();
            Flow flow = FlowData.GetFlow(admin);
            flow.AddState("State A", FlowStateCategory.Active, [], admin);
            flow.AddState("State B", FlowStateCategory.Active, [], admin);
            FlowState stateA = flow.States.Single(s => s.Name == "State A");
            FlowState stateB = flow.States.Single(s => s.Name == "State B");
            FlowTransition transition = flow.Transitions.Single(t => t.FromStateId == stateA.Id && t.ToStateId == stateB.Id);

            // Act
            Result result = flow.RemoveTransitionRole(transition.Id, ProjectRole.Developer, admin);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(FlowErrors.TransitionRoleNotAllowed);
        }
    }
}
