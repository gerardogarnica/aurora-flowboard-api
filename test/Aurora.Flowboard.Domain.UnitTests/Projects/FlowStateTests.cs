namespace Aurora.Flowboard.Domain.UnitTests.Projects;

public sealed class FlowStateTests
{
    public sealed class GetBacklogStateId
    {
        [Fact]
        public void Should_ReturnNull_When_ThereAreNoActiveStates()
        {
            // Act
            Guid? backlogStateId = FlowState.GetBacklogStateId([]);

            // Assert
            backlogStateId.Should().BeNull();
        }

        [Fact]
        public void Should_ReturnNull_When_ThereIsASingleActiveState()
        {
            // Act
            Guid? backlogStateId = FlowState.GetBacklogStateId([(Guid.NewGuid(), 1)]);

            // Assert
            backlogStateId.Should().BeNull();
        }

        [Fact]
        public void Should_ReturnLowestSortOrder_When_ThereAreSeveralActiveStates()
        {
            // Arrange
            Guid first = Guid.NewGuid();

            // Act
            Guid? backlogStateId = FlowState.GetBacklogStateId([(first, 1), (Guid.NewGuid(), 2), (Guid.NewGuid(), 3)]);

            // Assert
            backlogStateId.Should().Be(first);
        }

        [Fact]
        public void Should_ReturnLowestSortOrder_When_StatesAreUnordered()
        {
            // Arrange
            Guid first = Guid.NewGuid();

            // Act
            Guid? backlogStateId = FlowState.GetBacklogStateId([(Guid.NewGuid(), 3), (first, 1), (Guid.NewGuid(), 2)]);

            // Assert
            backlogStateId.Should().Be(first);
        }

        [Fact]
        public void Should_ReturnFirstActiveState_OfAProjectFlow()
        {
            // Arrange
            Project project = ProjectData.GetProjectWithFlowStates();
            FlowState todo = project.FlowStates.Single(s => s.Name == "Todo");

            // Act
            Guid? backlogStateId = FlowState.GetBacklogStateId([.. project.FlowStates
                .Where(s => s.Category == FlowStateCategory.Active)
                .Select(s => (s.Id, s.SortOrder))]);

            // Assert
            backlogStateId.Should().Be(todo.Id);
        }
    }
}
